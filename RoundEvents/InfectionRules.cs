using System;
using System.Collections.Generic;
using System.Linq;

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

namespace Brainless.RoundEvents
{
    // Chooses start rooms for Infection so nobody begins on top of a skeleton:
    // skeletons are spread as far apart as possible, survivors only use rooms at
    // least minimumGap away from every skeleton, and survivors fill different
    // rooms before any room gets a second player.
    internal static class SpreadPlanner
    {
        internal struct Point
        {
            internal double X, Y, Z;
            internal Point(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            internal double Distance(Point other) => Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y) + (Z - other.Z) * (Z - other.Z));
        }

        // Returns one room index per player: the first `infected` entries are the
        // skeletons, the rest are survivors. Empty input gives -1 for everyone.
        internal static int[] Plan(IReadOnlyList<Point> rooms, int infected, int humans, double minimumGap, Random random)
        {
            var result = new int[Math.Max(0, infected) + Math.Max(0, humans)];
            if (rooms == null || rooms.Count == 0)
            {
                for (int i = 0; i < result.Length; i++)
                    result[i] = -1;
                return result;
            }

            var order = new List<int>();
            for (int i = 0; i < rooms.Count; i++)
                order.Insert(random.Next(order.Count + 1), i);

            var skeletons = new List<int>();
            for (int i = 0; i < infected; i++)
            {
                int pick = Farthest(rooms, order, skeletons, skeletons);
                skeletons.Add(pick);
                result[i] = pick;
            }

            double Nearest(int room) => skeletons.Count == 0 ? double.MaxValue : skeletons.Min(s => rooms[room].Distance(rooms[s]));
            var safe = order.Where(r => !skeletons.Contains(r) && Nearest(r) >= minimumGap).ToList();
            if (safe.Count == 0)
            {
                // Small or crowded map: use the half of the free rooms farthest from skeletons.
                var free = order.Where(r => !skeletons.Contains(r)).OrderByDescending(Nearest).ToList();
                safe = free.Count == 0 ? order.OrderByDescending(Nearest).Take(Math.Max(1, order.Count / 2)).ToList() : free.Take(Math.Max(1, (free.Count + 1) / 2)).ToList();
            }

            var survivors = new List<int>();
            var taken = new List<int>(skeletons);
            for (int i = 0; i < humans; i++)
            {
                int pick;
                if (survivors.Count < safe.Count)
                {
                    pick = Farthest(rooms, safe, taken, survivors);
                    taken.Add(pick);
                }
                else
                    pick = survivors[i % safe.Count]; // every safe room used: share them evenly
                survivors.Add(pick);
                result[infected + i] = pick;
            }

            return result;
        }

        // The candidate whose nearest already-chosen room is farthest away; candidates
        // already in `used` are skipped unless every candidate is used.
        private static int Farthest(IReadOnlyList<Point> rooms, List<int> candidates, List<int> chosen, List<int> used)
        {
            var open = candidates.Where(c => !used.Contains(c)).ToList();
            if (open.Count == 0)
                return candidates[used.Count % candidates.Count];
            if (chosen.Count == 0)
                return open[0];
            int best = open[0];
            double bestGap = -1;
            foreach (int candidate in open)
            {
                double gap = chosen.Min(c => rooms[candidate].Distance(rooms[c]));
                if (gap > bestGap)
                {
                    bestGap = gap;
                    best = candidate;
                }
            }

            return best;
        }
    }
}
