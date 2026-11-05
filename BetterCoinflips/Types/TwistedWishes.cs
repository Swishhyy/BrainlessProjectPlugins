using System;
using System.Collections.Generic;
using System.Linq;
using BetterCoinflips.Configs;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using MEC;
using UnityEngine;

namespace BetterCoinflips.Types
{
    // Monkey's Paw wishes: something good with a built-in catch. Each entry carries its
    // own weight. Everything is tied to the current life and ends on death/role change.
    internal static class TwistedWishes
    {
        private static Config Config => Plugin.Instance.Config;
        private static CoinTimings T => Config.Timings ?? new CoinTimings();
        private static Configs.Translations Text => Plugin.Instance.Translation;
        private static readonly System.Random Rd = new();

        // Forced coin results from "Lucky, then cursed", by user ID (true = tails).
        private static readonly Dictionary<string, Queue<bool>> forced = new();

        internal static readonly List<CoinExtras.Weighted> All = new()
        {
            W(() => Config.FastThenWindedChance, () => Text.FastThenWindedMessage, FastThenWinded),
            W(() => Config.TankButHeavyChance, () => Text.TankButHeavyMessage, p =>
            {
                p.MaxHealth += T.TankMaxHp;
                p.Health += T.TankMaxHp;
                p.EnableEffect(EffectType.Slowness, T.TankSlowPercent, 0);
            }),
            W(() => Config.UnkillableThenOneHpChance, () => Text.UnkillableMessage, Unkillable),
            W(() => Config.SilentButDeafChance, () => Text.SilentButDeafMessage, p =>
            {
                p.EnableEffect(EffectType.SilentWalk, 10, T.SilentDeafSeconds);
                p.EnableEffect(EffectType.Deafened, 1, T.SilentDeafSeconds);
            }),
            W(() => Config.StrongButFragileChance, () => Text.StrongButFragileMessage, p =>
            {
                Modifiers.Dealt(p, 1 + T.StrongFragilePercent / 100, T.StrongFragileSeconds);
                Modifiers.Taken(p, 1 + T.StrongFragilePercent / 100, T.StrongFragileSeconds);
            }),
            W(() => Config.EndlessAmmoThenNoneChance, () => Text.EndlessAmmoMessage, p => Plugin.Instance.Run(EndlessAmmo(p, p.Role.Base))),
            W(() => Config.FullHealThenBlindChance, () => Text.FullHealThenBlindMessage, p =>
            {
                p.Health = p.MaxHealth;
                p.AddAhp(T.FullHealAhp, T.FullHealAhp, 0.4f);
                p.EnableEffect(EffectType.Blinded, 1, T.FullHealBlindSeconds);
            }),
            W(() => Config.GhostThenExposedChance, () => Text.GhostMessage, Ghost),
            W(() => Config.RichesButFrailChance, () => Text.RichesButFrailMessage, p =>
            {
                for (int i = 0; i < 3; i++)
                    Pickup.CreateAndSpawn(Riches[Rd.Next(Riches.Length)], p.Position, Quaternion.identity);
                p.MaxHealth = Math.Max(1, p.MaxHealth - T.FrailMaxHp);
                if (p.Health > p.MaxHealth)
                    p.Health = p.MaxHealth;
            }),
            W(() => Config.FreeExitItemsLeftChance, () => Text.FreeExitMessage, p =>
            {
                p.DropItems();
                p.Teleport(Door.Get(DoorType.EscapeSecondary));
            }),
            W(() => Config.LuckyThenCursedChance, () => Text.LuckyThenCursedMessage.Replace("{flips}", T.LuckyFlips.ToString()), p =>
            {
                var queue = new Queue<bool>();
                for (int i = 0; i < T.LuckyFlips; i++)
                    queue.Enqueue(false);
                for (int i = 0; i < T.LuckyFlips; i++)
                    queue.Enqueue(true);
                forced[p.UserId ?? p.Id.ToString()] = queue;
            }),
        };

        private static CoinExtras.Weighted W(Func<int> weight, Func<string> message, Action<Player> execute) => new() { Weight = weight, Create = () => new CoinFlipEffect(message(), execute) };

        private static readonly ItemType[] Riches = { ItemType.Medkit, ItemType.Adrenaline, ItemType.SCP500, ItemType.ArmorCombat, ItemType.GrenadeHE, ItemType.KeycardMTFOperative, ItemType.SCP207, ItemType.Painkillers };

        internal static CoinFlipEffect Roll()
        {
            var pool = All.Where(w => w.Weight() > 0).ToList();
            if (pool.Count == 0)
                return null;
            int roll = Rd.Next(pool.Sum(w => w.Weight()));
            return pool.First(w => (roll -= w.Weight()) < 0).Create();
        }

        // Next forced coin result for this player (true = tails), or null.
        internal static bool? NextForced(Player player)
        {
            string key = player.UserId ?? player.Id.ToString();
            if (!forced.TryGetValue(key, out var queue) || queue.Count == 0)
                return null;
            bool tails = queue.Dequeue();
            if (queue.Count == 0)
                forced.Remove(key);
            return tails;
        }

        internal static void Clear() => forced.Clear();

        private static void FastThenWinded(Player player)
        {
            player.EnableEffect(EffectType.MovementBoost, T.FastPercent, T.FastSeconds);
            var life = player.Role.Base;
            Plugin.Instance.Later(T.FastSeconds, () =>
            {
                if (!player.IsConnected || !player.IsAlive || player.Role.Base != life)
                    return;
                EventHandlers.SendBroadcast(player, Text.WindedMessage);
                Plugin.Instance.Run(SlowStamina(player, life));
            });
        }

        // Stamina recovers WindedRegenPercent slower: each tick keeps only part of the gain.
        private static IEnumerator<float> SlowStamina(Player player, PlayerRoles.PlayerRoleBase life)
        {
            float until = Time.time + T.WindedSeconds;
            float keep = Mathf.Clamp01(1 - T.WindedRegenPercent / 100);
            float last = player.Stamina;
            while (Time.time < until && player.IsConnected && player.IsAlive && player.Role.Base == life)
            {
                yield return Timing.WaitForSeconds(.1f);
                float now = player.Stamina;
                if (now > last)
                    player.Stamina = now = last + (now - last) * keep;
                last = now;
            }
        }

        private static void Unkillable(Player player)
        {
            var life = player.Role.Base;
            Modifiers.Unkillable(player, T.UnkillableSeconds);
            Plugin.Instance.Later(T.UnkillableSeconds, () =>
            {
                if (!player.IsConnected || !player.IsAlive || player.Role.Base != life)
                    return;
                player.Health = 1;
                EventHandlers.SendBroadcast(player, Text.UnkillableEndMessage);
            });
        }

        // The held gun's magazine stays full, then all reserve ammo is taken.
        private static IEnumerator<float> EndlessAmmo(Player player, PlayerRoles.PlayerRoleBase life)
        {
            float until = Time.time + T.EndlessAmmoSeconds;
            while (Time.time < until && player.IsConnected && player.IsAlive && player.Role.Base == life)
            {
                if (player.CurrentItem is Firearm gun && gun.MagazineAmmo < gun.MaxMagazineAmmo)
                    gun.MagazineAmmo = gun.MaxMagazineAmmo;
                yield return Timing.WaitForSeconds(.2f);
            }

            if (player.IsConnected && player.IsAlive && player.Role.Base == life)
            {
                player.ClearAmmo();
                EventHandlers.SendBroadcast(player, Text.NoAmmoMessage);
            }
        }

        private static void Ghost(Player player)
        {
            var life = player.Role.Base;
            player.EnableEffect(EffectType.Invisible, 1, T.GhostSeconds);
            Plugin.Instance.Later(T.GhostSeconds, () =>
            {
                if (!player.IsConnected || !player.IsAlive || player.Role.Base != life)
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
                    scp.ShowHint(Text.GhostHint.Replace("{player}", player.Nickname).Replace("{zone}", zone), 6);
                EventHandlers.SendBroadcast(player, Text.GhostEndMessage);
            });
        }
    }
}
