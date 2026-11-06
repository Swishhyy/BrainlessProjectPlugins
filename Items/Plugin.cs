using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Hazards;
using Exiled.API.Features.Items;
using Exiled.API.Features.Lockers;
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
    public sealed partial class Plugin : Plugin<Config>
    {
        public override string Name => "BrainlessItems";
        public override string Prefix => "brainless_items";
        public override string Author => "Swishhyy";
        public override Version Version => new Version(0, 13, 0);
        public override Version RequiredExiledVersion => new Version(9, 15, 3);
        public override PluginPriority Priority => PluginPriority.Low;
        public static Plugin Instance { get; private set; }

        private readonly Lifetime lifetime = new Lifetime();
        private readonly LimitedBudget budget = new LimitedBudget();
        private readonly FragProjectiles fragments = new FragProjectiles();
        private readonly Dictionary<uint, RegisteredItem> registered = new Dictionary<uint, RegisteredItem>();
        private readonly Dictionary<int, int> charges = new Dictionary<int, int>();
        private readonly GrenadeFuses<Payload> thrown = new GrenadeFuses<Payload>();
        private readonly Dictionary<Player, Channel> channels = new Dictionary<Player, Channel>();
        private readonly HashSet<Player> keyHeld = new HashSet<Player>();
        private readonly Dictionary<Player, MovementLayer> movement = new Dictionary<Player, MovementLayer>();
        private readonly List<Device> devices = new List<Device>();
        private readonly List<Hazards.TantrumEnvironmentalHazard> puddles = new List<Hazards.TantrumEnvironmentalHazard>();
        private KeybindSetting toolKey;
        private bool capacitySet;
        private IDisposable updates;
        private NaturalDistribution distribution;
        public override void OnEnabled()
        {
            bool migrateConfig = Config.CatalogRevision < 16;
            Config.Prepare();
            LoadIndividualItems(Config);
            Config.Prepare();
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
                CustomRarity.Register(spec.Id, spec.Rarity);
            }

            toolKey = new KeybindSetting(Config.ToolKeybindId, "Use custom item or nearby device", KeyCode.V, true, false, "Hold for tools, devices and specimens.", onChanged: KeyChanged);
            SettingBase.Register(new[] { toolKey }).ToArray();
            SupSubscribe();
            CursedSubscribe();
            P.UsingItem += Using;
            P.ItemAdded += GlowPickupHint;
            P.FlippingCoin += Coin;
            P.TogglingRadio += Radio;
            P.TogglingFlashlight += Flashlight;
            P.InteractingDoor += Door;
            P.ThrownProjectile += Thrown;
            P.Hurting += Hurting;
            P.ChangingRole += RoleChanged;
            P.Left += Left;
            P.ChangingItem += Selected;
            M.ExplodingGrenade.Subscribe(Exploding, -1000);
            M.SpawningGrenadeEffect.Subscribe(GrenadeEffect, -1000);
            M.ChangedIntoGrenade += ChangedIntoGrenade;
            U.UpgradingPickup += UpgradePickup;
            U.UpgradingInventoryItem += UpgradeInventory;
            C.TriggeringDoor += ComputerDoor;
            S.WaitingForPlayers += Reset;
            S.RoundStarted += RoundStarted;
            S.RoundEnded += Ended;
            Reset();
            AdminItems.Enable("brainless.items.commands");
            NativeSmoke.Enable();
            distribution = new NaturalDistribution("Items", lifetime, () => Config.Distribution, NaturalContent, () => Config.WorldLoot ? Config.Tuning.Supply.WorldLootCount : 0, () => Config.Debug);
            Instance = this;
            ConfigMigration.Tidy();
            if (migrateConfig)
                ConfigMigration.Save(this);
            base.OnEnabled();
            updates = Brainless.Internal.Updates.PluginUpdates.Start(GetType().Assembly, "Items", Config.AutoUpdate);
        }

        public override void OnDisabled()
        {
            Instance = null;
            distribution?.Dispose();
            distribution = null;
            updates?.Dispose();
            updates = null;
            SupUnsubscribe();
            CursedUnsubscribe();
            P.UsingItem -= Using;
            P.ItemAdded -= GlowPickupHint;
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
            M.SpawningGrenadeEffect -= GrenadeEffect;
            M.ChangedIntoGrenade -= ChangedIntoGrenade;
            U.UpgradingPickup -= UpgradePickup;
            U.UpgradingInventoryItem -= UpgradeInventory;
            C.TriggeringDoor -= ComputerDoor;
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
            SupCleanup();
            fragments.Clear();
            NativeSmoke.Clear();
            foreach (var payload in thrown.Values.ToArray())
                if (payload.Projectile.Base != null)
                    Safe.Run(() => payload.Projectile.Destroy());
            lifetime.Clear();
            channels.Clear();
            keyHeld.Clear();
            foreach (var puddle in puddles)
                Safe.Run(() =>
                {
                    if (puddle != null && puddle.IsActive)
                        puddle.ServerDestroy();
                });
            puddles.Clear();
            ClearGlows();
            devices.Clear();
            thrown.Clear();
        }

        private void Reset()
        {
            distribution?.Reset();
            Cleanup();
            charges.Clear();
            foreach (var item in registered.Values)
                item.TrackedSerials.Clear();
            SupReset();
            budget.Reset(0);
            capacitySet = false;
            lifetime.Run(Tick());
            lifetime.Run(SupUpdates());
            lifetime.Run(GlowLoop());
        }

        private void Ended(Exiled.Events.EventArgs.Server.RoundEndedEventArgs ev) => Reset();
        private void RoundStarted()
        {
            roundStartedAt = Time.time;
            EnsureCapacity();
            distribution?.RoundStarted();
            CursedLootRound();
        }

        private void EnsureCapacity()
        {
            if (capacitySet)
                return;
            capacitySet = true;
            budget.Capacity = Config.Tuning.Supply.Capacity(Population);
        }

        private int Population => Player.List.Count(p => p.IsConnected && !p.IsHost);

        internal bool CanIssue(ItemSpec spec, int count) => IssueFailure(spec, count, false) == null;
        internal string IssueFailure(ItemSpec spec, int count, bool ignoreLimits)
        {
            if (count < 1)
                return "The requested quantity must be positive.";
            if (ignoreLimits)
                return null;
            EnsureCapacity();
            int cap = spec.Id == 7102 ? Math.Min(spec.RoundCap, Config.Tuning.Supply.Clusters(Population)) : spec.RoundCap;
            if (spec.Id == 7102 && Population < Config.Tuning.Supply.ClusterMinPlayers)
                return $"Clusters require at least {Config.Tuning.Supply.ClusterMinPlayers} players for normal supply; currently {Population}.";
            if (budget.Count(spec.Id.ToString()) + count > cap)
                return $"{spec.Name} round limit reached ({budget.Count(spec.Id.ToString())}/{cap}).";
            if (budget.Total + count > budget.Capacity)
                return $"Utility supply exhausted ({budget.Total}/{budget.Capacity}); this kit needs {count} slot(s).";

            return null;
        }

        internal bool CanIssueBatch(uint[] ids)
        {
            EnsureCapacity();
            var ours = ids.Where(id => id >= 7101 && id <= 7160).ToArray();
            if (budget.Total + ours.Length > budget.Capacity)
                return false;
            return ours.GroupBy(id => id).All(g => registered.TryGetValue(g.Key, out var item) && CanIssue(item.Spec, g.Count()));
        }

        internal bool Mint(RegisteredItem item, int serial, bool ignoreLimits = false)
        {
            if (IssueFailure(item.Spec, 1, ignoreLimits) != null)
                return false;
            int cap = item.Id == 7102 ? Config.Tuning.Supply.Clusters(Population) : item.Spec.RoundCap;
            if (!budget.TryIssue(item.Id.ToString(), Math.Min(item.Spec.RoundCap, cap), ignoreLimits: ignoreLimits))
                return false;
            charges[serial] = Math.Max(1, item.Spec.Charges);
            return true;
        }

        internal void Unmint(RegisteredItem item, int serial, bool ignoreLimits)
        {
            if (charges.Remove(serial) && !ignoreLimits)
                budget.RevokeIssue(item.Id.ToString());
            item.TrackedSerials.Remove(serial);
        }

        private RegisteredItem Find(int serial) => registered.Values.FirstOrDefault(i => i.TrackedSerials.Contains(serial));
        private IEnumerable<NaturalContent> NaturalContent()
        {
            return registered.Values.Select(item => new NaturalContent { Id = item.Id, Name = item.Name, Body = item.Type, Rarity = item.Spec.Rarity, RequiredSlots = 1, CanIssue = () => CanIssue(item.Spec, 1), Give = p => item.TryGive(p, false, false), AdoptItem = item.AdoptItem, AdoptPickup = item.AdoptPickup, Spawn = position => item.Spawn(position) });
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
            // The Monkey's Paw is flipped like a normal coin; BrainlessCoinflips grants the wish.
            var coin = Find(ev.Item.Serial);
            if (coin == null || coin.Id == SupCatalog.MonkeysPaw)
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
            var used = Find(ev.Item.Serial);
            if (used == null)
                return;
            // Pills use the native animation; only the pre-checks run here.
            if (PillUsing(ev, used))
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
                SupLeave(ev.Player);
                channels.Remove(ev.Player);
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
            SupLeave(ev.Player);
            channels.Remove(ev.Player);
            keyHeld.Remove(ev.Player);
            if (movement.TryGetValue(ev.Player, out var layer))
            {
                layer.Clear();
                movement.Remove(ev.Player);
            }
        }

        private void Hurting(HurtingEventArgs ev)
        {
            if (ev.IsAllowed && ev.Amount > 0 && Config.Sup.InvisibilityPills.BreakOnAttack)
                StopStealth(ev.Attacker);
            if (Config.Tuning.Interaction.CancelOnDamage && ev.IsAllowed && ev.Amount > 0)
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
            var nearby = devices.FirstOrDefault(d => Vector3.Distance(player.Position, d.Position) <= Config.Tuning.Interaction.DeviceReach && Vector3.Dot(player.CameraTransform.forward, (d.Position - player.CameraTransform.position).normalized) > .75f && World.ClearLine(player.CameraTransform.position, d.Position + Vector3.up * .1f));
            if (nearby != null)
            {
                Begin(player, Config.Tuning.DoorBrace.RemoveSeconds, null, hold, () => RemoveDevice(nearby));
                return;
            }

            Item held = player.CurrentItem;
            var item = held == null ? null : Find(held.Serial);
            if (item == null || !player.IsHuman)
                return;
            if (ActivateSup(player, held, item, hold))
                return;
            switch (item.Id)
            {
                case 7105:
                    var door = LookDoor(player);
                    if (door == null)
                        return;
                    UseDoorItem(player, held, item, door, hold);
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

        private Door LookDoor(Player player, float? reach = null)
        {
            foreach (var hit in Physics.RaycastAll(player.CameraTransform.position, player.CameraTransform.forward, reach ?? Config.Tuning.DoorBrace.Reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            {
                if (hit.collider.GetComponentInParent<ReferenceHub>() != null)
                    continue;
                return Exiled.API.Features.Doors.Door.Get(hit.collider.gameObject);
            }

            return null;
        }

        private void Door(InteractingDoorEventArgs ev) => Safe.Run(() =>
        {
            var hacking = ev.Player.CurrentItem == null ? null : Find(ev.Player.CurrentItem.Serial);
            if (hacking?.Id == SupCatalog.CursedKeycard && ev.IsAllowed)
                CursedDoor(ev.Player, ev.Player.CurrentItem, ev.Door);
            if (hacking?.Id == 7119)
            {
                ev.IsAllowed = false;
                HackDoor(ev.Player, ev.Player.CurrentItem, ev.Door, false);
                return;
            }

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

                Begin(ev.Player, Config.Tuning.DoorBrace.RemoveSeconds, null, false, () => RemoveDevice(brace));
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
            if (door.IsOpen || door.IsLocked || Vector3.Distance(player.Position, door.Position) > Config.Tuning.DoorBrace.Reach)
                return;
            if (item.Id == 7105 && !World.OrdinaryDoor(door))
                return;
            Begin(player, Config.Tuning.DoorBrace.DeploySeconds, held, hold, () =>
            {
                if (door.IsLocked || door.IsOpen || Vector3.Distance(player.Position, door.Position) > Config.Tuning.DoorBrace.Reach || !Spend(player, held))
                    return;
                var device = new Device
                {
                    Id = 7105,
                    Door = door,
                    Position = door.Position,
                    Expires = Time.time + Config.Tuning.DoorBrace.DurationSeconds
                };
                device.Marker = World.Marker(lifetime, door.Position + Vector3.up, Color.yellow, new Vector3(.65f, .08f, .08f), PrimitiveType.Cube);
                devices.Add(device);
            });
            PlayAudio(door.Position, Config.Tuning.DoorBrace.DeploySeconds, "beep");
        }

        private void RemoveDevice(Device device)
        {
            devices.Remove(device);
            if (device.Marker != null)
                lifetime.Release(device.Marker);
            if (device.Text != null)
                lifetime.Release(device.Text);
        }

        // SCP-079 cannot open a door held by a Door Jammer.
        private void ComputerDoor(Exiled.Events.EventArgs.Scp079.TriggeringDoorEventArgs ev)
        {
            if (devices.Any(d => d.Id == 7105 && d.Door == ev.Door))
                ev.IsAllowed = false;
        }

        private void Thrown(ThrownProjectileEventArgs ev)
        {
            if (Config.Sup.InvisibilityPills.BreakOnAttack)
                StopStealth(ev.Player);
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
            if (ArmCursed(item, projectile, owner))
                return;
            if (item.Id == 7113 && projectile is ExplosionGrenadeProjectile impact)
            {
                var tuning = Config.Tuning.ImpactGrenade;
                impact.FuseTime = tuning.SafetyFuseSeconds;
                fragments.Track(impact, new DamageSource(owner), tuning.Radius, tuning.HumanDamage, tuning.ScpDamage, requireLineOfSight: tuning.RequireLineOfSight, affectOwner: tuning.AffectOwner, armorPenetration: tuning.ArmorPenetration);
                impact.GameObject.AddComponent<ImpactGrenade>().Initialize(impact, owner?.ReferenceHub, tuning);
                return;
            }

            if (item.Id == 7102 && projectile is ExplosionGrenadeProjectile parent && !Config.Tuning.ClusterGrenade.NativeParentBlast)
                parent.MaxRadius = Config.Tuning.ClusterGrenade.ParentRadius;
            thrown.Arm(projectile.Serial, new Payload { Item = item, Projectile = projectile, Source = new DamageSource(owner), Foam = Config.Tuning.ContainmentFoam, Gas = Config.Tuning.GasGrenade, Cluster = Config.Tuning.ClusterGrenade });
            if (projectile is TimeGrenadeProjectile timed)
                timed.FuseTime = item.Id == 7101 ? Config.Tuning.GasGrenade.FuseSeconds : item.Id == 7102 ? Config.Tuning.ClusterGrenade.FuseSeconds : Config.Tuning.ContainmentFoam.FuseSeconds;
        }

        private void Exploding(ExplodingGrenadeEventArgs ev) => Safe.Run(() =>
        {
            CursedFlashExploding(ev);
            if (fragments.Exploding(ev))
            {
                RetireSerial(ev.Projectile.Serial);
                return;
            }

            if (ev.Projectile == null || !thrown.TryGet(ev.Projectile.Serial, out var payload) || payload.Item.Id != 7102)
                return;
            bool allowed = ev.IsAllowed;
            var targets = ev.TargetsToAffect.ToArray();
            if (payload.Cluster.NativeParentBlast)
                targets = Array.Empty<Player>();
            else
            {
                ev.IsAllowed = allowed && payload.Cluster.DamageEnvironment;
                if (ev.IsAllowed)
                    ev.TargetsToAffect.Clear();
            }
            // HE grenades reach this callback after native visual/target setup.
            thrown.Resolve(ev.Projectile.Serial, allowed, p => Cluster(ev.Position, p.Source, p.Cluster, targets), p => RetireSerial(ev.Projectile.Serial));
        });
        private void GrenadeEffect(SpawningGrenadeEffectEventArgs ev) => Safe.Run(() =>
        {
            if (ev.Projectile == null || !thrown.TryGet(ev.Projectile.Serial, out var payload))
                return;
            if (payload.Item.Id == 7102 && ev.IsAllowed)
                return;
            bool allowed = ev.IsAllowed;
            ev.IsAllowed = false;
            // ProjectileExploding is raised BEFORE native effects and before
            // EXILED's flashbang ExplodingGrenade callback. Suppressing it and
            // waiting for the latter silently prevented smoke from ever spawning.
            // Resolve every utility grenade here, without a native flash or bang.
            thrown.Resolve(ev.Projectile.Serial, allowed, p => DetonateUtility(p, ev.Position), p =>
            {
                RetireSerial(ev.Projectile.Serial);
                // The cancelled native path does not destroy its projectile.
                // Network destruction is safe here: native fuse handling returns
                // immediately after the cancelled event, without reading it again.
                if (p.Projectile.Base != null)
                    p.Projectile.Destroy();
            });
        });
        private void DetonateUtility(Payload payload, Vector3 position)
        {
            switch (payload.Item.Id)
            {
                case 7101:
                    for (int i = 0; i < payload.Gas.SmokeEmitters; i++)
                        NativeSmoke.Spawn(Ground(position) + Vector3.up * .08f + Vector3.right * (i % 2 == 0 ? .08f : -.08f), payload.Gas.DurationSeconds);
                    if (Config.Debug)
                        Log.Info($"[Smoke 7101] Serial {payload.Projectile.Serial}: {payload.Gas.SmokeEmitters} full-size native emitters for {payload.Gas.DurationSeconds}s.");
                    break;
                case 7103:
                    // Spawn the puddles one tick later, outside the native grenade callback.
                    var foam = payload.Foam;
                    lifetime.Later(0, () => SpreadTantrum(position, foam));
                    break;
                default:
                    DetonateGeneral(payload, position);
                    break;
            }
        }

        // Containment foam leaves SCP-173's native tantrum mess across a wide area:
        // by default one puddle scaled to the radius; with single_puddle off, a grid
        // of normal puddles on solid floor with a clear line back to the blast.
        // A tantrum hazard that spawns on top of a player crashed the server, so puddles
        // never spawn where anyone (alive) is standing. They can still walk into one later,
        // exactly like SCP-173's own tantrum.
        private static bool PlayerInside(Vector3 floor, float radius) => Player.List.Any(p => p.IsAlive && Math.Abs(p.Position.y - floor.y) < 2.5f && new Vector2(p.Position.x - floor.x, p.Position.z - floor.z).sqrMagnitude <= radius * radius);

        private void SpreadTantrum(Vector3 position, FoamTuning tuning)
        {
            float clearance = Math.Max(1.5f, TantrumHazard.TantrumPrefab.MaxDistance + .75f);
            if (tuning.SinglePuddle && !PlayerInside(Ground(position), tuning.Radius + .75f))
            {
                PlaceScaledTantrum(Ground(position), tuning);
                return;
            }

            Vector3 center = Ground(position);
            int placed = 0;
            float spacing = Math.Max(.5f, tuning.Spacing);
            int steps = (int)Math.Ceiling(tuning.Radius / spacing);
            var points = new List<Vector3>();
            for (int x = -steps; x <= steps; x++)
                for (int z = -steps; z <= steps; z++)
                {
                    var offset = new Vector3(x * spacing + (z % 2 == 0 ? 0 : spacing / 2), 0, z * spacing * .87f);
                    if (offset.magnitude <= tuning.Radius)
                        points.Add(offset);
                }

            foreach (var offset in points.OrderBy(o => o.sqrMagnitude))
            {
                if (placed >= tuning.MaxPuddles)
                    break;
                Vector3 probe = center + offset + Vector3.up * .5f;
                if (!World.ClearLine(center + Vector3.up * .5f, probe))
                    continue;
                Vector3 floor = Ground(probe);
                if (Math.Abs(floor.y - center.y) > 1.5f || PlayerInside(floor, clearance))
                    continue;
                SpawnTantrum(floor, tuning.DurationSeconds, 1, null);
                placed++;
            }

            World.Cue(center, "Containment foam - sticky floor", 10);
        }

        // One SCP-173 tantrum stretched to the configured radius: the effect area
        // (MaxDistance) and the model's horizontal scale grow together. The scale is
        // set before the hazard spawns, so clients receive it in the spawn message.
        private void PlaceScaledTantrum(Vector3 floor, FoamTuning tuning)
        {
            var prefab = TantrumHazard.TantrumPrefab;
            float factor = tuning.Radius / Math.Max(.1f, prefab.MaxDistance);
            SpawnTantrum(floor, tuning.DurationSeconds, factor, tuning.Radius);
            World.Cue(floor, "Containment foam - sticky floor", 10);
        }

        // Same path as SCP-173's own tantrum: instantiate the prefab, set the synced
        // position 1.25 m above the floor and let the hazard spawn itself once in
        // Start(). EXILED's PlaceTantrum spawns it twice, which crashed clients.
        private void SpawnTantrum(Vector3 floor, float duration, float widthFactor, float? radius)
        {
            var puddle = UnityEngine.Object.Instantiate(TantrumHazard.TantrumPrefab);
            if (widthFactor != 1)
            {
                Vector3 scale = puddle.transform.localScale;
                puddle.transform.localScale = new Vector3(scale.x * widthFactor, scale.y, scale.z * widthFactor);
            }

            if (radius.HasValue)
                puddle.MaxDistance = radius.Value;
            puddle.SynchronizedPosition = new RelativePositioning.RelativePosition(floor + Vector3.up * 1.25f);
            puddle.HazardDuration = duration;
            puddles.Add(puddle);
        }

        private void PlayAudio(Vector3 position, float seconds, string kind)
        {
            var audio = Config.Tuning.Audio;
            FxAudio.Play(lifetime, position, seconds, kind, volume: audio.Volume, range: audio.Range, fullVolumeRange: audio.FullVolumeRange);
        }

        // With NativeParentBlast the game already dealt the first blast; targets is empty.
        private void Cluster(Vector3 position, DamageSource source, ClusterTuning tuning, IEnumerable<Player> targets)
        {
            var blastBudget = new BlastBudget(tuning.SharedHumanDamageCap, tuning.SharedScpDamageCap);
            foreach (var target in targets.Where(source.CanAffect).ToArray())
            {
                if ((!tuning.AffectOwner && target == source.Owner) || (tuning.RequireLineOfSight && !FragProjectiles.ClearBlast(position, target)))
                    continue;
                float amount = Rules.Falloff(Vector3.Distance(position, target.Position), tuning.ParentRadius, target.IsScp ? tuning.ParentScpDamage : tuning.ParentHumanDamage);
                source.Blast(target, position, blastBudget.Take(target.UserId, target.IsScp, amount), tuning.ArmorPenetration);
            }

            float rotation = UnityEngine.Random.Range(0, Mathf.PI * 2);
            // All configured children leave the first detonation together. Each has native
            // rigidbody bounce, gravity, ticking fuse and explosion effects.
            for (int i = 0; i < tuning.GrenadeCount; i++)
            {
                float angle = rotation + i * Mathf.PI * 2 / tuning.GrenadeCount;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 start = position + Vector3.up * .15f;
                Vector3 separated = start + outward * .25f;
                if (!Physics.Linecast(start, separated, InventorySystem.Items.ThrowableProjectiles.ExplosionGrenade.HitBlockerMask, QueryTriggerInteraction.Ignore))
                    start = separated;
                fragments.Launch(start, outward * tuning.HorizontalSpeed + Vector3.up * tuning.UpwardSpeed, tuning.ChildFuseSeconds + i * tuning.ChildFuseStaggerSeconds, source, tuning.Radius, tuning.HumanDamage, tuning.ScpDamage, blastBudget, tuning.RequireLineOfSight, tuning.AffectOwner, tuning.ArmorPenetration, tuning.DamageEnvironment, nativeScale: tuning.ChildNativeDamagePercent / 100f);
            }
        }

        private void RetireSerial(int serial)
        {
            Find(serial)?.TrackedSerials.Remove(serial);
            charges.Remove(serial);
        }

        private Vector3 Ground(Vector3 point)
        {
            foreach (var hit in Physics.RaycastAll(point + Vector3.up, Vector3.down, 4, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
                if (hit.collider.GetComponentInParent<ReferenceHub>() == null && hit.collider.GetComponentInParent<InventorySystem.Items.Pickups.ItemPickupBase>() == null && hit.collider.GetComponentInParent<AdminToys.AdminToyBase>() == null)
                    return hit.point;
            return point;
        }

        private IEnumerator<float> Tick()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(Config.Tuning.Interaction.TickSeconds);
                Safe.Run(Update);
            }
        }

        private void Update()
        {
            fragments.Prune();
            NativeSmoke.Prune();
            UpdateTrackers();
            foreach (var pair in channels.ToArray())
            {
                var p = pair.Key;
                var c = pair.Value;
                if (!p.IsConnected || !p.IsAlive || p.Role.Base != c.Role || p.IsCuffed || (c.Hold && !keyHeld.Contains(p)) || (c.Item != null && p.CurrentItem != c.Item) || Vector3.Distance(p.Position, c.Position) > Config.Tuning.Interaction.MoveTolerance)
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
            puddles.RemoveAll(p => p == null);
        }

        internal bool ReloadTuning(out string response)
        {
            try
            {
                var next = ConfigReload.Read(this);
                next.Prepare();
                LoadIndividualItems(next);
                next.Prepare();
                if (!next.IsEnabled || next.ToolKeybindId != Config.ToolKeybindId || next.Items.Count != Config.Items.Count || next.Items.Any(i => !Config.Items.Any(old => old.Id == i.Id && old.BaseType == i.BaseType && old.Enabled == i.Enabled)))
                    throw new InvalidOperationException("Enabled state, keybind IDs, item IDs, native bodies and registration changes require a server restart. Tuning was not changed.");
                if (next.Items.Where(i => registered.ContainsKey(i.Id)).Any(i => Exiled.CustomItems.API.Features.CustomItem.Registered.Any(other => !registered.Values.Contains(other) && other.Name == i.Name)))
                    throw new InvalidOperationException("An item name conflicts with another plugin.");
                ConfigReload.Apply(Config, next);
                foreach (var item in registered.Values)
                    item.Retune(Config.Items.Single(i => i.Id == item.Id));
                if (capacitySet)
                    budget.Capacity = Config.Tuning.Supply.Capacity(Population);
                updates?.Dispose();
                updates = Brainless.Internal.Updates.PluginUpdates.Start(GetType().Assembly, "Items", Config.AutoUpdate);
                response = "Item tuning reloaded. New throws/deployments use it; existing fields and charges keep their state. No DLL or supply reset.";
                return true;
            }
            catch (Exception exception)
            {
                response = "Item reload rejected; active settings retained. " + exception.Message;
                return false;
            }
        }

        private ItemType Conversion(uint id, ItemType type) => id == 7105 ? ItemType.Coin : type;
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
            Native914.ProcessPickup(replacement.Base, true, ev.KnobSetting);
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
            Native914.ProcessInventory(replacement.Base, ev.KnobSetting);
        });
        private sealed class Payload
        {
            internal RegisteredItem Item;
            internal Projectile Projectile;
            internal DamageSource Source;
            internal FoamTuning Foam;
            internal GasTuning Gas;
            internal ClusterTuning Cluster;
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
            internal Primitive Marker;
            internal Text Text;
        }
    }
}
