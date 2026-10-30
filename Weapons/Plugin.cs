using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Doors;
using Exiled.API.Interfaces;
using Exiled.API.Enums;
using Exiled.CustomItems.API;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp914;
using Exiled.Events.EventArgs.Map;
using MEC;
using UnityEngine;
using P = Exiled.Events.Handlers.Player;
using S = Exiled.Events.Handlers.Server;
using I = Exiled.Events.Handlers.Item;
using U = Exiled.Events.Handlers.Scp914;
using M = Exiled.Events.Handlers.Map;
using FxAudio = Brainless.Internal.Audio;

namespace Brainless.Weapons
{
    public sealed class Plugin : Plugin<Config>
    {
        public override string Name => "BrainlessWeapons";
        public override string Prefix => "brainless_weapons";
        public override string Author => "Brainless Project";
        public override Version Version => new Version(0, 1, 3);
        public override Version RequiredExiledVersion => new Version(9, 15, 2);
        public override PluginPriority Priority => PluginPriority.Low;

        private readonly Lifetime lifetime = new Lifetime();
        private readonly LimitedBudget budget = new LimitedBudget();
        private readonly ShotLedger shots = new ShotLedger();
        private readonly FragProjectiles grenades = new FragProjectiles();
        private readonly Dictionary<uint, RegisteredWeapon> registered = new Dictionary<uint, RegisteredWeapon>();
        private readonly Dictionary<int, WeaponState> states = new Dictionary<int, WeaponState>();
        private readonly Dictionary<Player, float> tagImmunity = new Dictionary<Player, float>();
        private readonly Dictionary<Door, DoorProgress> doors = new Dictionary<Door, DoorProgress>();
        private bool capacitySet, reflected;
        private Brainless.Internal.Updates.PluginUpdates updates;
        public override void OnEnabled()
        {
            Config.UpgradeCatalog();
            var enabled = Config.Weapons.Where(w => w.Enabled && (!w.Experimental || Config.AllowExperimentalWeapons)).ToArray();
            if (enabled.Select(s => s.Id).Distinct().Count() != enabled.Length || enabled.Select(s => s.Name).Distinct().Count() != enabled.Length)
                throw new InvalidOperationException("Weapon IDs and names must be unique.");
            if (enabled.Any(s => Exiled.CustomItems.API.Features.CustomItem.Registered.Any(i => i.Id == s.Id || i.Name == s.Name)))
                throw new InvalidOperationException("A configured weapon ID or name is already registered by another plugin.");
            foreach (var spec in Config.Weapons.Where(w => w.Enabled && (!w.Experimental || Config.AllowExperimentalWeapons)))
            {
                if (Exiled.CustomItems.API.Features.CustomItem.Registered.Any(i => i.Id == spec.Id))
                    throw new InvalidOperationException($"Custom ID {spec.Id} already registered.");
                var weapon = new RegisteredWeapon(this, spec);
                weapon.Register();
                registered.Add(spec.Id, weapon);
            }

            P.Shooting += Shooting;
            P.Shot += Shot;
            P.Hurting += Hurting;
            P.ReloadingWeapon += Reload;
            P.UnloadingWeapon += Unload;
            P.ItemAdded += Added;
            P.ChangedItem += Changed;
            M.ExplodingGrenade += Exploding;
            I.ChangingAttachments += Attachments;
            U.UpgradingPickup += UpgradePickup;
            U.UpgradingInventoryItem += UpgradeInventory;
            S.WaitingForPlayers += Reset;
            S.RoundEnded += Ended;
            S.RoundStarted += RoundStarted;
            Reset();
            AdminItems.Enable("brainless.weapons.commands");
            base.OnEnabled();
            updates = Brainless.Internal.Updates.PluginUpdates.Start(GetType().Assembly, "Weapons", Config.AutoUpdate);
        }

        public override void OnDisabled()
        {
            updates?.Dispose();
            updates = null;
            P.Shooting -= Shooting;
            P.Shot -= Shot;
            P.Hurting -= Hurting;
            P.ReloadingWeapon -= Reload;
            P.UnloadingWeapon -= Unload;
            P.ItemAdded -= Added;
            P.ChangedItem -= Changed;
            M.ExplodingGrenade -= Exploding;
            I.ChangingAttachments -= Attachments;
            U.UpgradingPickup -= UpgradePickup;
            U.UpgradingInventoryItem -= UpgradeInventory;
            S.WaitingForPlayers -= Reset;
            S.RoundEnded -= Ended;
            S.RoundStarted -= RoundStarted;
            AdminItems.Disable();
            lifetime.Clear();
            grenades.Clear();
            foreach (var weapon in registered.Values)
                weapon.Unregister();
            registered.Clear();
            base.OnDisabled();
        }

        private void Reset()
        {
            lifetime.Clear();
            grenades.Clear();
            states.Clear();
            shots.Clear();
            tagImmunity.Clear();
            doors.Clear();
            foreach (var item in registered.Values)
                item.TrackedSerials.Clear();
            budget.Reset(0);
            capacitySet = false;
            reflected = false;
        }

        private void Ended(Exiled.Events.EventArgs.Server.RoundEndedEventArgs ev) => Reset();
        private void EnsureCapacity()
        {
            if (!capacitySet)
            {
                capacitySet = true;
                budget.Capacity = Rules.WeaponCap(Player.List.Count(p => p.IsConnected && !p.IsHost));
            }
        }

        private void RoundStarted()
        {
            EnsureCapacity();
            if (!Config.WorldLoot)
                return;
            lifetime.Later(1, () =>
            {
                var room = Room.List.FirstOrDefault(r => r.Zone == ZoneType.HeavyContainment && r.Type.ToString().IndexOf("Armory", StringComparison.OrdinalIgnoreCase) >= 0);
                var available = registered.Values.Where(w => w.Id != 7205 && CanIssue(w.Spec, 1)).ToArray();
                if (room != null && available.Length > 0)
                    available[UnityEngine.Random.Range(0, available.Length)].Spawn(room.Position + Vector3.up);
            });
        }

        internal bool CanIssue(WeaponSpec spec, int count) => IssueFailure(spec, count, false) == null;
        internal string IssueFailure(WeaponSpec spec, int count, bool ignoreLimits)
        {
            if (count < 1)
                return "The requested quantity must be positive.";
            if (ignoreLimits)
                return null;
            EnsureCapacity();
            if (budget.Count(spec.Id.ToString()) + count > spec.RoundCap)
                return $"{spec.Name} round limit reached ({budget.Count(spec.Id.ToString())}/{spec.RoundCap}).";
            if (budget.Total + count > budget.Capacity)
                return $"Custom firearm supply exhausted ({budget.Total}/{budget.Capacity} this round).";
            return null;
        }

        internal bool CanIssueBatch(uint[] ids)
        {
            EnsureCapacity();
            var ours = ids.Where(i => registered.ContainsKey(i)).ToArray();
            if (budget.Total + ours.Length > budget.Capacity)
                return false;
            return ours.GroupBy(i => i).All(g => registered.TryGetValue(g.Key, out var item) && CanIssue(item.Spec, g.Count()));
        }

        internal bool Mint(RegisteredWeapon definition, Firearm firearm, bool ignoreLimits = false)
        {
            if (IssueFailure(definition.Spec, 1, ignoreLimits) != null || !budget.TryIssue(definition.Id.ToString(), definition.Spec.RoundCap, ignoreLimits: ignoreLimits))
                return false;
            states[firearm.Serial] = new WeaponState
            {
                Definition = definition,
                LastHeat = Time.time
            };
            int magazine = definition.Id == 7207 ? 16 : definition.Spec.Magazine;
            if (magazine > 0)
            {
                firearm.MaxMagazineAmmo = magazine;
                firearm.MagazineAmmo = magazine;
                firearm.BarrelAmmo = 0;
            }

            if (definition.Id == 7205)
                shots.Register(firearm.Serial, 6);
            else if (definition.Id == 7207)
                shots.Register(firearm.Serial, 16);
            return true;
        }

        private WeaponState Find(Item item) => item != null && states.TryGetValue(item.Serial, out var state) ? state : null;
        internal void Unmint(RegisteredWeapon item, int serial, bool ignoreLimits)
        {
            if (states.Remove(serial) && !ignoreLimits)
                budget.RevokeIssue(item.Id.ToString());
            shots.Forget(serial);
            item.TrackedSerials.Remove(serial);
        }

        private WeaponState Find(int serial) => states.TryGetValue(serial, out var state) ? state : null;
        private void Added(ItemAddedEventArgs ev)
        {
            if (ev.Item is Firearm firearm)
                Safe.Run(() => Clamp(firearm));
        }

        private void Changed(ChangedItemEventArgs ev)
        {
            var state = Find(ev.Item);
            if (state == null)
                return;
            if (ev.Item is Firearm firearm)
                Clamp(firearm);
            ev.Player.ShowHint($"<b>{state.Definition.Name}</b>\n{state.Definition.Description}", 5);
        }

        private void Clamp(Firearm firearm)
        {
            var state = Find(firearm);
            if (state == null)
                return;
            int capacity = state.Definition.Id == 7207 ? 16 : state.Definition.Spec.Magazine;
            if (capacity > 0)
                firearm.MaxMagazineAmmo = capacity;
            if (FixedAmmo(state))
            {
                int left = shots.Remaining(firearm.Serial);
                int barrel = Math.Min(firearm.BarrelAmmo, left);
                firearm.BarrelAmmo = barrel;
                firearm.MagazineAmmo = Math.Min(firearm.MagazineAmmo, left - barrel);
            }
        }

        private void Reload(ReloadingWeaponEventArgs ev)
        {
            var s = Find(ev.Item);
            if (s == null)
                return;
            if (FixedAmmo(s))
                ev.IsAllowed = false;
            else
                Clamp(ev.Firearm);
        }

        private void Unload(UnloadingWeaponEventArgs ev)
        {
            if (FixedAmmo(Find(ev.Item)))
                ev.IsAllowed = false;
        }

        private void Attachments(Exiled.Events.EventArgs.Item.ChangingAttachmentsEventArgs ev)
        {
            if (FixedAmmo(Find(ev.Item)))
                ev.IsAllowed = false;
        }

        private void Shooting(ShootingEventArgs ev) => Safe.Run(() =>
        {
            var state = Find(ev.Item);
            if (state == null || !ev.IsAllowed)
                return;
            Clamp(ev.Firearm);
            if (FixedAmmo(state) && shots.Remaining(ev.Item.Serial) < 1)
            {
                ev.IsAllowed = false;
                return;
            }

            if (state.Definition.Id == 7207 && Time.time < state.NextLaunch)
            {
                ev.IsAllowed = false;
                ev.Firearm.AutomaticActionModule?.ServerResync();
                return;
            }

            if (state.Definition.Id == 7204)
            {
                Cool(state);
                if (state.VentUntil > Time.time)
                {
                    ev.IsAllowed = false;
                    ev.Player.ShowHint("Redline venting...", 1);
                    return;
                }

                state.Heat = Math.Min(100, state.Heat + 9);
                state.LastFire = Time.time;
                if (state.Heat >= 100)
                {
                    state.VentUntil = Time.time + 3;
                    state.Heat = 0;
                    FxAudio.Play(lifetime, ev.Player.Position, 1, "beep");
                }
            }

            state.Token++;
            state.AmmoBefore = ev.Firearm.MagazineAmmo + ev.Firearm.BarrelAmmo;
            if (FixedAmmo(state))
            {
                // Consume after the native shooting event has finished dispatching.
                int token = state.Token;
                var gun = ev.Firearm;
                bool launcher = state.Definition.Id == 7207;
                Vector3 origin = ev.Player.CameraTransform.position;
                Vector3 direction = ev.Direction.normalized;
                var source = new DamageSource(ev.Player);
                if (launcher)
                    state.NextLaunch = Time.time + Mathf.Clamp(Config.LauncherShotInterval, .2f, 5);
                lifetime.Later(0, () =>
                {
                    if (!ev.IsAllowed || state.ConsumedToken == token)
                        return;
                    state.ConsumedToken = token;
                    shots.Consume(gun.Serial);
                    Clamp(gun);
                    if (launcher)
                    {
                        // Keep the muzzle on the near side of a wall. The grenade
                        // collides/bounces normally after leaving the weapon.
                        Vector3 muzzle = origin + direction * .55f;
                        if (Physics.Raycast(origin, direction, out var hit, .6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                            muzzle = hit.point - direction * .1f;
                        grenades.Launch(muzzle, direction * Mathf.Clamp(Config.LauncherSpeed, 5, 35), Mathf.Clamp(Config.LauncherFuse, .5f, 5), source, Mathf.Clamp(Config.LauncherBlastRadius, .5f, 4), Mathf.Clamp(Config.LauncherHumanDamage, 0, 75), Mathf.Clamp(Config.LauncherScpDamage, 0, 150));
                    }
                });
            }
        });
        private static bool FixedAmmo(WeaponState state) => state != null && (state.Definition.Id == 7205 || state.Definition.Id == 7207);
        private void Exploding(ExplodingGrenadeEventArgs ev) => Safe.Run(() => grenades.Exploding(ev));
        private void Cool(WeaponState state)
        {
            float start = Math.Max(state.LastHeat, state.LastFire + .6f);
            state.Heat = Math.Max(0, state.Heat - Math.Max(0, Time.time - start) * 25);
            state.LastHeat = Time.time;
        }

        private void Hurting(HurtingEventArgs ev) => Safe.Run(() =>
        {
            if (reflected || !ev.IsAllowed || ev.Amount <= 0 || !(ev.DamageHandler.CustomBase is Exiled.API.Features.DamageHandlers.FirearmDamageHandler damage))
                return;
            var state = Find(damage.Item);
            if (state == null)
                return;
            if (state.Definition.Id == 7207)
            {
                ev.IsAllowed = false;
                ev.Amount = 0;
                return;
            }

            if (state.Definition.Id == 7201 && ev.Player.IsScp)
                ev.Amount = Rules.ShieldDamage(ev.Amount, ev.Player.HumeShield, 1.2f, state.Definition.Spec.ScpDamage);
            else
                ev.Amount *= ev.Player.IsScp ? state.Definition.Spec.ScpDamage : state.Definition.Spec.HumanDamage;
            if (state.Definition.Id != 7202 || ev.Attacker == null || !HitboxIdentity.IsEnemy(ev.Attacker.Role.Type, ev.Player.Role.Type))
                return;
            if (tagImmunity.TryGetValue(ev.Player, out float immune) && immune > Time.time)
                return;
            if (!state.Tags.TryGetValue(ev.Player, out var hits) || Time.time - hits.Start > 2)
                state.Tags[ev.Player] = hits = new Tag
                {
                    Start = Time.time
                };
            if (hits.Token == state.Token)
                return;
            hits.Token = state.Token;
            hits.Hits++;
            if (hits.Hits < 3)
                return;
            state.Tags.Remove(ev.Player);
            tagImmunity[ev.Player] = Time.time + 11;
            var target = ev.Player;
            var role = target.Role.Base;
            Vector3 position = target.Position;
            target.ShowHint("Rattler tag - nearby players can hear you for three seconds", 3);
            FxAudio.Play(lifetime, position, 3, "beep", () =>
            {
                if (target.IsConnected && target.IsAlive && target.Role.Base == role)
                    position = target.Position;
                return position;
            });
        });
        private void Shot(ShotEventArgs ev) => Safe.Run(() =>
        {
            var state = Find(ev.Item);
            if (state == null)
                return;
            if (state.Definition.Id == 7207)
            {
                ev.CanHurt = false;
                ev.CanSpawnImpactEffects = false;
                return;
            }

            if (!ev.CanHurt)
                return;
            if (state.Definition.Id == 7203)
                Breach(ev, state);
            if (state.Definition.Id == 7206 && ev.Target == null)
                Bounce(ev);
        });
        private void Breach(ShotEventArgs ev, WeaponState state)
        {
            var door = ev.RaycastHit.collider == null ? null : Door.Get(ev.RaycastHit.collider.gameObject);
            if (!World.OrdinaryDoor(door) || Vector3.Distance(ev.Player.Position, door.Position) > 2 || state.DoorToken == state.Token || state.AmmoBefore < 2)
                return;
            state.DoorToken = state.Token;
            int ammoBefore = state.AmmoBefore;
            var gun = ev.Firearm;
            var player = ev.Player;
            lifetime.Later(.02f, () =>
            {
                if (!player.Items.Contains(gun) || !World.OrdinaryDoor(door))
                    return;
                int spent = Math.Max(0, ammoBefore - gun.MagazineAmmo - gun.BarrelAmmo);
                int extra = Math.Max(0, 2 - spent);
                if (gun.MagazineAmmo + gun.BarrelAmmo < extra)
                    return;
                int fromMagazine = Math.Min(extra, gun.MagazineAmmo);
                gun.MagazineAmmo -= fromMagazine;
                gun.BarrelAmmo -= extra - fromMagazine;
                if (!doors.TryGetValue(door, out var progress) || Time.time - progress.Last > 6)
                    doors[door] = progress = new DoorProgress();
                progress.Hits++;
                progress.Last = Time.time;
                World.Cue(door.Position, $"Door breach {progress.Hits}/3", 10);
                if (progress.Hits >= 3 && door is IDamageableDoor damageable)
                {
                    damageable.Break();
                    doors.Remove(door);
                }
            });
        }

        private void Bounce(ShotEventArgs ev)
        {
            var hit = ev.RaycastHit;
            if (hit.collider == null || !(hit.collider is MeshCollider) || hit.collider.attachedRigidbody != null || Door.Get(hit.collider.gameObject) != null || hit.distance >= 20)
                return;
            ev.CanHurt = false;
            Vector3 origin = hit.point + hit.normal * .03f;
            Vector3 direction = Vector3.Reflect(ev.Player.CameraTransform.forward, hit.normal).normalized;
            foreach (var bounce in Physics.RaycastAll(origin, direction, 20 - hit.distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            {
                var identity = bounce.collider.GetComponentInParent<HitboxIdentity>();
                if (identity == null)
                {
                    Trace(origin, bounce.point);
                    return;
                }

                var target = Player.Get(identity.TargetHub);
                Trace(origin, bounce.point);
                if (target == null || target == ev.Player || !World.CanDamage(ev.Player, target))
                    return;
                var handler = new PlayerStatsSystem.FirearmDamageHandler(ev.Firearm.Base, ev.Damage * .5f, ev.Firearm.Penetration, true)
                {
                    Hitbox = HitboxType.Body
                };
                reflected = true;
                try
                {
                    target.Hurt(handler);
                }
                finally
                {
                    reflected = false;
                }

                return;
            }

            Trace(origin, origin + direction * (20 - hit.distance));
        }

        private void Trace(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < .01f)
                return;
            var marker = World.Marker(lifetime, (from + to) * .5f, Color.yellow, new Vector3(.018f, .018f, delta.magnitude), PrimitiveType.Cube);
            marker.Rotation = Quaternion.LookRotation(delta);
            lifetime.Later(.12f, () => lifetime.Release(marker));
            FxAudio.Play(lifetime, from, .12f, "beep");
        }

        private void UpgradePickup(UpgradingPickupEventArgs ev) => Safe.Run(() =>
        {
            var state = Find(ev.Pickup.Serial);
            if (state == null)
                return;
            ev.IsAllowed = false;
            state.Definition.TrackedSerials.Remove(ev.Pickup.Serial);
            states.Remove(ev.Pickup.Serial);
            if (FixedAmmo(state))
            {
                var pickup = Pickup.CreateAndSpawn(ItemType.Coin, ev.Pickup.Position);
                ev.Pickup.Destroy();
                Scp914.Scp914Upgrader.ProcessPickup(pickup.Base, true, ev.KnobSetting);
            }
            else
            {
                // Keep the original serial and native serialized magazine/chamber data.
                if (ev.Pickup is FirearmPickup gun)
                    gun.MaxAmmo = Item.Create<Firearm>(state.Definition.Type).MaxMagazineAmmo;
                Scp914.Scp914Upgrader.ProcessPickup(ev.Pickup.Base, true, ev.KnobSetting);
            }
        });
        private void UpgradeInventory(UpgradingInventoryItemEventArgs ev) => Safe.Run(() =>
        {
            var state = Find(ev.Item);
            if (state == null)
                return;
            ev.IsAllowed = false;
            Item replacement;
            state.Definition.TrackedSerials.Remove(ev.Item.Serial);
            states.Remove(ev.Item.Serial);
            if (FixedAmmo(state))
            {
                replacement = Item.Create(ItemType.Coin);
                ev.Player.RemoveItem(ev.Item);
                ev.Player.AddItem(replacement);
            }
            else
            {
                replacement = ev.Item;
                if (replacement is Firearm gun)
                    gun.MaxMagazineAmmo = Item.Create<Firearm>(state.Definition.Type).MaxMagazineAmmo;
            }

            if (Scp914.Scp914Upgrader.TryGetProcessor(replacement.Type, out var processor))
            {
                var result = processor.UpgradeInventoryItem(ev.KnobSetting, replacement.Base);
                Scp914.Processors.Scp914ItemProcessor.ProcessResultingItem(replacement.Base, result, ev.KnobSetting);
            }
        });
        private sealed class WeaponState
        {
            internal RegisteredWeapon Definition;
            internal float Heat, LastFire, LastHeat, VentUntil, NextLaunch;
            internal int Token, ConsumedToken, DoorToken, AmmoBefore;
            internal Dictionary<Player, Tag> Tags = new Dictionary<Player, Tag>();
        }

        private sealed class Tag
        {
            internal float Start;
            internal int Hits, Token = -1;
        }

        private sealed class DoorProgress
        {
            internal int Hits;
            internal float Last;
        }
    }
}
