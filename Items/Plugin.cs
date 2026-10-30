using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Pickups.Projectiles;
using Exiled.API.Features.Toys;
using Exiled.CustomItems.API;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Scp914;
using MEC;
using PlayerRoles;
using UnityEngine;
using P = Exiled.Events.Handlers.Player;
using S = Exiled.Events.Handlers.Server;
using M = Exiled.Events.Handlers.Map;
using U = Exiled.Events.Handlers.Scp914;
using C = Exiled.Events.Handlers.Scp079;
using FxAudio = Brainless.Internal.Audio;

namespace Brainless.Items
{
    public sealed class Plugin : Plugin<Config>
    {
        public override string Name => "BrainlessItems";
        public override string Prefix => "brainless_items";
        public override string Author => "Brainless Project";
        public override Version Version => new Version(0, 1, 3);
        public override Version RequiredExiledVersion => new Version(9, 15, 2);
        public override PluginPriority Priority => PluginPriority.Low;

        private readonly Lifetime lifetime = new Lifetime();
        private readonly LimitedBudget budget = new LimitedBudget();
        private readonly FragProjectiles fragments = new FragProjectiles();
        private readonly Dictionary<uint, RegisteredItem> registered = new Dictionary<uint, RegisteredItem>();
        private readonly Dictionary<int, int> charges = new Dictionary<int, int>();
        private readonly Dictionary<int, Payload> thrown = new Dictionary<int, Payload>();
        private readonly Dictionary<Player, Channel> channels = new Dictionary<Player, Channel>();
        private readonly HashSet<Player> keyHeld = new HashSet<Player>();
        private readonly Dictionary<Player, float> filters = new Dictionary<Player, float>();
        private readonly Dictionary<Player, MovementLayer> movement = new Dictionary<Player, MovementLayer>();
        private readonly HashSet<string> specimenUsed = new HashSet<string>();
        private readonly List<Device> devices = new List<Device>();
        private readonly List<Field> fields = new List<Field>();
        private readonly Dictionary<Room, float> jamCooldown = new Dictionary<Room, float>();
        private KeybindSetting toolKey;
        private int chalkNumber;
        private bool capacitySet;
        private Brainless.Internal.Updates.PluginUpdates updates;
        public override void OnEnabled()
        {
            var enabled = Config.Items.Where(s => s.Enabled).ToArray();
            if (enabled.Select(s => s.Id).Distinct().Count() != enabled.Length || enabled.Select(s => s.Name).Distinct().Count() != enabled.Length)
                throw new InvalidOperationException("Item IDs and names must be unique.");
            if (enabled.Any(s => Exiled.CustomItems.API.Features.CustomItem.Registered.Any(i => i.Id == s.Id || i.Name == s.Name)))
                throw new InvalidOperationException("A configured item ID or name is already registered by another plugin.");
            foreach (var spec in Config.Items.Where(s => s.Enabled))
            {
                if (Exiled.CustomItems.API.Features.CustomItem.Registered.Any(i => i.Id == spec.Id))
                    throw new InvalidOperationException($"Custom item ID {spec.Id} already registered.");
                var item = new RegisteredItem(this, spec);
                item.Register();
                registered.Add(spec.Id, item);
            }

            toolKey = new KeybindSetting(Config.ToolKeybindId, "Use custom item or nearby device", KeyCode.V, true, false, "Hold for tools, devices and specimens.", onChanged: KeyChanged);
            SettingBase.Register(new[] { toolKey }).ToArray();
            P.UsingItem += Using;
            P.FlippingCoin += Coin;
            P.TogglingRadio += Radio;
            P.TogglingFlashlight += Flashlight;
            P.InteractingDoor += Door;
            P.ThrownProjectile += Thrown;
            P.Hurting += Hurting;
            P.ChangingRole += RoleChanged;
            P.Left += Left;
            P.ChangingItem += Selected;
            M.ExplodingGrenade += Exploding;
            M.ChangedIntoGrenade += ChangedIntoGrenade;
            U.UpgradingPickup += UpgradePickup;
            U.UpgradingInventoryItem += UpgradeInventory;
            C.TriggeringDoor += ComputerDoor;
            C.RoomBlackout += Blackout;
            C.LockingDown += Lockdown;
            C.ChangingSpeakerStatus += Speaker;
            S.WaitingForPlayers += Reset;
            S.RoundStarted += RoundStarted;
            S.RoundEnded += Ended;
            Reset();
            AdminItems.Enable("brainless.items.commands");
            NativeSmoke.Enable();
            base.OnEnabled();
            updates = Brainless.Internal.Updates.PluginUpdates.Start(GetType().Assembly, "Items", Config.AutoUpdate);
        }

        public override void OnDisabled()
        {
            updates?.Dispose();
            updates = null;
            P.UsingItem -= Using;
            P.FlippingCoin -= Coin;
            P.TogglingRadio -= Radio;
            P.TogglingFlashlight -= Flashlight;
            P.InteractingDoor -= Door;
            P.ThrownProjectile -= Thrown;
            P.Hurting -= Hurting;
            P.ChangingRole -= RoleChanged;
            P.Left -= Left;
            P.ChangingItem -= Selected;
            M.ExplodingGrenade -= Exploding;
            M.ChangedIntoGrenade -= ChangedIntoGrenade;
            U.UpgradingPickup -= UpgradePickup;
            U.UpgradingInventoryItem -= UpgradeInventory;
            C.TriggeringDoor -= ComputerDoor;
            C.RoomBlackout -= Blackout;
            C.LockingDown -= Lockdown;
            C.ChangingSpeakerStatus -= Speaker;
            S.WaitingForPlayers -= Reset;
            S.RoundStarted -= RoundStarted;
            S.RoundEnded -= Ended;
            SettingBase.Unregister(settings: new[] { toolKey }).ToArray();
            AdminItems.Disable();
            foreach (var item in registered.Values)
                item.Unregister();
            registered.Clear();
            Cleanup();
            NativeSmoke.Disable();
            base.OnDisabled();
        }

        private void Cleanup()
        {
            foreach (var layer in movement.Values)
                layer.Clear();
            movement.Clear();
            fragments.Clear();
            NativeSmoke.Clear();
            foreach (var payload in thrown.Values.ToArray())
                if (payload.Projectile.Base != null)
                    Safe.Run(() => payload.Projectile.Destroy());
            lifetime.Clear();
            channels.Clear();
            keyHeld.Clear();
            fields.Clear();
            devices.Clear();
            filters.Clear();
            thrown.Clear();
        }

        private void Reset()
        {
            Cleanup();
            charges.Clear();
            foreach (var item in registered.Values)
                item.TrackedSerials.Clear();
            specimenUsed.Clear();
            jamCooldown.Clear();
            chalkNumber = 0;
            budget.Reset(0);
            capacitySet = false;
            lifetime.Run(Tick());
        }

        private void Ended(Exiled.Events.EventArgs.Server.RoundEndedEventArgs ev) => Reset();
        private void RoundStarted()
        {
            EnsureCapacity();
            if (Config.WorldLoot)
                lifetime.Later(1, SpawnWorldLoot);
        }

        private void EnsureCapacity()
        {
            if (capacitySet)
                return;
            capacitySet = true;
            budget.Capacity = Rules.UtilityCap(Player.List.Count(p => p.IsConnected && !p.IsHost));
        }

        private int Population => Player.List.Count(p => p.IsConnected && !p.IsHost);

        internal bool CanIssue(ItemSpec spec, int count) => IssueFailure(spec, count, false) == null;
        internal string IssueFailure(ItemSpec spec, int count, bool ignoreLimits)
        {
            if (count < 1)
                return "The requested quantity must be positive.";
            if (spec.Id == 7101 && !registered.ContainsKey(7111))
                return "Gas requires the Filter Inhaler to be enabled.";
            if (ignoreLimits)
                return null;
            EnsureCapacity();
            int cap = spec.Id == 7102 ? Math.Min(spec.RoundCap, Rules.ClusterCap(Population)) : spec.RoundCap;
            if (spec.Id == 7102 && Population < 12)
                return $"Clusters require at least 12 players for normal supply; currently {Population}.";
            if (budget.Count(spec.Id.ToString()) + count > cap)
                return $"{spec.Name} round limit reached ({budget.Count(spec.Id.ToString())}/{cap}).";
            int slots = spec.Id == 7101 ? count * 2 : count;
            if (budget.Total + slots > budget.Capacity)
                return $"Utility supply exhausted ({budget.Total}/{budget.Capacity}); this kit needs {slots} slot(s).";
            if (spec.Id == 7101)
            {
                var filter = registered[7111];
                if (!budget.CanIssue("7111", filter.Spec.RoundCap, count))
                    return "The paired Filter Inhaler round limit has been reached.";
            }

            return null;
        }

        internal bool CanIssueBatch(uint[] ids)
        {
            EnsureCapacity();
            var ours = ids.Where(id => id >= 7101 && id <= 7112).ToArray();
            int extra = ours.Count(id => id == 7101);
            if (budget.Total + ours.Length + extra > budget.Capacity)
                return false;
            return ours.GroupBy(id => id).All(g => registered.TryGetValue(g.Key, out var item) && CanIssue(item.Spec, g.Count()));
        }

        internal bool Mint(RegisteredItem item, int serial, bool ignoreLimits = false)
        {
            if (IssueFailure(item.Spec, 1, ignoreLimits) != null)
                return false;
            int cap = item.Id == 7102 ? Rules.ClusterCap(Population) : item.Spec.RoundCap;
            if (!budget.TryIssue(item.Id.ToString(), Math.Min(item.Spec.RoundCap, cap), ignoreLimits: ignoreLimits))
                return false;
            charges[serial] = Math.Max(1, item.Spec.Charges);
            return true;
        }

        internal void Setup(Item item, RegisteredItem definition)
        {
            if (item is Exiled.API.Features.Items.Keycards.CustomKeycardItem card)
            {
                card.Permissions = KeycardPermissions.ContainmentLevelOne;
                card.ItemName = definition.Name;
            }
        }

        internal void Unmint(RegisteredItem item, int serial, bool ignoreLimits)
        {
            if (charges.Remove(serial) && !ignoreLimits)
                budget.RevokeIssue(item.Id.ToString());
            item.TrackedSerials.Remove(serial);
        }

        internal string GiveFilter(Player player, bool ignoreLimits)
        {
            if (registered.TryGetValue(7111, out var item))
                return item.TryGive(player, ignoreLimits, false);
            return "The paired Filter Inhaler is disabled.";
        }

        internal string SpawnFilter(Vector3 position, bool ignoreLimits)
        {
            if (registered.TryGetValue(7111, out var item))
                return item.TrySpawn(position, ignoreLimits, out _);
            return "The paired Filter Inhaler is disabled.";
        }

        private RegisteredItem Find(int serial) => registered.Values.FirstOrDefault(i => i.TrackedSerials.Contains(serial));
        private void SpawnWorldLoot()
        {
            var room = Room.List.Where(r => r.Zone == ZoneType.HeavyContainment && r.Type.ToString().IndexOf("Armory", StringComparison.OrdinalIgnoreCase) >= 0).FirstOrDefault();
            if (room == null)
                return;
            var options = registered.Values.Where(i => CanIssue(i.Spec, 1)).ToArray();
            if (options.Length == 0)
                return;
            var choice = options[UnityEngine.Random.Range(0, options.Length)];
            choice.Spawn(room.Position + Vector3.up);
            if (choice.Id != 7101)
            {
                var other = options.FirstOrDefault(i => i != choice && CanIssue(i.Spec, 1));
                other?.Spawn(room.Position + Vector3.up + Vector3.right * .5f);
            }
        }

        private void KeyChanged(Player player, SettingBase setting) => Safe.Run(() =>
        {
            if (!(setting is KeybindSetting key))
                return;
            if (key.IsPressed)
            {
                keyHeld.Add(player);
                Activate(player, true);
            }
            else
            {
                keyHeld.Remove(player);
                if (channels.TryGetValue(player, out var c) && c.Hold)
                    channels.Remove(player);
            }
        });
        private void Coin(FlippingCoinEventArgs ev)
        {
            if (Find(ev.Item.Serial) == null)
                return;
            ev.IsAllowed = false;
            Safe.Run(() => Activate(ev.Player, false));
        }

        private void Radio(TogglingRadioEventArgs ev)
        {
            if (Find(ev.Item.Serial) == null)
                return;
            ev.IsAllowed = false;
            Safe.Run(() => Activate(ev.Player, false));
        }

        private void Flashlight(TogglingFlashlightEventArgs ev)
        {
            if (Find(ev.Item.Serial) == null)
                return;
            ev.IsAllowed = false;
            Safe.Run(() => Activate(ev.Player, false));
        }

        private void Using(UsingItemEventArgs ev)
        {
            if (Find(ev.Item.Serial) == null)
                return;
            ev.IsAllowed = false;
            Safe.Run(() => Activate(ev.Player, false));
        }

        private void Selected(ChangingItemEventArgs ev)
        {
            if (ev.Item == null)
                return;
            var item = Find(ev.Item.Serial);
            if (item != null)
                ev.Player.ShowHint($"<b>{item.Name}</b>\n{item.Description}", 5);
        }

        private void RoleChanged(ChangingRoleEventArgs ev)
        {
            if (ev.IsAllowed)
            {
                channels.Remove(ev.Player);
                filters.Remove(ev.Player);
                keyHeld.Remove(ev.Player);
                if (movement.TryGetValue(ev.Player, out var layer))
                {
                    layer.Clear();
                    movement.Remove(ev.Player);
                }
            }
        }

        private void Left(LeftEventArgs ev)
        {
            channels.Remove(ev.Player);
            keyHeld.Remove(ev.Player);
            filters.Remove(ev.Player);
            if (movement.TryGetValue(ev.Player, out var layer))
            {
                layer.Clear();
                movement.Remove(ev.Player);
            }
        }

        private void Hurting(HurtingEventArgs ev)
        {
            if (ev.IsAllowed && ev.Amount > 0)
                channels.Remove(ev.Player);
        }

        private void Begin(Player player, float duration, Item item, bool hold, Action action)
        {
            if (channels.ContainsKey(player))
                return;
            channels[player] = new Channel
            {
                Player = player,
                Start = Time.time,
                Duration = duration,
                Item = item,
                Role = player.Role.Base,
                Position = player.Position,
                Hold = hold,
                Complete = action
            };
            player.ShowHint($"Hold position for {duration:0.#} seconds...", duration);
        }

        private void Activate(Player player, bool hold)
        {
            if (!player.IsAlive || player.IsCuffed || channels.ContainsKey(player))
                return;
            var nearby = devices.FirstOrDefault(d => Vector3.Distance(player.Position, d.Position) <= 3 && Vector3.Dot(player.CameraTransform.forward, (d.Position - player.CameraTransform.position).normalized) > .75f && World.ClearLine(player.CameraTransform.position, d.Position + Vector3.up * .1f));
            if (nearby != null)
            {
                if (nearby.Id == 7107 && player.IsHuman)
                {
                    if (nearby.Scans < 1)
                        return;
                    Begin(player, 3, null, hold, () =>
                    {
                        if (!devices.Contains(nearby) || nearby.Scans < 1)
                            return;
                        nearby.Scans--;
                        bool humans = Player.List.Any(p => p != player && p.IsAlive && p.IsHuman && p.CurrentRoom == nearby.Room);
                        bool scps = Player.List.Any(p => p != player && p.IsAlive && p.IsScp && p.Role.Type != RoleTypeId.Scp079 && p.CurrentRoom == nearby.Room);
                        player.ShowHint($"Survey: other humans {(humans ? "YES" : "NO")}; physical SCPs {(scps ? "YES" : "NO")}", 6);
                        World.Cue(nearby.Position, "Survey scan complete");
                    });
                    FxAudio.Play(lifetime, nearby.Position, 3, "beep");
                    return;
                }

                Begin(player, nearby.Id == 7105 ? 3 : 2, null, hold, () => RemoveDevice(nearby));
                return;
            }

            if (player.Role.Type == RoleTypeId.Scp0492)
            {
                var specimen = Pickup.List.FirstOrDefault(p => Find(p.Serial)?.Id == 7112 && Vector3.Distance(p.Position, player.Position) < 2 && World.ClearLine(player.CameraTransform.position, p.Position));
                if (specimen == null || specimenUsed.Contains(player.UserId))
                    return;
                Begin(player, 4, null, hold, () =>
                {
                    if (!Pickup.List.Contains(specimen) || specimenUsed.Contains(player.UserId))
                        return;
                    specimenUsed.Add(player.UserId);
                    specimen.Destroy();
                    player.Heal(80);
                });
                return;
            }

            Item held = player.CurrentItem;
            var item = held == null ? null : Find(held.Serial);
            if (item == null || !player.IsHuman)
                return;
            switch (item.Id)
            {
                case 7105:
                    var door = LookDoor(player);
                    if (door == null)
                        return;
                    UseDoorItem(player, held, item, door, hold);
                    break;
                case 7106:
                case 7107:
                    Begin(player, 2, held, hold, () => Deploy(player, held, item));
                    break;
                case 7108:
                    if (Spend(player, held))
                    {
                        Vector3 position = Ground(player.Position);
                        var marker = World.Marker(lifetime, position + Vector3.up * .03f, Color.cyan, new Vector3(.2f, .015f, .2f));
                        var text = lifetime.Own(Text.Create(position + Vector3.up * .2f, (++chalkNumber).ToString()));
                        var chalk = new Device
                        {
                            Id = 7108,
                            Position = position,
                            Expires = Time.time + 45,
                            Marker = marker,
                            Text = text
                        };
                        devices.Add(chalk);
                        while (devices.Count(d => d.Id == 7108) > 9)
                            RemoveDevice(devices.First(d => d.Id == 7108));
                    }

                    break;
                case 7109:
                    Begin(player, 4, held, hold, () =>
                    {
                        if (Spend(player, held))
                            player.Heal(35);
                    });
                    break;
                case 7111:
                    Begin(player, 2, held, hold, () =>
                    {
                        if (Spend(player, held))
                        {
                            filters[player] = Time.time + 12;
                            player.ShowHint("Custom gas filter active for 12 seconds", 3);
                        }
                    });
                    break;
                case 7112:
                    player.ShowHint("Drop or throw the specimen. A zombie can choose to eat it with the tool key.", 5);
                    break;
            }
        }

        private bool Spend(Player player, Item item)
        {
            if (!player.Items.Contains(item) || !charges.TryGetValue(item.Serial, out int uses) || uses < 1)
                return false;
            charges[item.Serial] = --uses;
            if (uses == 0)
            {
                player.RemoveItem(item);
                Find(item.Serial)?.TrackedSerials.Remove(item.Serial);
            }
            else
                player.ShowHint($"{uses} uses left", 2);
            return true;
        }

        private Door LookDoor(Player player)
        {
            foreach (var hit in Physics.RaycastAll(player.CameraTransform.position, player.CameraTransform.forward, 3, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            {
                if (hit.collider.GetComponentInParent<ReferenceHub>() != null)
                    continue;
                return Exiled.API.Features.Doors.Door.Get(hit.collider.gameObject);
            }

            return null;
        }

        private void Door(InteractingDoorEventArgs ev) => Safe.Run(() =>
        {
            if (!ev.IsAllowed)
                return;
            var brace = devices.FirstOrDefault(d => d.Id == 7105 && d.Door == ev.Door);
            if (brace != null)
            {
                ev.IsAllowed = false;
                if (ev.Player.Role.Type == RoleTypeId.Scp106)
                {
                    ev.IsAllowed = true;
                    return;
                }

                Begin(ev.Player, 3, null, false, () => RemoveDevice(brace));
                return;
            }

            var item = ev.Player.CurrentItem == null ? null : Find(ev.Player.CurrentItem.Serial);
            if (item?.Id != 7105)
                return;
            ev.IsAllowed = false;
            UseDoorItem(ev.Player, ev.Player.CurrentItem, item, ev.Door, false);
        });
        private void UseDoorItem(Player player, Item held, RegisteredItem item, Door door, bool hold)
        {
            if (door.IsOpen || door.IsLocked || Vector3.Distance(player.Position, door.Position) > 3)
                return;
            if (item.Id == 7105 && !World.OrdinaryDoor(door))
                return;
            Begin(player, 2, held, hold, () =>
            {
                if (door.IsLocked || door.IsOpen || Vector3.Distance(player.Position, door.Position) > 3 || !Spend(player, held))
                    return;
                var device = new Device
                {
                    Id = 7105,
                    Door = door,
                    Position = door.Position,
                    Expires = Time.time + 8
                };
                device.Marker = World.Marker(lifetime, door.Position + Vector3.up, Color.yellow, new Vector3(.65f, .08f, .08f), PrimitiveType.Cube);
                devices.Add(device);
            });
            FxAudio.Play(lifetime, door.Position, 2, "beep");
        }

        private void Deploy(Player player, Item held, RegisteredItem item)
        {
            Room room = player.CurrentRoom;
            if (room == null || room.Zone == ZoneType.Surface || room.Type == RoomType.Pocket)
                return;
            if (item.Id == 7106 && (devices.Any(d => d.Id == 7106) || (jamCooldown.TryGetValue(room, out float cooldown) && cooldown > Time.time)))
            {
                player.ShowHint("Jammer unavailable in this room yet", 3);
                return;
            }

            if (!Spend(player, held))
                return;
            Vector3 point = Ground(player.Position);
            var d = new Device
            {
                Id = item.Id,
                Position = point,
                Room = room,
                ActiveAt = Time.time + 2,
                Expires = Time.time + (item.Id == 7106 ? 8 : 45),
                Scans = 2
            };
            d.Marker = World.Marker(lifetime, point + Vector3.up * .15f, item.Id == 7106 ? Color.red : Color.green, new Vector3(.3f, .3f, .3f), PrimitiveType.Cube);
            d.Text = lifetime.Own(Text.Create(point + Vector3.up * .55f, item.Name));
            devices.Add(d);
            if (item.Id == 7106)
            {
                jamCooldown[room] = d.Expires + 30;
                World.Cue(point, "Jammer arming - hold tool key to deactivate");
                FxAudio.Play(lifetime, point, 8, "beep");
            }
        }

        private void RemoveDevice(Device device)
        {
            devices.Remove(device);
            if (device.Marker != null)
                lifetime.Release(device.Marker);
            if (device.Text != null)
                lifetime.Release(device.Text);
        }

        private bool Jammed(Room room) => room != null && devices.Any(d => d.Id == 7106 && d.Room == room && d.ActiveAt <= Time.time && d.Expires > Time.time);
        private void ComputerDoor(Exiled.Events.EventArgs.Scp079.TriggeringDoorEventArgs ev)
        {
            if (devices.Any(d => d.Id == 7105 && d.Door == ev.Door) || World.OrdinaryDoor(ev.Door) && ev.Door.Rooms.Any(Jammed))
                ev.IsAllowed = false;
        }

        private void Blackout(Exiled.Events.EventArgs.Scp079.RoomBlackoutEventArgs ev)
        {
            if (Jammed(ev.Room))
                ev.IsAllowed = false;
        }

        private void Lockdown(Exiled.Events.EventArgs.Scp079.LockingDownEventArgs ev)
        {
            if (Jammed(ev.Room))
                ev.IsAllowed = false;
        }

        private void Speaker(Exiled.Events.EventArgs.Scp079.ChangingSpeakerStatusEventArgs ev)
        {
            if (Jammed(ev.Room))
                ev.IsAllowed = false;
        }

        private void Thrown(ThrownProjectileEventArgs ev)
        {
            var item = Find(ev.Throwable.Serial);
            if (item == null)
                return;
            Arm(item, ev.Projectile, ev.Player);
        }

        private void ChangedIntoGrenade(ChangedIntoGrenadeEventArgs ev)
        {
            var item = Find(ev.Pickup.Serial);
            if (item != null)
                Arm(item, ev.Projectile, ev.Pickup.PreviousOwner);
        }

        private void Arm(RegisteredItem item, Projectile projectile, Player owner)
        {
            thrown[projectile.Serial] = new Payload
            {
                Item = item,
                Projectile = projectile,
                Source = new DamageSource(owner)
            };
            if (projectile is TimeGrenadeProjectile timed)
                timed.FuseTime = item.Id == 7101 ? 2.5f : item.Id == 7102 ? 3 : item.Id == 7104 ? 1 : 2;
        }

        private void Exploding(ExplodingGrenadeEventArgs ev) => Safe.Run(() =>
        {
            if (fragments.Exploding(ev))
                return;
            if (!thrown.TryGetValue(ev.Projectile.Serial, out var payload))
                return;
            bool allowed = ev.IsAllowed;
            ev.IsAllowed = false;
            thrown.Remove(ev.Projectile.Serial);
            Vector3 position = ev.Position;
            // Native EffectGrenade.ServerFuseEnd already destroys the projectile.
            if (!allowed)
                return;
            switch (payload.Item.Id)
            {
                case 7101:
                case 7103:
                    bool gas = payload.Item.Id == 7101;
                    float radius = gas ? 4 : 3;
                    var field = new Field
                    {
                        Gas = gas,
                        Position = position,
                        Room = Room.Get(position),
                        Source = payload.Source,
                        Expires = Time.time + (gas ? 8 : 6),
                        Radius = radius
                    };
                    if (gas)
                        field.Smoke = NativeSmoke.Spawn(Ground(position), radius);
                    else
                        field.Marker = World.Marker(lifetime, position + Vector3.up * .025f, new Color(.1f, .65f, .15f, .35f), new Vector3(radius * 2, .02f, radius * 2));
                    fields.Add(field);
                    World.Cue(position, gas ? "Irritant gas - leave the cloud or use a filter" : "Containment foam - slow floor", 10);
                    break;
                case 7102:
                    Cluster(position, payload.Source);
                    break;
                case 7104:
                    FxAudio.Play(lifetime, position, 6, "steps");
                    break;
            }
        });
        private void Cluster(Vector3 position, DamageSource source)
        {
            var blastBudget = new BlastBudget();
            float rotation = UnityEngine.Random.Range(0, Mathf.PI * 2);
            // All four leave the first detonation together. Each has native
            // rigidbody bounce, gravity, ticking fuse and explosion effects.
            for (int i = 0; i < 4; i++)
            {
                float angle = rotation + i * Mathf.PI * .5f;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                fragments.Launch(position + Vector3.up * .15f, outward * 4.5f + Vector3.up * 4, 1.5f + i * .08f, source, 2.5f, 35, 90, blastBudget);
            }
        }

        private Vector3 Ground(Vector3 point) => Physics.Raycast(point + Vector3.up, Vector3.down, out var hit, 3, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hit.point : point - Vector3.up * .8f;
        private IEnumerator<float> Tick()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(.1f);
                Safe.Run(Update);
            }
        }

        private void Update()
        {
            foreach (var pair in channels.ToArray())
            {
                var p = pair.Key;
                var c = pair.Value;
                if (!p.IsConnected || !p.IsAlive || p.Role.Base != c.Role || p.IsCuffed || (c.Hold && !keyHeld.Contains(p)) || (c.Item != null && p.CurrentItem != c.Item) || Vector3.Distance(p.Position, c.Position) > .6f)
                {
                    channels.Remove(p);
                    continue;
                }

                if (Time.time >= c.Start + c.Duration)
                {
                    channels.Remove(p);
                    Safe.Run(c.Complete);
                }
            }

            foreach (var d in devices.Where(d => d.Expires <= Time.time).ToArray())
                RemoveDevice(d);
            foreach (var field in fields.Where(f => f.Expires <= Time.time).ToArray())
            {
                fields.Remove(field);
                lifetime.Release(field.Marker);
                NativeSmoke.Release(field.Smoke);
            }

            foreach (var player in Player.List.Where(p => p.IsAlive).ToArray())
            {
                float slow = 1;
                foreach (var field in fields)
                {
                    if (!field.Source.CanAffect(player) || player.CurrentRoom != field.Room || Vector3.Distance(player.Position, field.Position) > field.Radius || !World.ClearLine(field.Position + Vector3.up * .2f, player.Position))
                        continue;
                    if (!field.Gas)
                    {
                        slow = Math.Min(slow, player.IsScp ? .92f : .9f);
                        continue;
                    }

                    if (!player.IsHuman || (filters.TryGetValue(player, out float until) && until > Time.time))
                        continue;
                    slow = Math.Min(slow, .8f);
                }

                if (!movement.TryGetValue(player, out var layer))
                    movement[player] = layer = new MovementLayer();
                layer.Set(player, slow);
            }
        }

        private ItemType Conversion(uint id, ItemType type) => id == 7110 ? ItemType.KeycardJanitor : id == 7105 || id == 7108 || id == 7112 ? ItemType.Coin : type;
        private void UpgradePickup(UpgradingPickupEventArgs ev) => Safe.Run(() =>
        {
            var item = Find(ev.Pickup.Serial);
            if (item == null)
                return;
            ev.IsAllowed = false;
            var old = ev.Pickup;
            item.TrackedSerials.Remove(old.Serial);
            charges.Remove(old.Serial);
            var replacement = Pickup.CreateAndSpawn(Conversion(item.Id, old.Type), old.Position);
            old.Destroy();
            Scp914.Scp914Upgrader.ProcessPickup(replacement.Base, true, ev.KnobSetting);
        });
        private void UpgradeInventory(UpgradingInventoryItemEventArgs ev) => Safe.Run(() =>
        {
            var item = Find(ev.Item.Serial);
            if (item == null)
                return;
            ev.IsAllowed = false;
            item.TrackedSerials.Remove(ev.Item.Serial);
            charges.Remove(ev.Item.Serial);
            var type = Conversion(item.Id, ev.Item.Type);
            ev.Player.RemoveItem(ev.Item);
            var replacement = ev.Player.AddItem(type);
            if (Scp914.Scp914Upgrader.TryGetProcessor(type, out var processor))
            {
                var result = processor.UpgradeInventoryItem(ev.KnobSetting, replacement.Base);
                Scp914.Processors.Scp914ItemProcessor.ProcessResultingItem(replacement.Base, result, ev.KnobSetting);
            }
        });
        private sealed class Payload
        {
            internal RegisteredItem Item;
            internal Projectile Projectile;
            internal DamageSource Source;
        }

        private sealed class Channel
        {
            internal Player Player;
            internal float Start, Duration;
            internal Item Item;
            internal PlayerRoles.PlayerRoleBase Role;
            internal Vector3 Position;
            internal bool Hold;
            internal Action Complete;
        }

        private sealed class Device
        {
            internal uint Id;
            internal Vector3 Position;
            internal Room Room;
            internal Door Door;
            internal float Expires, ActiveAt;
            internal int Scans;
            internal Primitive Marker;
            internal Text Text;
        }

        private sealed class Field
        {
            internal bool Gas;
            internal Vector3 Position;
            internal Room Room;
            internal DamageSource Source;
            internal float Expires, Radius;
            internal Primitive Marker;
            internal Scp244Pickup Smoke;
        }
    }
}
