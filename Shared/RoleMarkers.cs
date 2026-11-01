using Exiled.API.Features;

namespace Brainless.Internal
{
    internal static class RoleMarkers
    {
        internal const string Rogue = "project_brainless.rogue_life";
        internal static bool IsRogue(Player player) => player != null && player.IsAlive && player.SessionVariables.TryGetValue(Rogue, out var life) && ReferenceEquals(life, player.Role.Base);
    }
}
