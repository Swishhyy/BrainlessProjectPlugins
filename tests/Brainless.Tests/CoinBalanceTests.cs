using System;

internal static class CoinBalanceTests
{
    internal static void Run(Action<bool, string> check)
    {
#if NATIVE_CONFIG_TESTS
        // A 5.1.x config: every stock value from before the rebalance.
        var old = new BetterCoinflips.Configs.Config { TurnIntoScpChance = 30, KickChance = 5, WarheadChance = 10, SpectSwapChance = 10, LiveHeChance = 30, InstantExplosionChance = 10, TeslaTpChance = 15 };
        old.BadEffects.UnionWith(new[] { Exiled.API.Enums.EffectType.Decontaminating, Exiled.API.Enums.EffectType.CardiacArrest, Exiled.API.Enums.EffectType.PocketCorroding });
        check(old.UpgradeBalance() && old.BalanceRevision == 2, "coin balance migration runs once on old configs");
        check(old.TurnIntoScpChance == 8 && old.WarheadChance == 3 && old.LiveHeChance == 8, "untouched gamble outcomes move to the rarer defaults");
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
        int tails = fresh.HpReductionChance + fresh.TpToClassDCellsChance + fresh.RandomBadEffectChance + fresh.WarheadChance + fresh.LightsOutChance + fresh.LiveHeChance + fresh.TrollFlashChance + fresh.ScpTpChance + fresh.OneHpLeftChance + fresh.PrimedVaseChance + fresh.ShitPantsChance + fresh.FakeCassieChance + fresh.TurnIntoScpChance + fresh.InventoryResetChance + fresh.ClassSwapChance + fresh.InstantExplosionChance + fresh.PlayerSwapChance + fresh.KickChance + fresh.SpectSwapChance + fresh.TeslaTpChance + fresh.InventorySwapChance + fresh.RandomTeleportChance + fresh.HandcuffChance + fresh.CursedCustomItemChance;
        int pranks = fresh.TrollFlashChance + fresh.FakeCassieChance + fresh.ShitPantsChance + fresh.LightsOutChance + fresh.PrimedVaseChance + fresh.RandomTeleportChance + fresh.PlayerSwapChance;
        check(pranks * 2 > tails, "most unlucky flips are harmless pranks by default");
        check(fresh.LuckyTailsPercent == 30, "about a third of tails flips turn lucky, so roughly two in three flips give a goodie");
#endif
    }
}
