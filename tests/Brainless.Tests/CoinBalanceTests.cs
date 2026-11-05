using System;
using System.Linq;

internal static class CoinBalanceTests
{
    internal static void Run(Action<bool, string> check)
    {
#if NATIVE_CONFIG_TESTS
        // A 5.1.x config: every stock value from before the rebalance.
        var old = new BetterCoinflips.Configs.Config { TurnIntoScpChance = 30, KickChance = 5, WarheadChance = 10, SpectSwapChance = 10, LiveHeChance = 30, InstantExplosionChance = 10, TeslaTpChance = 15 };
        old.BadEffects.UnionWith(new[] { Exiled.API.Enums.EffectType.Decontaminating, Exiled.API.Enums.EffectType.CardiacArrest, Exiled.API.Enums.EffectType.PocketCorroding });
        check(old.UpgradeBalance() && old.BalanceRevision == 2, "coin balance migration runs once on old configs");
        check(old.TurnIntoScpChance == 0 && old.WarheadChance == 0 && old.InventorySwapChance == 0 && old.InventoryResetChance == 0 && old.LiveHeChance == 8, "Turn into SCP, warhead, inventory swap and the old inventory reset are off; the live grenade is rarer");
        check(old.KickChance == 0 && old.SpectSwapChance == 0 && old.InstantExplosionChance == 0 && old.TeslaTpChance == 0, "outcomes that send players to spectator are off by default");
        check(!old.BadEffects.Contains(Exiled.API.Enums.EffectType.Decontaminating) && !old.BadEffects.Contains(Exiled.API.Enums.EffectType.CardiacArrest) && !old.BadEffects.Contains(Exiled.API.Enums.EffectType.PocketCorroding) && old.BadEffects.Contains(Exiled.API.Enums.EffectType.Bleeding), "the stock bad-effect list loses only its lethal effects");
        check(!old.UpgradeBalance(), "coin balance migration does not repeat");
        var first = new BetterCoinflips.Configs.Config { BalanceRevision = 1, SpectSwapChance = 3, InstantExplosionChance = 4, TeslaTpChance = 6, LiveHeChance = 15 };
        first.UpgradeBalance();
        check(first.SpectSwapChance == 0 && first.InstantExplosionChance == 0 && first.TeslaTpChance == 0 && first.LiveHeChance == 8, "configs saved by the first rebalance also reach the current defaults");
        var edited = new BetterCoinflips.Configs.Config { TurnIntoScpChance = 50, KickChance = 5 };
        edited.BadEffects.Add(Exiled.API.Enums.EffectType.Decontaminating);
        edited.UpgradeBalance();
        check(edited.TurnIntoScpChance == 50 && edited.KickChance == 0 && edited.BadEffects.Contains(Exiled.API.Enums.EffectType.Decontaminating), "owner-edited coin weights and custom effect lists are kept");
        var fresh = new BetterCoinflips.Configs.Config();
        var props = typeof(BetterCoinflips.Configs.Config).GetProperties();
        int Weight(string name) => (int)System.Array.Find(props, p => p.Name == name).GetValue(fresh);
        string[] newTails = { "FrozenChance", "SlowedChance", "ExhaustedChance", "SinkholeChance", "ButterfingersChance", "DropHeldItemChance", "LoseAmmoChance", "EmptyMagazineChance", "BlindedChance", "DeafenedChance", "ConcussedChance", "FlashedChance", "LocationLeakChance", "CassieCalloutChance", "LockedInChance", "RoomBlackoutChance", "DropKeycardsChance", "DropArmorChance", "CursedMaxHpChance", "BleedingChance", "HypothermiaChance", "StaminaDrainedChance", "PoisonedChance", "HuntedChance", "BackToSpawnChance", "JunkSwapChance", "TripChance", "WeakArmsChance", "GlassJawChance", "UnluckyCoinChance" };
        string[] newHeads = { "SpeedBoostChance", "ShieldUpChance", "ToughSkinChance", "SecondWindChance", "AmmoRefillChance", "KeycardUpgradeChance", "RandomScpItemChance", "LuckyCoinChance", "SecondChanceChance", "FullHealChance" };
        check(newTails.All(n => Weight(n) > 0) && newTails.Length >= 30, "all thirty extra tails outcomes are on by default");
        check(newHeads.All(n => Weight(n) > 0), "all ten extra heads outcomes are on by default");
        check(fresh.ItemWipeChancePercent == 0.1 && System.Array.Find(props, p => p.Name == "LuckyTailsPercent") == null, "the item wipe is a separate 0.1% roll and lucky tails is gone, so coins are a straight 50/50");
#endif
    }
}
