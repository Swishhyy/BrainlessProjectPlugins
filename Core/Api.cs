using System;

namespace Brainless.Core
{
    public static class Api
    {
        public static bool IsReady => Plugin.Instance?.Store != null;

        public static bool CanTrack(string userId)
        {
            var plugin = Plugin.Instance;
            if (plugin?.Store == null)
                return false;
            var player = Exiled.API.Features.Player.Get(userId);
            return player != null && player.IsConnected ? plugin.CanTrack(player) : plugin.Store.CanTrack(userId);
        }

        public static bool RecordAction(string userId, string name, string action, int xp, string eventId) => CanTrack(userId) && (Plugin.Instance?.Store?.Record(userId, name, action, xp, eventId) ?? false);
        public static PlayerStats GetPlayer(string userId) => Plugin.Instance?.Store?.Get(userId);
        public static PlayerStats[] Leaderboard(int count = 10) => Plugin.Instance?.Store?.Top(count) ?? Array.Empty<PlayerStats>();
    }
}
