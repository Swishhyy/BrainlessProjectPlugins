using System;
using System.Collections.Generic;

namespace Brainless.Internal
{
    // Compiled into each provider; no shared DLL or cross-plugin state.
    internal sealed class DistributionLedger
    {
        private readonly HashSet<object> rolledLives = new HashSet<object>();
        private readonly Dictionary<string, int> playerAwards = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<object, int> roomAwards = new Dictionary<object, int>();
        internal int MapAwards { get; private set; }
        internal int ExtraAwards { get; private set; }
        internal int PlayerAwards { get; private set; }
        internal bool MapProcessed { get; private set; }

        internal bool BeginMap()
        {
            if (MapProcessed)
                return false;
            MapProcessed = true;
            return true;
        }

        internal bool BeginLife(object life) => life != null && rolledLives.Add(life);
        internal bool CanGive(string user, int roundCap, int userCap) => !string.IsNullOrWhiteSpace(user) && PlayerAwards < roundCap && (!playerAwards.TryGetValue(user, out int count) || count < userCap) && userCap > 0;
        internal bool TryGive(string user, int roundCap, int userCap, Func<bool> issue)
        {
            if (!CanGive(user, roundCap, userCap) || !issue())
                return false;
            playerAwards[user] = playerAwards.TryGetValue(user, out int count) ? count + 1 : 1;
            PlayerAwards++;
            return true;
        }

        internal bool CanPlace(object room, int roomCap) => room != null && (!roomAwards.TryGetValue(room, out int count) || count < roomCap) && roomCap > 0;
        internal bool TryPlace(object room, int roomCap, int channelCap, bool extra, Func<bool> issue)
        {
            if ((extra ? ExtraAwards : MapAwards) >= channelCap || !CanPlace(room, roomCap) || !issue())
                return false;
            roomAwards[room] = roomAwards.TryGetValue(room, out int count) ? count + 1 : 1;
            if (extra)
                ExtraAwards++;
            else
                MapAwards++;
            return true;
        }

        internal void Reset()
        {
            rolledLives.Clear();
            playerAwards.Clear();
            roomAwards.Clear();
            MapAwards = ExtraAwards = PlayerAwards = 0;
            MapProcessed = false;
        }
    }

    internal static class DistributionRules
    {
        internal static bool Roll(float percent, Random random) => !float.IsNaN(percent) && !float.IsInfinity(percent) && percent > 0 && random.NextDouble() * 100 < percent;
        internal static void Shuffle<T>(IList<T> values, Random random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T swap = values[i];
                values[i] = values[j];
                values[j] = swap;
            }
        }
    }
}
