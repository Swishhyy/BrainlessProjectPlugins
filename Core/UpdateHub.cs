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
                var global = Plugin.Instance.Config.AutoUpdate;
                var config = new AutoUpdateConfig
                {
                    Enabled = true,
                    CheckIntervalMinutes = interval,
                    IncludePrereleases = prereleases,
                    Token = string.IsNullOrWhiteSpace(token) ? global.Token : token,
                    TokenEnvironmentVariable = string.IsNullOrWhiteSpace(environmentVariable) ? global.TokenEnvironmentVariable : environmentVariable,
                    ReleasesToScan = scan
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
                updater.Dispose();
                lock (gate)
                    if (registrations.TryGetValue(assembly, out var current) && ReferenceEquals(current, this))
                        registrations.Remove(assembly);
            }
        }
    }
}
