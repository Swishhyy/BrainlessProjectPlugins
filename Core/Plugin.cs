using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Brainless.Internal.Updates;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;

namespace Brainless.Core
{
    public sealed class Plugin : Plugin<Config>
    {
        public override string Name => "ProjectBrainlessCore";
        public override string Prefix => "project_brainless_core";
        public override string Author => "Project Brainless";
        public override Version Version => new Version(0, 1, 1);
        public override Version RequiredExiledVersion => new Version(9, 15, 2);
        public override PluginPriority Priority => PluginPriority.Highest;
        public static Plugin Instance { get; private set; }
        public StatsStore Store { get; private set; }
        internal DiscordBot Bot { get; private set; }

        private readonly ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        private CancellationTokenSource stop;
        private Task saver;
        private CoroutineHandle logger;
        private IDisposable updates;
        public override void OnEnabled()
        {
            if (Config.Discord == null || Config.AutoUpdate == null || Config.SaveIntervalSeconds < 5 || Config.SaveIntervalSeconds > 3600 || Config.XpPerLevelSquared < 1 || Config.MaxXpPerPlayerPerRound < 0 || Config.MaxXpPerPlayerPerRound > 100000)
                throw new InvalidOperationException("Invalid Core stats/save configuration.");
            Instance = this;
            UpdateHub.Enable();
            updates = PluginUpdates.Start(GetType().Assembly, "Core", Config.AutoUpdate);
            logger = Timing.RunCoroutine(FlushMessages());
            try
            {
                string directory = string.IsNullOrWhiteSpace(Config.DataDirectory) ? Path.Combine(Paths.Configs, "ProjectBrainless", Server.Port.ToString()) : Path.GetFullPath(Config.DataDirectory);
                Store = new StatsStore(Path.Combine(directory, "stats.json"), Config.MaxXpPerPlayerPerRound);
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
                if (Config.Discord.Enabled)
                {
                    try
                    {
                        Bot = new DiscordBot(Config.Discord, store, Config.XpPerLevelSquared, messages.Enqueue);
                    }
                    catch (Exception failure)
                    {
                        Log.Warn("[Project Brainless Core] Discord bot could not start (" + failure.GetType().Name + "). Stats and gameplay remain available; check discord.token and settings.");
                    }
                }
            }
            catch (Exception failure)
            {
                Log.Error("[Project Brainless Core] Stats unavailable (" + failure.GetType().Name + "). Existing data was preserved; shared update service remains enabled.");
            }

            Exiled.Events.Handlers.Server.RoundStarted += RoundStarted;
            base.OnEnabled();
        }

        private void RoundStarted() => Store?.BeginRound();
        private System.Collections.Generic.IEnumerator<float> FlushMessages()
        {
            while (true)
            {
                while (messages.TryDequeue(out string message))
                    Log.Info("[Project Brainless Core] " + message);
                yield return Timing.WaitForSeconds(1);
            }
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= RoundStarted;
            Bot?.Dispose();
            Bot = null;
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
