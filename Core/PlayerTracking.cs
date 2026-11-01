using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using UnityEngine;

namespace Brainless.Core
{
    public sealed partial class Plugin
    {
        private readonly Dictionary<Player, float> playtime = new Dictionary<Player, float>();
        private readonly Dictionary<Player, long> playtimeConsent = new Dictionary<Player, long>();
        private CoroutineHandle privacyLoop;
        private void StartPrivacy()
        {
            Exiled.Events.Handlers.Player.Verified += PrivacyVerified;
            Exiled.Events.Handlers.Player.Left += PrivacyLeft;
            foreach (var player in Player.List.Where(p => p.IsConnected && !p.IsHost))
                PrivacyVerified(new VerifiedEventArgs(player));
            privacyLoop = Timing.RunCoroutine(TrackPlaytime());
        }

        private void StopPrivacy(bool flush = true)
        {
            Exiled.Events.Handlers.Player.Verified -= PrivacyVerified;
            Exiled.Events.Handlers.Player.Left -= PrivacyLeft;
            Timing.KillCoroutines(privacyLoop);
            if (flush)
                foreach (var player in playtime.Keys.ToArray())
                    try
                    {
                        FlushPlaytime(player);
                    }
                    catch (Exception failure)
                    {
                        messages.Enqueue("Final playtime save failed (" + failure.GetType().Name + ").");
                    }

            playtime.Clear();
            playtimeConsent.Clear();
        }

        internal bool CanTrack(Player player)
        {
            if (Store == null || player == null || player.IsHost || string.IsNullOrWhiteSpace(player.UserId))
                return false;
            Store.SetDoNotTrack(player.UserId, player.DoNotTrack);
            return Store.CanTrack(player.UserId);
        }

        private void PrivacyVerified(VerifiedEventArgs ev)
        {
            if (CanTrack(ev.Player))
            {
                playtime[ev.Player] = Time.realtimeSinceStartup;
                playtimeConsent[ev.Player] = Store.PrivacyRevision;
            }
        }

        private void PrivacyLeft(LeftEventArgs ev) => FlushPlaytime(ev.Player, true);
        private void FlushPlaytime(Player player, bool remove = false)
        {
            if (!playtime.TryGetValue(player, out float last))
                return;
            if (CanTrack(player))
            {
                if (!playtimeConsent.TryGetValue(player, out long consent) || consent != Store.PrivacyRevision)
                {
                    playtime[player] = Time.realtimeSinceStartup;
                    playtimeConsent[player] = Store.PrivacyRevision;
                    if (remove)
                    {
                        playtime.Remove(player);
                        playtimeConsent.Remove(player);
                    }

                    return;
                }

                long seconds = (long)Math.Max(0, Time.realtimeSinceStartup - last);
                if (seconds > 0)
                    Store.AddPlaytime(player.UserId, player.Nickname, seconds);
                playtime[player] = last + seconds;
            }
            else
                remove = true;
            if (remove)
            {
                playtime.Remove(player);
                playtimeConsent.Remove(player);
            }
        }

        private IEnumerator<float> TrackPlaytime()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(1);
                foreach (var player in Player.List.Where(p => p.IsConnected && !p.IsHost).ToArray())
                {
                    try
                    {
                        if (!CanTrack(player))
                        {
                            playtime.Remove(player);
                            playtimeConsent.Remove(player);
                            continue;
                        }

                        if (!playtime.ContainsKey(player))
                        {
                            playtime[player] = Time.realtimeSinceStartup;
                            playtimeConsent[player] = Store.PrivacyRevision;
                        }
                        else
                            FlushPlaytime(player);
                    }
                    catch (Exception failure)
                    {
                        playtime.Remove(player);
                        playtimeConsent.Remove(player);
                        messages.Enqueue("Privacy/playtime update failed (" + failure.GetType().Name + "); stored-data cleanup will retry.");
                    }
                }
            }
        }
    }
}
