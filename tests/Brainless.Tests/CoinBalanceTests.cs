using System;

internal static class CoinBalanceTests
{
    internal static void Run(Action<bool, string> check)
    {
#if NATIVE_CONFIG_TESTS
        var old = new BetterCoinflips.Configs.Config { TurnIntoScpChance = 30, KickChance = 5, WarheadChance = 10, SpectSwapChance = 10, LiveHeChance = 30, InstantExplosionChance = 10 };
        old.BadEffects.UnionWith(new[] { Exiled.API.Enums.EffectType.Decontaminating, Exiled.API.Enums.EffectType.CardiacArrest, Exiled.API.Enums.EffectType.PocketCorroding });
        check(old.UpgradeBalance() && old.BalanceRevision == 1, "coin balance revision 1 runs once on old configs");
        check(old.TurnIntoScpChance == 8 && old.KickChance == 0 && old.WarheadChance == 3 && old.SpectSwapChance == 3 && old.LiveHeChance == 15 && old.InstantExplosionChance == 4, "untouched harsh tails weights move to the gentler defaults");
        check(!old.BadEffects.Contains(Exiled.API.Enums.EffectType.Decontaminating) && !old.BadEffects.Contains(Exiled.API.Enums.EffectType.CardiacArrest) && !old.BadEffects.Contains(Exiled.API.Enums.EffectType.PocketCorroding) && old.BadEffects.Contains(Exiled.API.Enums.EffectType.Bleeding), "the stock bad-effect list loses only its lethal effects");
        check(!old.UpgradeBalance(), "coin balance migration does not repeat");
        var edited = new BetterCoinflips.Configs.Config { TurnIntoScpChance = 50, KickChance = 5 };
        edited.BadEffects.Add(Exiled.API.Enums.EffectType.Decontaminating);
        edited.UpgradeBalance();
        check(edited.TurnIntoScpChance == 50 && edited.KickChance == 0 && edited.BadEffects.Contains(Exiled.API.Enums.EffectType.Decontaminating), "owner-edited coin weights and custom effect lists are kept");
        var fresh = new BetterCoinflips.Configs.Config();
        int harsh = fresh.KickChance + fresh.TurnIntoScpChance + fresh.WarheadChance + fresh.SpectSwapChance + fresh.InstantExplosionChance + fresh.LiveHeChance + fresh.OneHpLeftChance + fresh.InventoryResetChance + fresh.TeslaTpChance;
        int tails = harsh + fresh.HpReductionChance + fresh.TpToClassDCellsChance + fresh.RandomBadEffectChance + fresh.LightsOutChance + fresh.TrollFlashChance + fresh.ScpTpChance + fresh.PrimedVaseChance + fresh.ShitPantsChance + fresh.FakeCassieChance + fresh.ClassSwapChance + fresh.PlayerSwapChance + fresh.InventorySwapChance + fresh.RandomTeleportChance + fresh.HandcuffChance + fresh.CursedCustomItemChance;
        check(harsh * 100 < tails * 20, "round-ruining tails outcomes are under 20% of tails by default");
#endif
    }
}
