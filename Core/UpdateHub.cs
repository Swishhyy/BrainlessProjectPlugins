using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Brainless.Internal.Updates;

namespace Brainless.Core
{
    public static class UpdateHub
    {
        private static readonly object gate = new object ();
        private static readonly Dictionary<Assembly, Lease> registrations = new Dictionary<Assembly, Lease>();
        private static bool enabled;
        public static int Count
        {
            get
            {
                lock (gate)
                    return registrations.Count;
            }
        }

        private static PluginUpdates core;

        // Core's own updater (it does not register with itself).
        internal static void SetCore(IDisposable updater)
        {
            lock (gate)
                core = updater as PluginUpdates;
        }

        // Every running updater, Core's first, for a forced check (pbupdate).
        internal static PluginUpdates[] Updaters()
        {
            lock (gate)
            {
                var list = new List<PluginUpdates>();
                if (core != null)
                    list.Add(core);
                foreach (var lease in registrations.Values)
                    if (lease.Updater is PluginUpdates updater)
                        list.Add(updater);
                return list.ToArray();
            }
        }

        internal static void Enable()
        {
            lock (gate)
                enabled = true;
        }

        public static IDisposable Register(Assembly assembly, string id, int interval, bool prereleases, string token, string environmentVariable, int scan)
        {
            lock (gate)
            {
                if (!enabled || Plugin.Instance == null)
                    return null;
                if (registrations.TryGetValue(assembly, out var previous))
                    previous.Dispose();
                // Core is in charge: its token, interval, prerelease and scan settings apply
                // to every Brainless plugin. A plugin's own token is only a fallback when
                // Core has none. With Core's updater off, no plugin updates itself either.
                var global = Plugin.Instance.Config.AutoUpdate;
                if (global == null || !global.Enabled)
                    return new Lease(assembly, null);
                var config = new AutoUpdateConfig
                {
                    Enabled = true,
                    CheckIntervalMinutes = global.CheckIntervalMinutes,
                    IncludePrereleases = global.IncludePrereleases,
                    Token = string.IsNullOrWhiteSpace(global.Token) ? token : global.Token,
                    TokenEnvironmentVariable = string.IsNullOrWhiteSpace(global.TokenEnvironmentVariable) ? environmentVariable : global.TokenEnvironmentVariable,
                    ReleasesToScan = global.ReleasesToScan
                };
                var updater = PluginUpdates.StartStandalone(assembly, id, config);
                if (updater == null)
                    return null;
                var lease = new Lease(assembly, updater);
                registrations[assembly] = lease;
                return lease;
            }
        }

        internal static void Disable()
        {
            Lease[] leases;
            lock (gate)
            {
                enabled = false;
                core = null;
                leases = new List<Lease>(registrations.Values).ToArray();
                registrations.Clear();
            }

            foreach (var lease in leases)
                lease.Dispose();
        }

        private sealed class Lease : IDisposable
        {
            private readonly Assembly assembly;
            private readonly IDisposable updater;
            internal IDisposable Updater => updater;
            private int disposed;
            internal Lease(Assembly assembly, IDisposable updater)
            {
                this.assembly = assembly;
                this.updater = updater;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref disposed, 1) != 0)
                    return;
                updater?.Dispose();
                lock (gate)
                    if (registrations.TryGetValue(assembly, out var current) && ReferenceEquals(current, this))
                        registrations.Remove(assembly);
            }
        }
    }
}
