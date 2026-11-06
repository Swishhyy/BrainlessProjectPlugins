using Exiled.API.Features;

namespace Brainless.ScpProxChat
{
    // Read by Brainless Roles through reflection for its HUD; no hard DLL reference.
    public static class VoiceApi
    {
        // "team", "nearby" or "mimic" for a living SCP with a voice choice; otherwise null.
        public static string HudStatus(Player player) => Plugin.Instance?.HudStatus(player);
    }
}
