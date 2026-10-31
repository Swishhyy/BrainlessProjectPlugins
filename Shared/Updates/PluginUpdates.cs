using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Exiled.API.Features;
using MEC;

namespace Brainless.Internal.Updates
{
    internal sealed class PluginUpdates : IDisposable
    {
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
        private readonly string label;
        private readonly ReleaseClient client;
        private readonly CoroutineHandle logger;
        private readonly Task worker;
        private bool disposed;
        private PluginUpdates(Assembly assembly, string id, string path, AutoUpdateConfig config)
        {
            label = "[" + assembly.GetName().Name + " updater] ";
            var exiled = Exiled.Loader.Loader.Version;
            var game = new Version(GameCore.Version.Major, GameCore.Version.Minor, GameCore.Version.Revision);
            client = new ReleaseClient(() => config.ResolveToken(Environment.GetEnvironmentVariable));
            logger = Timing.RunCoroutine(WriteMessages());
            Log.Info(label + "Enabled; release checks every " + Math.Max(5, config.CheckIntervalMinutes) + " minutes. New DLLs load after a full server restart.");
            worker = Task.Run(async () =>
            {
                string last = null;
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(20), cancellation.Token).ConfigureAwait(false);
                    while (!cancellation.IsCancellationRequested)
                    {
                        string status;
                        try
                        {
                            status = await client.CheckAndInstallAsync(id, assembly.GetName().Name, path, assembly.GetName().Version, exiled, game, config, cancellation.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)when (cancellation.IsCancellationRequested)
                        {
                            break;
                        }
                        catch (UpdateException exception)
                        {
                            status = exception.Message;
                        }
                        // Do not echo credentials, headers, signed URLs or raw HTTP exception messages.
                        catch (Exception exception)
                        {
                            status = "Update check failed (" + exception.GetType().Name + "); the plugin keeps running and will retry.";
                        }

                        if (status != last)
                        {
                            messages.Enqueue(status);
                            last = status;
                        }

                        await Task.Delay(TimeSpan.FromMinutes(Math.Max(5, Math.Min(1440, config.CheckIntervalMinutes))), cancellation.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)when (cancellation.IsCancellationRequested)
                {
                }
            });
        }

        internal static IDisposable Start(Assembly assembly, string id, AutoUpdateConfig config)
        {
            if (config?.Enabled != true)
                return null;
            if (assembly.GetName().Name != "Brainless.Core")
            {
                try
                {
                    var core = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == "Brainless.Core");
                    var register = core?.GetType("Brainless.Core.UpdateHub")?.GetMethod("Register", BindingFlags.Public | BindingFlags.Static);
                    var shared = register?.Invoke(null, new object[] { assembly, id, config.CheckIntervalMinutes, config.IncludePrereleases, config.Token, config.TokenEnvironmentVariable, config.ReleasesToScan }) as IDisposable;
                    if (shared != null)
                        return shared;
                }
                catch (Exception)
                {
                    Log.Warn("[" + assembly.GetName().Name + " updater] Core service unavailable; using the standalone updater.");
                }
            }

            return StartStandalone(assembly, id, config);
        }

        internal static PluginUpdates StartStandalone(Assembly assembly, string id, AutoUpdateConfig config)
        {
            if (config?.Enabled != true)
                return null;
            try
            {
                if (!Exiled.Loader.Loader.Locations.TryGetValue(assembly, out string path) || string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    Log.Warn("[" + assembly.GetName().Name + " updater] Exact installed DLL path was not found; updater disabled for this instance.");
                    return null;
                }

                return new PluginUpdates(assembly, id, Path.GetFullPath(path), config);
            }
            catch (Exception exception)
            {
                Log.Warn("[" + assembly.GetName().Name + " updater] Could not start (" + exception.GetType().Name + "). Gameplay plugin remains enabled.");
                return null;
            }
        }

        private IEnumerator<float> WriteMessages()
        {
            while (!cancellation.IsCancellationRequested)
            {
                while (messages.TryDequeue(out var message))
                    Log.Info(label + message);
                yield return Timing.WaitForSeconds(1);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            cancellation.Cancel();
            Timing.KillCoroutines(logger);
            // Never block the server thread waiting for network I/O.
            worker.ContinueWith(_ =>
            {
                client.Dispose();
                cancellation.Dispose();
            }, TaskScheduler.Default);
        }
    }
}
