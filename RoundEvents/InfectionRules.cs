using System;
using System.Collections.Generic;

namespace Brainless.RoundEvents
{
    internal enum InfectionAttack
    {
        Other,
        Slap,
        Strangulation
    }

    internal enum InfectionOutcome
    {
        Continue,
        Humans,
        Infected,
        Draw
    }

    internal static class InfectionRules
    {
        internal static int SeedCount(int players, int fixedCount, double percentage, int maximum, int minimumSurvivors)
        {
            int requested = Math.Max(fixedCount, (int)Math.Ceiling(players * percentage / 100));
            return Math.Max(0, Math.Min(Math.Min(requested, maximum), players - minimumSurvivors));
        }

        internal static bool Infects(bool active, bool humanVictim, bool ownInfectedAttacker, bool differentPlayers, InfectionAttack attack, bool slap, bool strangle) => active && humanVictim && ownInfectedAttacker && differentPlayers && (attack == InfectionAttack.Slap && slap || attack == InfectionAttack.Strangulation && strangle);
        internal static InfectionOutcome Outcome(int humans, int infected, int pending)
        {
            if (pending > 0 || humans > 0 && infected > 0)
                return InfectionOutcome.Continue;
            if (humans > 0)
                return InfectionOutcome.Humans;
            if (infected > 0)
                return InfectionOutcome.Infected;
            return InfectionOutcome.Draw;
        }
    }

    // Session + dead-role identity stops a delayed conversion from overwriting a
    // newer life, reconnect, another event, or a stopped/restarted round.
    internal sealed class InfectionState<T>
        where T : class
    {
        private readonly Dictionary<string, object> infected = new Dictionary<string, object>();
        private readonly Dictionary<string, PendingInfection<T>> pending = new Dictionary<string, PendingInfection<T>>();
        internal bool Active { get; private set; }
        internal int Epoch { get; private set; }
        internal int PendingCount => pending.Count;

        internal void Start()
        {
            Stop();
            Active = true;
        }

        internal void Stop()
        {
            Active = false;
            Epoch++;
            infected.Clear();
            pending.Clear();
        }

        internal bool IsInfected(string user, object life) => user != null && life != null && infected.TryGetValue(user, out var owned) && ReferenceEquals(owned, life);
        internal void MarkInfected(string user, object life)
        {
            if (!Active || user == null || life == null)
                return;
            pending.Remove(user);
            infected[user] = life;
        }

        internal void LifeChanged(string user, object life)
        {
            if (user == null)
                return;
            if (infected.TryGetValue(user, out var old) && !ReferenceEquals(old, life))
                infected.Remove(user);
            if (pending.TryGetValue(user, out var ticket) && !ReferenceEquals(ticket.DeadLife, life))
                pending.Remove(user);
        }

        internal PendingInfection<T> Queue(string user, object deadLife, T payload)
        {
            if (!Active || user == null || deadLife == null || payload == null || pending.ContainsKey(user))
                return null;
            var ticket = new PendingInfection<T>(user, deadLife, payload, Epoch);
            pending[user] = ticket;
            return ticket;
        }

        internal bool Take(PendingInfection<T> ticket, object actualLife)
        {
            if (ticket == null || !Active || ticket.Epoch != Epoch || !pending.TryGetValue(ticket.User, out var current) || !ReferenceEquals(current, ticket))
                return false;
            pending.Remove(ticket.User);
            return ReferenceEquals(ticket.DeadLife, actualLife);
        }

        internal void Cancel(PendingInfection<T> ticket)
        {
            if (ticket != null && pending.TryGetValue(ticket.User, out var current) && ReferenceEquals(current, ticket))
                pending.Remove(ticket.User);
        }

        internal void Forget(string user)
        {
            if (user == null)
                return;
            pending.Remove(user);
            infected.Remove(user);
        }
    }

    internal sealed class PendingInfection<T>
        where T : class
    {
        internal readonly string User;
        internal readonly object DeadLife;
        internal readonly T Payload;
        internal readonly int Epoch;
        internal PendingInfection(string user, object deadLife, T payload, int epoch)
        {
            User = user;
            DeadLife = deadLife;
            Payload = payload;
            Epoch = epoch;
        }
    }
}
