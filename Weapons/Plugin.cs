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
    public sealed partial class Plugin : Plugin<Config>
    {
        public override string Name => "BrainlessWeapons";
        public override string Prefix => "brainless_weapons";
        public override string Author => "Brainless Project";
        public override Version Version => new Version(0, 7, 0);
        public override Version RequiredExiledVersion => new Version(9, 15, 3);
        public override PluginPriority Priority => PluginPriority.Low;
        public static Plugin Instance { get; private set; }

        private readonly Lifetime lifetime = new Lifetime();
        private readonly LimitedBudget budget = new LimitedBudget();
        private readonly ShotLedger shots = new ShotLedger();
        private readonly FragProjectiles grenades = new FragProjectiles();
        private readonly Dictionary<uint, RegisteredWeapon> registered = new Dictionary<uint, RegisteredWeapon>();
        private readonly Dictionary<int, WeaponState> states = new Dictionary<int, WeaponState>();
        private readonly Dictionary<Door, DoorProgress> doors = new Dictionary<Door, DoorProgress>();
        private bool capacitySet, reflected;
        private IDisposable updates;
        private NaturalDistribution distribution;
        public override void OnEnabled()
        {
            Config.Prepare();
            LoadIndividualWeapons(Config);
            Config.Prepare();
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
            Instance = this;
            Reset();
            AdminItems.Enable("brainless.weapons.commands");
            distribution = new NaturalDistribution("Weapons", lifetime, () => Config.Distribution, NaturalContent, () => Config.WorldLoot ? Config.Tuning.Supply.WorldLootCount : 0, () => Config.Debug);
            base.OnEnabled();
            updates = Brainless.Internal.Updates.PluginUpdates.Start(GetType().Assembly, "Weapons", Config.AutoUpdate);
        }

        public override void OnDisabled()
        {
            Instance = null;
            distribution?.Dispose();
            distribution = null;
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
            RailReset();
            lifetime.Clear();
            grenades.Clear();
            foreach (var weapon in registered.Values)
                weapon.Unregister();
            registered.Clear();
            base.OnDisabled();
        }

        private void Reset()
        {
            distribution?.Reset();
            RailReset();
            lifetime.Clear();
            grenades.Clear();
            states.Clear();
            shots.Clear();
            doors.Clear();
            foreach (var item in registered.Values)
                item.TrackedSerials.Clear();
            budget.Reset(0);
            capacitySet = false;
            reflected = false;
            lifetime.Run(CleanupProjectiles());
        }

        private IEnumerator<float> CleanupProjectiles()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(1);
                Safe.Run(grenades.Prune);
            }
        }

        private void Ended(Exiled.Events.EventArgs.Server.RoundEndedEventArgs ev) => Reset();
        private void EnsureCapacity()
        {
            if (!capacitySet)
            {
                capacitySet = true;
                budget.Capacity = Config.Tuning.Supply.Capacity(Player.List.Count(p => p.IsConnected && !p.IsHost));
            }
        }

        private void RoundStarted()
        {
            EnsureCapacity();
            distribution?.RoundStarted();
        }

        private IEnumerable<NaturalContent> NaturalContent() => registered.Values.Select(item => new NaturalContent { Id = item.Id, Name = item.Name, Body = item.Type, CanIssue = () => CanIssue(item.Spec, 1), Give = p => item.TryGive(p, false, false), AdoptItem = item.AdoptItem, AdoptPickup = item.AdoptPickup, Spawn = position => item.Spawn(position) });
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
            if (!MintSerial(definition, firearm.Serial, ignoreLimits))
                return false;
            int magazine = definition.Spec.Magazine;
            if (magazine > 0)
            {
                firearm.MaxMagazineAmmo = magazine;
                firearm.MagazineAmmo = magazine;
                firearm.BarrelAmmo = 0;
            }

            return true;
        }

        internal bool MintSerial(RegisteredWeapon definition, int serial, bool ignoreLimits = false)
        {
            if (IssueFailure(definition.Spec, 1, ignoreLimits) != null || !budget.TryIssue(definition.Id.ToString(), definition.Spec.RoundCap, ignoreLimits: ignoreLimits))
                return false;
            states[serial] = new WeaponState
            {
                Definition = definition,
                InitialCapacity = definition.Spec.Magazine
            };
            if (definition.Id == 7207 || definition.Id == 7208)
                shots.Register(serial, definition.Spec.Magazine);
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
            int capacity = FixedAmmo(state) ? state.InitialCapacity : state.Definition.Spec.Magazine;
            if (state.Definition.Id == 7207)
                capacity = LauncherRules.Capacity(capacity);
            if (capacity > 0)
                firearm.MaxMagazineAmmo = capacity;
            if (FixedAmmo(state))
            {
                int left = Math.Min(shots.Remaining(firearm.Serial), capacity);
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
            grenades.Prune();
            var state = Find(ev.Item);
            if (state == null || !ev.IsAllowed)
                return;
            if (state.Definition.Id == 7208)
            {
                StartRailgun(ev, state);
                return;
            }

            Clamp(ev.Firearm);
            if (FixedAmmo(state) && shots.Remaining(ev.Item.Serial) < 1)
            {
                ev.IsAllowed = false;
                return;
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
                lifetime.Later(0, () =>
                {
                    if (!ev.IsAllowed || state.ConsumedToken == token)
                        return;
                    state.ConsumedToken = token;
                    if (!shots.Consume(gun.Serial))
                    {
                        Clamp(gun);
                        return;
                    }

                    Clamp(gun);
                    if (launcher)
                    {
                        // Keep the muzzle on the near side of a wall. The grenade
                        // collides/bounces normally after leaving the weapon.
                        Vector3 muzzle = origin + direction * .55f;
                        if (Physics.Raycast(origin, direction, out var hit, .6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                            muzzle = hit.point - direction * .1f;
                        grenades.Launch(muzzle, direction * Config.LauncherSpeed, Config.LauncherFuse, source, Config.LauncherBlastRadius, Config.LauncherHumanDamage, Config.LauncherScpDamage, requireLineOfSight: Config.LauncherRequireLineOfSight, affectOwner: Config.LauncherAffectOwner, armorPenetration: Config.LauncherArmorPenetration, damageEnvironment: Config.LauncherDamageEnvironment);
                        if (shots.Remaining(gun.Serial) == 0)
                            lifetime.Later(.5f, () => DiscardEmptyLauncher(gun.Serial));
                    }
                });
            }
        });
        private static bool FixedAmmo(WeaponState state) => state != null && (state.Definition.Id == 7207 || state.Definition.Id == 7208);
        private void Exploding(ExplodingGrenadeEventArgs ev) => Safe.Run(() => grenades.Exploding(ev));
        private void Hurting(HurtingEventArgs ev) => Safe.Run(() =>
        {
            if (reflected || !ev.IsAllowed || ev.Amount <= 0 || !(ev.DamageHandler.CustomBase is Exiled.API.Features.DamageHandlers.FirearmDamageHandler damage))
                return;
            var state = Find(damage.Item);
            if (state == null)
                return;
            if (state.Definition.Id == 7207 || state.Definition.Id == 7208)
            {
                ev.IsAllowed = false;
                ev.Amount = 0;
                return;
            }

            if (state.Definition.Id == 7201 && ev.Player.IsScp)
                ev.Amount = Rules.ShieldDamage(ev.Amount, ev.Player.HumeShield, Config.Tuning.Nullifier.ShieldDamageMultiplier, state.Definition.Spec.ScpDamage);
            else
                ev.Amount *= ev.Player.IsScp ? state.Definition.Spec.ScpDamage : state.Definition.Spec.HumanDamage;
        });
        private void Shot(ShotEventArgs ev) => Safe.Run(() =>
        {
            var state = Find(ev.Item);
            if (state == null)
                return;
            if (state.Definition.Id == 7207 || state.Definition.Id == 7208)
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
            var tuning = Config.Tuning.BreachShotgun;
            if (!World.OrdinaryDoor(door) || Vector3.Distance(ev.Player.Position, door.Position) > tuning.Reach || state.DoorToken == state.Token || state.AmmoBefore < tuning.ShellsPerCycle)
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
                int extra = Math.Max(0, tuning.ShellsPerCycle - spent);
                if (gun.MagazineAmmo + gun.BarrelAmmo < extra)
                    return;
                int fromMagazine = Math.Min(extra, gun.MagazineAmmo);
                gun.MagazineAmmo -= fromMagazine;
                gun.BarrelAmmo -= extra - fromMagazine;
                if (!doors.TryGetValue(door, out var progress) || Time.time - progress.Last > tuning.ProgressResetSeconds)
                    doors[door] = progress = new DoorProgress();
                progress.Hits++;
                progress.Last = Time.time;
                World.Cue(door.Position, $"Door breach {progress.Hits}/{tuning.CyclesToBreach}", 10);
                if (progress.Hits >= tuning.CyclesToBreach && door is IDamageableDoor damageable)
                {
                    damageable.Break();
                    doors.Remove(door);
                }
            });
        }

        private void Bounce(ShotEventArgs ev)
        {
            var hit = ev.RaycastHit;
            var tuning = Config.Tuning.Ricochet;
            if (hit.collider == null || !(hit.collider is MeshCollider) || hit.collider.attachedRigidbody != null || Door.Get(hit.collider.gameObject) != null || hit.distance >= tuning.Range)
                return;
            ev.CanHurt = false;
            Vector3 origin = hit.point + hit.normal * .03f;
            Vector3 direction = Vector3.Reflect(ev.Player.CameraTransform.forward, hit.normal).normalized;
            foreach (var bounce in Physics.RaycastAll(origin, direction, tuning.Range - hit.distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
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
                var handler = new PlayerStatsSystem.FirearmDamageHandler(ev.Firearm.Base, ev.Damage * tuning.BounceDamageMultiplier, ev.Firearm.Penetration, true)
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

            Trace(origin, origin + direction * (tuning.Range - hit.distance));
        }

        private void Trace(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < .01f)
                return;
            var marker = World.Marker(lifetime, (from + to) * .5f, Color.yellow, new Vector3(.018f, .018f, delta.magnitude), PrimitiveType.Cube);
            marker.Rotation = Quaternion.LookRotation(delta);
            lifetime.Later(Config.Tuning.Ricochet.TracerSeconds, () => lifetime.Release(marker));
            PlayAudio(from, Config.Tuning.Ricochet.TracerSeconds, "beep");
        }

        private void PlayAudio(Vector3 position, float seconds, string kind)
        {
            var audio = Config.Tuning.Audio;
            FxAudio.Play(lifetime, position, seconds, kind, volume: audio.Volume, range: audio.Range, fullVolumeRange: audio.FullVolumeRange);
        }

        internal bool ReloadTuning(out string response)
        {
            try
            {
                var next = ConfigReload.Read(this);
                next.Prepare();
                LoadIndividualWeapons(next);
                next.Prepare();
                if (!next.IsEnabled || next.AllowExperimentalWeapons != Config.AllowExperimentalWeapons || next.Weapons.Count != Config.Weapons.Count || next.Weapons.Any(w => !Config.Weapons.Any(old => old.Id == w.Id && old.BaseType == w.BaseType && old.Enabled == w.Enabled && old.Experimental == w.Experimental)))
                    throw new InvalidOperationException("Enabled/experimental registration, IDs and native-body changes require a server restart. Tuning was not changed.");
                if (next.Weapons.Where(w => registered.ContainsKey(w.Id)).Any(w => Exiled.CustomItems.API.Features.CustomItem.Registered.Any(other => !registered.Values.Contains(other) && other.Name == w.Name)))
                    throw new InvalidOperationException("A weapon name conflicts with another plugin.");
                ConfigReload.Apply(Config, next);
                foreach (var weapon in registered.Values)
                    weapon.Retune(Config.Weapons.Single(w => w.Id == weapon.Id));
                if (capacitySet)
                    budget.Capacity = Config.Tuning.Supply.Capacity(Player.List.Count(p => p.IsConnected && !p.IsHost));
                updates?.Dispose();
                updates = Brainless.Internal.Updates.PluginUpdates.Start(GetType().Assembly, "Weapons", Config.AutoUpdate);
                response = "Weapon tuning reloaded. Existing finite weapons retain their original shot budgets; new grants use configured magazine counts. Supply was not reset.";
                return true;
            }
            catch (Exception exception)
            {
                response = "Weapon reload rejected; active settings retained. " + exception.Message;
                return false;
            }
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
                Native914.ProcessPickup(pickup.Base, true, ev.KnobSetting);
            }
            else
            {
                // Keep the original serial and native serialized magazine/chamber data.
                if (ev.Pickup is FirearmPickup gun)
                    gun.MaxAmmo = Item.Create<Firearm>(state.Definition.Type).MaxMagazineAmmo;
                Native914.ProcessPickup(ev.Pickup.Base, true, ev.KnobSetting);
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

            Native914.ProcessInventory(replacement.Base, ev.KnobSetting);
        });
        private sealed class WeaponState
        {
            internal int InitialCapacity;
            internal RegisteredWeapon Definition;
            internal int Token, ConsumedToken, DoorToken, AmmoBefore;
        }

        private sealed class DoorProgress
        {
            internal int Hits;
            internal float Last;
        }
    }
}
