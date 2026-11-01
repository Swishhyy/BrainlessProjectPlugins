using System.Linq;
using Brainless.Internal;
using Exiled.API.Features;
using MEC;

namespace Brainless.SlFixes
{
    public sealed partial class Plugin
    {
        private bool Trackable(Player player) => Real(player) && !player.DoNotTrack && CoreBridge.CanTrack(player.UserId);
        private void ForgetTracking(Player player)
        {
            lives.Remove(player);
            participants.Remove(player.UserId);
            foreach (string key in xpAwards.Keys.Where(k => k.StartsWith(player.UserId + ":", System.StringComparison.Ordinal)).ToArray())
                xpAwards.Remove(key);
        }

        private System.Collections.Generic.IEnumerator<float> PrivacyCleanup()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(1);
                foreach (var player in lives.Keys.ToArray())
                    if (!Trackable(player))
                        ForgetTracking(player);
                foreach (string user in participants.Keys.ToArray())
                    if (!CoreBridge.CanTrack(user))
                    {
                        participants.Remove(user);
                        foreach (string key in xpAwards.Keys.Where(k => k.StartsWith(user + ":", System.StringComparison.Ordinal)).ToArray())
                            xpAwards.Remove(key);
                    }
            }
        }
    }
}
