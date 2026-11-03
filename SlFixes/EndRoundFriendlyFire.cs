using Brainless.Internal;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Server;

namespace Brainless.SlFixes
{
    // When the round ends, friendly fire turns on so everyone can fight until the
    // restart. The game already pauses its friendly-fire auto-punish detector at
    // round end. The server's own setting comes back when the next round starts.
    public sealed partial class Plugin
    {
        private bool? friendlyFireBeforeRoundEnd;

        private void EndRoundFriendlyFire(RoundEndedEventArgs ev) => Safe.Run(() =>
        {
            var cfg = Config.EndOfRoundFriendlyFire;
            if (!cfg.Enabled || Server.FriendlyFire)
                return;
            friendlyFireBeforeRoundEnd = false;
            Server.FriendlyFire = true;
            if (cfg.BroadcastSeconds > 0 && !string.IsNullOrWhiteSpace(cfg.Message))
                Map.Broadcast(cfg.BroadcastSeconds, cfg.Message);
            Log.Info("[Project Brainless Fixes] Round over: friendly fire enabled until the next round.");
        });

        private void RestoreFriendlyFire() => Safe.Run(() =>
        {
            if (!friendlyFireBeforeRoundEnd.HasValue)
                return;
            bool previous = friendlyFireBeforeRoundEnd.Value;
            friendlyFireBeforeRoundEnd = null;
            Server.FriendlyFire = previous;
        });
    }
}
