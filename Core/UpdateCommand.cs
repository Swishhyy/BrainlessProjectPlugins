using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandSystem;
using Exiled.API.Features;
using MEC;

namespace Brainless.Core
{
    // pbupdate: check every Brainless plugin for updates right now, install them, and
    // restart so they load. The restart waits for the round to end (the game's own
    // "restart next round"); "pbupdate now", or running it in the lobby, restarts after a
    // short countdown instead.
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public sealed class UpdateCommand : ICommand
    {
        private static bool running;
        public string Command => "pbupdate";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Check all Brainless plugins for updates now, install them and restart at the end of the round (pbupdate now = restart right away).";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Brainless.Internal.Perms.Require(sender, true, out response))
                return false;
            bool now = arguments.Count == 1 && arguments.Array[arguments.Offset].Equals("now", StringComparison.OrdinalIgnoreCase);
            if (arguments.Count > 1 || (arguments.Count == 1 && !now))
            {
                response = "Usage: pbupdate [now]";
                return false;
            }

            if (Plugin.Instance == null)
            {
                response = "Core is not enabled.";
                return false;
            }

            var updaters = UpdateHub.Updaters();
            if (updaters.Length == 0)
            {
                response = "No updaters are running. Check project_brainless_core.auto_update (enabled and token).";
                return false;
            }

            if (running)
            {
                response = "An update check is already running.";
                return false;
            }

            running = true;
            var reply = Player.Get(sender);
            Timing.RunCoroutine(Run(updaters, now, reply));
            response = "Checking " + updaters.Length + " plugin(s) for updates. Results follow in a few seconds.";
            return true;
        }

        private static IEnumerator<float> Run(Internal.Updates.PluginUpdates[] updaters, bool now, Player reply)
        {
            // Network work happens off the main thread; results are handled back here.
            var task = Task.Run(async () =>
            {
                var lines = new List<string>();
                bool restart = false;
                foreach (var updater in updaters)
                {
                    string status = await updater.CheckNowAsync().ConfigureAwait(false);
                    lines.Add(updater.Name + ": " + status);
                    restart |= updater.RestartNeeded;
                }

                return (lines, restart);
            });
            while (!task.IsCompleted)
                yield return Timing.WaitForSeconds(.5f);
            running = false;
            if (task.IsFaulted)
            {
                Report(reply, "Update check failed (" + task.Exception?.GetBaseException().GetType().Name + ").");
                yield break;
            }

            var (results, restartNeeded) = task.Result;
            string summary = string.Join("\n", results);
            if (!restartNeeded)
            {
                Report(reply, "Everything is up to date; no restart needed.\n" + summary);
                yield break;
            }

            if (now || !Round.IsStarted)
            {
                Report(reply, "Updates installed. Restarting in 15 seconds.\n" + summary);
                Map.Broadcast(10, "<size=30><b>Server restarting for an update in 15 seconds.</b></size>", Broadcast.BroadcastFlags.Normal, true);
                yield return Timing.WaitForSeconds(15);
                Server.Restart();
                yield break;
            }

            ServerStatic.StopNextRound = ServerStatic.NextRoundAction.Restart;
            Report(reply, "Updates installed. The server restarts when this round ends (pbupdate now restarts right away).\n" + summary);
            Map.Broadcast(8, "<size=28>The server will restart after this round for an update.</size>", Broadcast.BroadcastFlags.Normal, true);
        }

        private static void Report(Player reply, string text)
        {
            Log.Info("[pbupdate] " + text);
            if (reply != null && reply.IsConnected)
                reply.RemoteAdminMessage(text, true, "pbupdate");
        }
    }
}
