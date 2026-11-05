using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using UnityEngine;
using P = Exiled.Events.Handlers.Player;

namespace Brainless.Items
{
    // Cursed items (7123-7126) and native use for pills: consumables play the normal
    // SCP-500/adrenaline animation, then the native effect is cancelled and ours applies.
    public sealed partial class Plugin
    {
        private readonly System.Random cursedRandom = new System.Random();

        private void CursedSubscribe()
        {
            P.UsingItemCompleted += UsingCompleted;
            P.ActivatingWarheadPanel += WarheadPanel;
        }

        private void CursedUnsubscribe()
        {
            P.UsingItemCompleted -= UsingCompleted;
            P.ActivatingWarheadPanel -= WarheadPanel;
        }

        // Start of use: block early (cooldown, no 096 target...) so the pill isn't wasted.
        private bool PillUsing(UsingItemEventArgs ev, RegisteredItem item)
        {
            if (!IsPill(item.Id))
                return false;
            string blocked = PillBlocked(ev.Player, item);
            if (blocked != null)
            {
                ev.IsAllowed = false;
                ev.Player.ShowHint(blocked, 3);
            }

            return true;
        }

        private void UsingCompleted(UsingItemCompletedEventArgs ev) => Safe.Run(() =>
        {
            var item = ev.Item == null ? null : Find(ev.Item.Serial);
            if (item == null || !IsPill(item.Id))
                return;
            // No native SCP-500 heal / adrenaline boost: the custom effect replaces it.
            ev.IsAllowed = false;
            string blocked = PillBlocked(ev.Player, item);
            if (blocked != null)
            {
                ev.Player.ShowHint(blocked, 3);
                return;
            }

            if (Spend(ev.Player, ev.Item))
                PillEffect(ev.Player, item);
        });

        private static readonly EffectType[] Ailments =
        {
            EffectType.Bleeding, EffectType.Poisoned, EffectType.Burned, EffectType.Concussed, EffectType.Blinded, EffectType.Deafened,
            EffectType.Ensnared, EffectType.Exhausted, EffectType.Flashed, EffectType.Hemorrhage, EffectType.Hypothermia, EffectType.SinkHole,
            EffectType.Stained, EffectType.Traumatized, EffectType.Disabled, EffectType.Asphyxiated, EffectType.Corroding, EffectType.AmnesiaVision,
            EffectType.AmnesiaItems, EffectType.Scp207, EffectType.AntiScp207,
        };

        // 50/50: a super heal or a placebo.
        private void CursedScp500(Player player)
        {
            var cfg = Config.Cursed;
            if (cursedRandom.NextDouble() * 100 >= cfg.Scp500SuccessPercent)
            {
                player.ShowHint("<b>Placebo.</b> Nothing happened.", 4);
                return;
            }

            player.Health = player.MaxHealth;
            foreach (var ailment in Ailments)
                player.DisableEffect(ailment);
            player.AddAhp(cfg.Scp500Ahp, cfg.Scp500Ahp, 0.4f);
            var life = player.Role.Base;
            for (int second = 1; second <= (int)cfg.Scp500RegenSeconds; second++)
                lifetime.Later(second, () =>
                {
                    if (player.IsConnected && player.IsAlive && player.Role.Base == life)
                        player.Health = Mathf.Min(player.MaxHealth, player.Health + cfg.Scp500RegenPerSecond);
                });
            player.ShowHint("<b>It worked!</b> Fully healed, cured and shielded.", 4);
        }

        // A huge rush, then a crash.
        private void CursedAdrenaline(Player player)
        {
            var cfg = Config.Cursed;
            player.EnableEffect(EffectType.MovementBoost, cfg.AdrenalineSpeedPercent, cfg.AdrenalineSeconds);
            player.Stamina = player.StaminaStat.MaxValue;
            player.ShowHint($"<b>RUSH!</b> {cfg.AdrenalineSeconds:0}s before the crash...", 4);
            var life = player.Role.Base;
            lifetime.Later(cfg.AdrenalineSeconds, () =>
            {
                if (!player.IsConnected || !player.IsAlive || player.Role.Base != life)
                    return;
                player.EnableEffect(EffectType.Ensnared, 1, cfg.AdrenalineFreezeSeconds);
                player.EnableEffect(EffectType.Blinded, 1, cfg.AdrenalineBlindSeconds);
                player.ShowHint("<b>Crash.</b>", 3);
            });
        }

        // Cursed Keycard: every keycard door it opens pings the SCPs and spends a use.
        private void CursedDoor(Player player, Item card, Door door)
        {
            if (!door.IsKeycardDoor || door.IsOpen)
                return;
            string zone = player.Zone switch
            {
                ZoneType.LightContainment => "Light Containment",
                ZoneType.HeavyContainment => "Heavy Containment",
                ZoneType.Entrance => "Entrance Zone",
                ZoneType.Surface => "Surface",
                _ => "the facility",
            };
            foreach (var scp in Player.List.Where(p => p.IsAlive && p.IsScp))
                scp.ShowHint("A cursed keycard was used: <b>" + player.Nickname + "</b> is in " + zone + ".", 5);
            lifetime.Later(.1f, () =>
            {
                if (player.IsConnected && player.Items.Contains(card) && Spend(player, card) && !player.Items.Contains(card))
                    player.ShowHint("The cursed keycard crumbled to dust.", 3);
            });
        }

        private void WarheadPanel(ActivatingWarheadPanelEventArgs ev)
        {
            var held = ev.Player.CurrentItem == null ? null : Find(ev.Player.CurrentItem.Serial);
            if (held?.Id != SupCatalog.CursedKeycard)
                return;
            ev.IsAllowed = false;
            ev.Player.ShowHint("The cursed keycard won't touch the warhead.", 3);
        }

        private static readonly ItemType[] KeycardSources = { ItemType.KeycardJanitor, ItemType.KeycardScientist, ItemType.KeycardResearchCoordinator, ItemType.KeycardZoneManager, ItemType.KeycardGuard };

        // Rare cursed natural loot, a couple of seconds after the map's own loot.
        private void CursedLootRound()
        {
            lifetime.Later(Config.Distribution.MapDelaySeconds + 1, () =>
            {
                var entries = new List<CursedLoot.Entry>();
                void Add(uint id, params ItemType[] sources)
                {
                    if (registered.TryGetValue(id, out var item))
                        entries.Add(new CursedLoot.Entry { Id = id, Sources = sources, Spawn = position => item.TrySpawn(position, true, out var pickup) == null ? pickup : null });
                }

                Add(SupCatalog.CursedKeycard, KeycardSources);
                Add(SupCatalog.CursedScp500, ItemType.SCP500);
                Add(SupCatalog.CursedAdrenaline, ItemType.Adrenaline);
                Add(SupCatalog.MonkeysPaw, ItemType.Coin);
                int placed = CursedLoot.Run(entries, Config.Cursed.LootChancePercent, Config.Cursed.LootMaxPerItem, cursedRandom);
                if (Config.Debug)
                    Log.Debug("Cursed loot placed: " + placed);
            });
        }
    }
}
