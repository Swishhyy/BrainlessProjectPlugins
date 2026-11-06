using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using UnityEngine;

namespace Brainless.Items
{
    // Revision 16 item variants: four small variations each of Painkillers, Medkit,
    // Adrenaline and SCP-207, used like the normal item (same pipeline as the SCP-500 pills).
    public sealed partial class Plugin
    {
        internal static readonly HashSet<uint> VariantIds = new HashSet<uint>(Enumerable.Range(7139, 16).Select(i => (uint)i));
        private readonly Dictionary<Player, Timed> damageBoosts = new Dictionary<Player, Timed>();
        private readonly Dictionary<Player, Timed> ghosts = new Dictionary<Player, Timed>();
        private readonly Dictionary<Player, Timed> secondChances = new Dictionary<Player, Timed>();

        private void VariantSubscribe()
        {
            Exiled.Events.Handlers.Player.Hurting += VariantHurting;
            Exiled.Events.Handlers.Player.Dying += VariantDying;
        }

        private void VariantUnsubscribe()
        {
            Exiled.Events.Handlers.Player.Hurting -= VariantHurting;
            Exiled.Events.Handlers.Player.Dying -= VariantDying;
        }

        private void VariantReset()
        {
            damageBoosts.Clear();
            ghosts.Clear();
            secondChances.Clear();
        }

        private static bool Active(Dictionary<Player, Timed> map, Player player, out Timed timed) => map.TryGetValue(player, out timed) && timed.Until > Time.time && player.IsAlive && ReferenceEquals(player.Role.Base, timed.Life);
        internal bool IsGhosted(Player player) => player != null && Active(ghosts, player, out _);

        private void Give(Dictionary<Player, Timed> map, Player player, float seconds, float value = 0) => map[player] = new Timed { Until = Time.time + seconds, Life = player.Role.Base, Value = value };

        // Heals a little every second for a while (stops on death or role change).
        private void HealOverTime(Player player, float perSecond, int seconds)
        {
            var life = player.Role.Base;
            for (int second = 1; second <= seconds; second++)
                lifetime.Later(second, () =>
                {
                    if (player.IsConnected && player.IsAlive && ReferenceEquals(player.Role.Base, life))
                        player.Heal(perSecond);
                });
        }

        private bool VariantEffect(Player player, uint id)
        {
            switch (id)
            {
                case 7139: // Painkillers-A Extended
                    HealOverTime(player, 2, 30);
                    player.ShowHint("Extended release: healing 60 HP over 30 s.", 3);
                    return true;
                case 7140: // Painkillers-B Stim
                    player.Heal(25);
                    player.EnableEffect(EffectType.MovementBoost, 10, 15, false);
                    player.ShowHint("Stim: +25 HP and faster for 15 s.", 3);
                    return true;
                case 7141: // Painkillers-C Cure-all
                    player.Heal(20);
                    foreach (var ailment in new[] { EffectType.Poisoned, EffectType.Bleeding, EffectType.Burned, EffectType.Hemorrhage })
                        player.DisableEffect(ailment);
                    CureRolesPoison(player);
                    player.ShowHint("Cure-all: +20 HP, poison, bleeding and burns cured.", 3);
                    return true;
                case 7142: // Painkillers-D Numbing
                    player.AddAhp(30, 30, 0, .7f, 0, true);
                    player.ShowHint("Numbing: +30 shield.", 3);
                    return true;
                case 7143: // Medkit-A Field
                    player.Health = player.MaxHealth;
                    player.AddAhp(20, 20, 0, .7f, 0, true);
                    player.ShowHint("Field medkit: fully healed, +20 shield.", 3);
                    return true;
                case 7144: // Medkit-B Quick
                    player.Heal(65);
                    player.EnableEffect(EffectType.MovementBoost, 10, 10, false);
                    player.ShowHint("Quick medkit: +65 HP and a burst of speed.", 3);
                    return true;
                case 7145: // Medkit-C Regen
                    HealOverTime(player, 2, 45);
                    player.ShowHint("Regen medkit: healing 2 HP/s for 45 s.", 3);
                    return true;
                case 7146: // Medkit-D Combat
                    player.Heal(50);
                    Give(damageBoosts, player, 20, 1.15f);
                    player.ShowHint("Combat medkit: +50 HP and 15% more damage for 20 s.", 3);
                    return true;
                case 7147: // Adrenaline-A Iron
                    player.AddAhp(40, 40, 0, .7f, 0, true);
                    player.ShowHint("Iron: +40 shield.", 3);
                    return true;
                case 7148: // Adrenaline-B Berserk
                    Give(damageBoosts, player, 15, 1.2f);
                    player.ShowHint("Berserk: 20% more damage for 15 s.", 3);
                    return true;
                case 7149: // Adrenaline-C Ghost
                    Give(ghosts, player, 20);
                    player.EnableEffect(EffectType.SilentWalk, 8, 20, false);
                    player.ShowHint("Ghost: silent and hidden from SCP-939 for 20 s.", 3);
                    return true;
                case 7150: // Adrenaline-D Second
                    Give(secondChances, player, 30);
                    player.ShowHint("Second chance: a killing blow in the next 30 s leaves you at 1 HP.", 4);
                    return true;
                case 7151: // SCP-207-A Diet
                    player.EnableEffect(EffectType.MovementBoost, 20, 30, false);
                    player.ShowHint("Diet cola: faster for 30 s, no health drain.", 3);
                    return true;
                case 7152: // SCP-207-B Cherry
                    player.Heal(30);
                    player.EnableEffect(EffectType.MovementBoost, 20, 20, false);
                    player.ShowHint("Cherry cola: +30 HP and faster for 20 s.", 3);
                    return true;
                case 7153: // SCP-207-C Frozen
                    player.EnableEffect(EffectType.Invigorated, 1, 40, false);
                    player.ShowHint("Frozen cola: endless stamina for 40 s.", 3);
                    return true;
                case 7154: // SCP-207-D Lightning
                    player.EnableEffect(EffectType.MovementBoost, 40, 15, false);
                    var life = player.Role.Base;
                    lifetime.Later(15, () =>
                    {
                        if (player.IsConnected && player.IsAlive && ReferenceEquals(player.Role.Base, life))
                            player.EnableEffect(EffectType.Slowness, 20, 10, false);
                    });
                    player.ShowHint("Lightning cola: very fast for 15 s, then slowed for 10 s.", 3);
                    return true;
            }

            return false;
        }

        private void VariantHurting(HurtingEventArgs ev) => Safe.Run(() =>
        {
            if (ev.IsAllowed && ev.Amount > 0 && ev.Attacker != null && ev.Attacker != ev.Player && Active(damageBoosts, ev.Attacker, out var boost))
                ev.Amount *= boost.Value;
        });

        private void VariantDying(DyingEventArgs ev) => Safe.Run(() =>
        {
            if (!ev.IsAllowed || !Active(secondChances, ev.Player, out _))
                return;
            secondChances.Remove(ev.Player);
            ev.IsAllowed = false;
            ev.Player.Health = 1;
            ev.Player.ShowHint("<b>Second chance!</b> You survived at 1 HP.", 4);
        });

        // Roles keeps its own SCP poison; ask it to cure that too (no hard reference).
        private static void CureRolesPoison(Player player)
        {
            try
            {
                var roles = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == "Brainless.Roles");
                roles?.GetType("Brainless.Roles.RolesApi")?.GetMethod("CurePoison", new[] { typeof(Player) })?.Invoke(null, new object[] { player });
            }
            catch (Exception exception)
            {
                Log.Debug("[Brainless Items] Roles poison cure skipped: " + exception.GetBaseException().Message);
            }
        }

        internal static IEnumerable<ItemSpec> VariantDefaults() => new[]
        {
            V(7139, "Painkillers-A Extended", ItemType.Painkillers, "Heals 60 health slowly over 30 seconds."),
            V(7140, "Painkillers-B Stim", ItemType.Painkillers, "Heals 25 health and makes you 10% faster for 15 seconds."),
            V(7141, "Painkillers-C Cure-all", ItemType.Painkillers, "Heals 20 health and cures poison, bleeding and burns."),
            V(7142, "Painkillers-D Numbing", ItemType.Painkillers, "Gives 30 artificial health."),
            V(7143, "Medkit-A Field", ItemType.Medkit, "Fully heals you and gives 20 artificial health."),
            V(7144, "Medkit-B Quick", ItemType.Medkit, "Heals 65 health and gives a short burst of speed."),
            V(7145, "Medkit-C Regen", ItemType.Medkit, "Heals 2 health per second for 45 seconds."),
            V(7146, "Medkit-D Combat", ItemType.Medkit, "Heals 50 health and gives 15% more damage for 20 seconds."),
            V(7147, "Adrenaline-A Iron", ItemType.Adrenaline, "Gives 40 artificial health instead of stamina."),
            V(7148, "Adrenaline-B Berserk", ItemType.Adrenaline, "Gives 20% more damage for 15 seconds."),
            V(7149, "Adrenaline-C Ghost", ItemType.Adrenaline, "Silent footsteps and hidden from SCP-939 for 20 seconds."),
            V(7150, "Adrenaline-D Second", ItemType.Adrenaline, "If you would die in the next 30 seconds, you survive at 1 health (once)."),
            V(7151, "SCP-207-A Diet", ItemType.SCP207, "Cola speed for 30 seconds with no health drain."),
            V(7152, "SCP-207-B Cherry", ItemType.SCP207, "Cola speed for 20 seconds and 30 health."),
            V(7153, "SCP-207-C Frozen", ItemType.SCP207, "No speed, but endless stamina for 40 seconds."),
            V(7154, "SCP-207-D Lightning", ItemType.SCP207, "Double cola speed for 15 seconds, then slowed for 10 seconds."),
        };

        private static ItemSpec V(uint id, string name, ItemType body, string description) => new ItemSpec { Id = id, Name = name, BaseType = body, Description = description, RoundCap = 3 };

        private sealed class Timed
        {
            internal float Until, Value;
            internal PlayerRoleBase Life;
        }
    }
}
