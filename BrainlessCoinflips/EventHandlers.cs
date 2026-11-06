using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Coinflips.Configs;
using Brainless.Coinflips.Types;
using Brainless.Internal;
using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Player;
using UnityEngine;

namespace Brainless.Coinflips
{
    public sealed class EventHandlers
    {
        private static Config Config => Plugin.Instance.Config;
        private static Configs.Translations Translations => Plugin.Instance.Translation;

        private readonly System.Random random = new System.Random();
        public static Dictionary<ushort, int> CoinUses = new Dictionary<ushort, int>();
        private readonly Dictionary<string, DateTime> cooldowns = new Dictionary<string, DateTime>();
        private Dictionary<ItemType, int> remainingReplacements = new Dictionary<ItemType, int>();
        private int remainingDefaultCoins;
        public EventHandlers()
        {
            Reset();
        }

        public void Reset()
        {
            CoinUses.Clear();
            cooldowns.Clear();
            remainingDefaultCoins = Config.DefaultCoinsAmount;
            doubledByPlayer.Clear();
            doubledThisRound = 0;
            remainingReplacements = Config.ItemToReplace == null ? new Dictionary<ItemType, int>() : new Dictionary<ItemType, int>(Config.ItemToReplace);
        }

        public void OnLeft(LeftEventArgs ev)
        {
            if (ev.Player.UserId != null)
                cooldowns.Remove(ev.Player.UserId);
            Types.Modifiers.Forget(ev.Player);
        }

        public static void SendBroadcast(Player player, string message, bool showHint = false, bool isTails = false)
        {
            if (player == null || !player.IsConnected)
                return;
            if (!string.IsNullOrEmpty(message))
                player.Broadcast(new Exiled.API.Features.Broadcast(message, Config.BroadcastTime), true);
            var hints = Translations.HintMessages?.ToArray();
            if (showHint && Config.HintDuration > 0 && hints?.Length > 0)
                player.ShowHint(hints[Math.Min(isTails ? 0 : 1, hints.Length - 1)], Config.HintDuration);
        }

        public void OnCoinFlip(FlippingCoinEventArgs ev) => Safe.Run(() => Flip(ev));
        private void Flip(FlippingCoinEventArgs ev)
        {
            if (!ev.IsAllowed || ev.Item == null || ev.Item.Type != ItemType.Coin || !ev.Player.IsAlive)
                return;
            // Custom coins are skipped, except the Monkey's Paw (Brainless Items).
            var custom = CustomItem.Registered.FirstOrDefault(item => item.Check(ev.Item));
            if (custom != null && custom.Id != Config.MonkeysPawId)
                return;
            string user = ev.Player.UserId ?? ev.Player.Id.ToString();
            if (cooldowns.TryGetValue(user, out var last) && (DateTime.UtcNow - last).TotalSeconds < Math.Max(0, Config.CoinCooldown))
            {
                ev.IsAllowed = false;
                SendBroadcast(ev.Player, Translations.TossOnCooldownMessage);
                return;
            }

            if (custom != null)
            {
                MonkeysPaw(ev, custom, user);
                return;
            }

            // "Lucky, then cursed" can force the next results; otherwise a strict 50/50.
            bool? forced = TwistedWishes.NextForced(ev.Player);
            if (forced.HasValue)
                ev.IsTails = forced.Value;
            bool bad = ev.IsTails;
            int[] weights = bad ? new[]
            {
                Config.HpReductionChance,
                Config.TpToClassDCellsChance,
                Config.RandomBadEffectChance,
                Config.WarheadChance,
                Config.LightsOutChance,
                Config.LiveHeChance,
                Config.TrollFlashChance,
                Config.ScpTpChance,
                Config.OneHpLeftChance,
                Config.PrimedVaseChance,
                Config.ShitPantsChance,
                Config.FakeCassieChance,
                Config.TurnIntoScpChance,
                Config.InventoryResetChance,
                Config.ClassSwapChance,
                Config.InstantExplosionChance,
                Config.PlayerSwapChance,
                Config.KickChance,
                Config.SpectSwapChance,
                Config.TeslaTpChance,
                Config.InventorySwapChance,
                Config.RandomTeleportChance,
                Config.HandcuffChance,
                BrainlessContent.HasItems(Config.CursedCustomItems) ? Config.CursedCustomItemChance : 0
            }

            : new[]
            {
                Config.KeycardChance,
                Config.MedicalKitChance,
                Config.TpToEscapeChance,
                Config.HealChance,
                Config.MoreHpChance,
                Config.HatChance,
                Config.RandomGoodEffectChance,
                Config.OneAmmoLogicerChance,
                Config.LightbulbChance,
                Config.PinkCandyChance,
                Config.BadRevoChance,
                Config.EmptyHidChance,
                Config.ForceRespawnChance,
                Config.SizeChangeChance,
                Config.RandomItemChance,
                BrainlessContent.HasItems(Config.CustomItems) ? Config.CustomItemChance : 0,
                BrainlessContent.HasRoles ? Config.CustomRoleChance : 0
            };
            var extras = bad ? CoinExtras.Bad : CoinExtras.Good;
            weights = weights.Concat(extras.Select(e => Math.Max(0, e.Weight()))).ToArray();
            // The very rare item wipe is rolled before any other tails outcome.
            bool wipe = bad && random.NextDouble() * 100 < Config.ItemWipeChancePercent;
            int effectIndex = wipe ? 0 : Rules.WeightedIndex(weights, random);
            if (effectIndex < 0)
                return;
            ushort serial = ev.Item.Serial;
            if (!CoinUses.TryGetValue(serial, out int uses))
            {
                int low = Config.MinMaxDefaultCoins?.Count > 0 ? Math.Max(0, Config.MinMaxDefaultCoins[0]) : 1;
                int high = Config.MinMaxDefaultCoins?.Count > 1 ? Math.Max(low + 1, Config.MinMaxDefaultCoins[1]) : Math.Max(2, low + 1);
                CoinUses[serial] = uses = random.Next(low, high);
            }

            if (uses <= 0)
            {
                ev.IsAllowed = false;
                RemoveCoin(serial);
                SendBroadcast(ev.Player, Translations.CoinNoUsesMessage);
                return;
            }

            cooldowns[user] = DateTime.UtcNow;
            CoinUses[serial] = uses - 1;
            var builtIn = bad ? CoinFlipEffect.BadEffects : CoinFlipEffect.GoodEffects;
            var effect = wipe ? CoinExtras.ItemWipe() : effectIndex < builtIn.Count ? builtIn[effectIndex] : extras[effectIndex - builtIn.Count].Create();
            try
            {
                effect.Execute(ev.Player);
            }
            catch (Exception exception)
            {
                Log.Error($"Brainless Coinflips effect {effectIndex}: {exception}");
                SendBroadcast(ev.Player, "The coin could not find a valid target for this effect.");
            }

            string message = effect.Message;
            // Read the live count: Lucky coin adds flips and Unlucky coin breaks it.
            if (!CoinUses.TryGetValue(serial, out int left) || left <= 0)
            {
                RemoveCoin(serial);
                message += Translations.CoinBreaksMessage;
            }

            SendBroadcast(ev.Player, message, true, ev.IsTails);
        }

        // One twisted wish, then the paw crumbles.
        private void MonkeysPaw(FlippingCoinEventArgs ev, CustomItem paw, string user)
        {
            var wish = TwistedWishes.Roll();
            if (wish == null)
                return;
            cooldowns[user] = DateTime.UtcNow;
            try
            {
                wish.Execute(ev.Player);
            }
            catch (Exception exception)
            {
                Log.Error($"Brainless Coinflips Monkey's Paw wish: {exception}");
            }

            var player = ev.Player;
            var item = ev.Item;
            Plugin.Instance.Later(1f, () =>
            {
                paw.TrackedSerials.Remove(item.Serial);
                if (player.IsConnected && player.Items.Contains(item))
                    player.RemoveItem(item);
            });
            SendBroadcast(player, Translations.MonkeysPawPrefix + wish.Message + Translations.PawCrumblesMessage);
        }

        private static void RemoveCoin(ushort serial)
        {
            foreach (var player in Player.List.ToArray())
            {
                var coin = player.Items.FirstOrDefault(i => i.Serial == serial && i.Type == ItemType.Coin);
                if (coin != null)
                {
                    player.RemoveItem(coin);
                    return;
                }
            }

            var pickup = Pickup.List.FirstOrDefault(p => p.Serial == serial && p.Type == ItemType.Coin);
            pickup?.Destroy();
        }

        private bool RemoveDefault()
        {
            if (remainingDefaultCoins == 0)
                return false;
            if (remainingDefaultCoins > 0)
                remainingDefaultCoins--;
            return true;
        }

        // SCP-914 Very Fine doubles ordinary coins. Each coin is processed on its own, so a
        // stack of 2 becomes 4. Capped per player, per round and by the coins in the world;
        // past a cap the coin just comes out unchanged.
        private readonly Dictionary<string, int> doubledByPlayer = new Dictionary<string, int>();
        private int doubledThisRound;

        private string DoublingBlocked(Player player)
        {
            if (doubledThisRound >= Config.Scp914CoinMaxPerRound)
                return "SCP-914 has made enough coins this round.";
            if (player?.UserId != null && doubledByPlayer.TryGetValue(player.UserId, out int mine) && mine >= Config.Scp914CoinMaxPerPlayer)
                return "SCP-914 won't double any more of your coins this round.";
            int coins = Pickup.List.Count(p => p != null && p.Type == ItemType.Coin) + Player.List.Sum(p => p.Items.Count(i => i.Type == ItemType.Coin));
            if (coins >= Config.Scp914CoinWorldCap)
                return "There are already too many coins in the facility.";
            return null;
        }

        private void CountDoubled(Player player)
        {
            doubledThisRound++;
            if (player?.UserId != null)
                doubledByPlayer[player.UserId] = (doubledByPlayer.TryGetValue(player.UserId, out int mine) ? mine : 0) + 1;
        }

        public void OnUpgradingPickup(Exiled.Events.EventArgs.Scp914.UpgradingPickupEventArgs ev) => Safe.Run(() =>
        {
            if (!ev.IsAllowed || !Config.Scp914DoublesCoins || ev.KnobSetting != Scp914.Scp914KnobSetting.VeryFine || ev.Pickup?.Base == null || ev.Pickup.Type != ItemType.Coin || CustomItem.Registered.Any(i => i.Check(ev.Pickup)))
                return;
            var owner = ev.Pickup.PreviousOwner;
            if (DoublingBlocked(owner) != null)
                return;
            ev.IsAllowed = false;
            ev.Pickup.Position = ev.OutputPosition;
            Pickup.CreateAndSpawn(ItemType.Coin, ev.OutputPosition + UnityEngine.Vector3.right * .1f, null, owner);
            CountDoubled(owner);
        });

        public void OnUpgradingInventoryItem(Exiled.Events.EventArgs.Scp914.UpgradingInventoryItemEventArgs ev) => Safe.Run(() =>
        {
            if (!ev.IsAllowed || !Config.Scp914DoublesCoins || ev.KnobSetting != Scp914.Scp914KnobSetting.VeryFine || ev.Item == null || ev.Item.Type != ItemType.Coin || ev.Player == null || CustomItem.Registered.Any(i => i.Check(ev.Item)))
                return;
            string blocked = DoublingBlocked(ev.Player);
            if (blocked != null)
            {
                ev.Player.ShowHint(blocked, 3);
                return;
            }

            ev.IsAllowed = false;
            if (ev.Player.Items.Count < 8)
                ev.Player.AddItem(ItemType.Coin);
            else
                Pickup.CreateAndSpawn(ItemType.Coin, ev.Player.Position, null, ev.Player);
            CountDoubled(ev.Player);
        });

        public void OnSpawningItem(SpawningItemEventArgs ev)
        {
            if (ev.IsAllowed && ev.Pickup.Type == ItemType.Coin && !CustomItem.Registered.Any(i => i.Check(ev.Pickup)) && RemoveDefault())
                ev.IsAllowed = false;
        }

        public void OnFillingLocker(FillingLockerEventArgs ev)
        {
            if (!ev.IsAllowed)
                return;
            if (ev.Pickup.Type == ItemType.Coin && RemoveDefault())
            {
                ev.IsAllowed = false;
                return;
            }

            if (!remainingReplacements.TryGetValue(ev.Pickup.Type, out int remaining) || remaining == 0)
                return;
            if (remaining > 0)
                remainingReplacements[ev.Pickup.Type] = remaining - 1;
            ev.IsAllowed = false;
            Pickup.CreateAndSpawn(ItemType.Coin, ev.Pickup.Position, Quaternion.identity);
        }
    }
}
