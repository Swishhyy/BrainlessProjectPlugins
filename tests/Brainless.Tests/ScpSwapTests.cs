using System;

internal static class ScpSwapTests
{
    internal static void Run(Action<bool, string> check)
    {
#if NATIVE_CONFIG_TESTS
        bool Parses(string text, PlayerRoles.RoleTypeId expected) => Brainless.ScpSwap.SwapRules.TryParse(text, out var role) && role == expected;
        check(Parses("173", PlayerRoles.RoleTypeId.Scp173) && Parses("scp173", PlayerRoles.RoleTypeId.Scp173) && Parses("SCP-173", PlayerRoles.RoleTypeId.Scp173) && Parses("peanut", PlayerRoles.RoleTypeId.Scp173), "SCP swap accepts numbers, scp-prefixed and nickname forms");
        check(Parses("096", PlayerRoles.RoleTypeId.Scp096) && Parses("96", PlayerRoles.RoleTypeId.Scp096) && Parses("079", PlayerRoles.RoleTypeId.Scp079) && Parses("3114", PlayerRoles.RoleTypeId.Scp3114), "SCP swap parses leading-zero and short SCP numbers");
        check(!Brainless.ScpSwap.SwapRules.TryParse("049-2", out _) && !Brainless.ScpSwap.SwapRules.TryParse("0492", out _) && !Brainless.ScpSwap.SwapRules.TryParse("", out _) && !Brainless.ScpSwap.SwapRules.TryParse("scp", out _), "SCP swap never parses SCP-049-2 or empty input");
        check(Brainless.ScpSwap.SwapRules.InWindow(0, 60) && Brainless.ScpSwap.SwapRules.InWindow(60, 60) && !Brainless.ScpSwap.SwapRules.InWindow(60.5, 60) && !Brainless.ScpSwap.SwapRules.InWindow(5, 0), "SCP swap window is inclusive and a zero window disables player swaps");
        check(Brainless.ScpSwap.SwapRules.HasSwapsLeft(0, 1) && !Brainless.ScpSwap.SwapRules.HasSwapsLeft(1, 1) && Brainless.ScpSwap.SwapRules.HasSwapsLeft(50, 0), "SCP swap per-player limit treats 0 as unlimited");
        check(Brainless.ScpSwap.SwapRules.Name(PlayerRoles.RoleTypeId.Scp939) == "SCP-939", "SCP swap names SCPs in the usual SCP-### form");
#endif
    }
}
