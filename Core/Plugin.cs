using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Brainless.Internal.Updates;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;

namespace Brainless.Core
{
    public sealed partial class Plugin : Plugin<Config>
    {
        public override string Name => "ProjectBrainlessCore";
        public override string Prefix => "project_brainless_core";
        public override string Author => "Project Brainless";
        public override Version Version => new Version(0, 3, 3);
        public override Version RequiredExiledVersion => new Version(9, 15, 3);
        public override PluginPriority Priority => PluginPriority.Highest;
        public static Plugin Instance { get; private set; }
        public StatsStore Store { get; private set; }
        internal StatsApi Http { get; private set; }

        private readonly ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        private CancellationTokenSource stop;
        private Task saver;
        private CoroutineHandle logger;
        private IDisposable updates;
        public override void OnEnabled()
        {
            if (Config.StatsApi == null || Config.AutoUpdate == null || Config.SaveIntervalSeconds < 5 || Config.SaveIntervalSeconds > 3600 || Config.XpPerLevelSquared < 1 || Config.MaxXpPerPlayerPerRound < 0 || Config.MaxXpPerPlayerPerRound > 100000)
                throw new InvalidOperationException("Invalid Core stats/save configuration.");
            Config.StatsApi.Validate();
            Instance = this;
            UpdateHub.Enable();
            updates = PluginUpdates.Start(GetType().Assembly, "Core", Config.AutoUpdate);
            logger = Timing.RunCoroutine(FlushMessages());
            try
            {
                string directory = string.IsNullOrWhiteSpace(Config.DataDirectory) ? Path.Combine(Paths.Configs, "ProjectBrainless", Server.Port.ToString()) : Path.GetFullPath(Config.DataDirectory);
                Store = new StatsStore(Path.Combine(directory, "stats.json"), Config.MaxXpPerPlayerPerRound);
                StartPrivacy();
                if (Store.RecoveredBackup)
                    Log.Warn("[Project Brainless Core] Restored XP data from backup; the damaged file was preserved.");
                stop = new CancellationTokenSource();
                var cancellation = stop.Token;
                var store = Store;
                saver = Task.Run(async () =>
                {
                    try
                    {
                        while (!cancellation.IsCancellationRequested)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(Config.SaveIntervalSeconds), cancellation).ConfigureAwait(false);
                            try
                            {
                                store.Save();
                            }
                            catch (Exception failure)
                            {
                                messages.Enqueue("XP save failed (" + failure.GetType().Name + "); in-memory stats remain available.");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });
                if (Config.StatsApi.Enabled)
                {
                    try
                    {
                        Http = new StatsApi(Config.StatsApi, store, Config.XpPerLevelSquared, messages.Enqueue, () => serverStatus, () => IdleMode.IdleModeActive);
                    }
                    catch (Exception failure)
                    {
                        Log.Warn("[Project Brainless Core] Stats API could not start on port " + Config.StatsApi.Port + " (" + failure.GetType().Name + ": " + failure.Message + "). Stats and gameplay remain available; check that the port is free and forwarded.");
                    }
                }
            }
            catch (Exception failure)
            {
                StopPrivacy(false);
                Store = null;
                Log.Error("[Project Brainless Core] Stats unavailable (" + failure.GetType().Name + "). Existing data was preserved; shared update service remains enabled.");
            }

            Exiled.Events.Handlers.Server.RoundStarted += RoundStarted;
            base.OnEnabled();
        }

        private void RoundStarted() => Store?.BeginRound();
        private volatile ServerSnapshot serverStatus;

        // Runs on the game thread: flushes log lines and refreshes the server status
        // snapshot that the stats API serves at /api/v1/status. It checks a real-time
        // clock every frame instead of waiting on game time, because the server's idle
        // mode slows game time to 1% (and runs at 1 FPS) when nobody is online.
        private System.Collections.Generic.IEnumerator<float> FlushMessages()
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            long next = 0;
            while (true)
            {
                if (clock.ElapsedMilliseconds < next)
                {
                    yield return Timing.WaitForOneFrame;
                    continue;
                }

                next = clock.ElapsedMilliseconds + 1000;
                while (messages.TryDequeue(out string message))
                    Log.Info("[Project Brainless Core] " + message);
                try
                {
                    serverStatus = new ServerSnapshot(Player.List.Count(p => p != null && p.IsConnected && !p.IsHost && !p.IsNPC), Server.MaxPlayerCount, Round.IsStarted, Round.IsStarted ? (int)Round.ElapsedTime.TotalSeconds : 0, DateTime.UtcNow);
                }
                catch (Exception)
                {
                    // The next tick tries again; a stale snapshot reports offline.
                }

                yield return Timing.WaitForOneFrame;
            }
        }

        public override void OnDisabled()
        {
            StopPrivacy();
            Exiled.Events.Handlers.Server.RoundStarted -= RoundStarted;
            Http?.Dispose();
            Http = null;
            stop?.Cancel();
            try
            {
                Store?.Save();
            }
            catch (Exception failure)
            {
                Log.Warn("[Project Brainless Core] Final XP save failed (" + failure.GetType().Name + ").");
            }

            if (saver != null)
                saver.ContinueWith(_ => stop?.Dispose(), TaskScheduler.Default);
            Timing.KillCoroutines(logger);
            updates?.Dispose();
            UpdateHub.Disable();
            Store = null;
            Instance = null;
            base.OnDisabled();
        }
    }
}
