using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using Exiled.CustomItems.API.Features;
using UnityEngine;

namespace Brainless.Internal
{
    // Rare cursed loot: at round start, a small share of matching native map pickups is
    // swapped for a cursed custom item in the same spot (same position, rotation and
    // physics state, so locker items stay put). Kept separate from the main loot
    // percentages so cursed items do not use up their shared per-round replacements.
    internal static class CursedLoot
    {
        internal sealed class Entry
        {
            internal uint Id;
            internal ItemType[] Sources;
            // Spawns the custom item at a position and returns the pickup, or null.
            internal Func<Vector3, Pickup> Spawn;
        }

        internal static int Run(IEnumerable<Entry> entries, double chancePercent, int maxPerItem, System.Random random)
        {
            if (chancePercent <= 0 || maxPerItem <= 0)
                return 0;
            int total = 0;
            var pickups = Pickup.List.Where(p => p != null && p.Base != null && !CustomItem.Registered.Any(c => c.TrackedSerials.Contains(p.Serial))).OrderBy(_ => random.Next()).ToList();
            foreach (var entry in entries)
            {
                int made = 0;
                foreach (var original in pickups.Where(p => p.Base != null && entry.Sources.Contains(p.Type)).ToArray())
                {
                    if (made >= maxPerItem)
                        break;
                    if (random.NextDouble() * 100 >= chancePercent)
                        continue;
                    var position = original.Position;
                    var rotation = original.Rotation;
                    bool kinematic = original.Rigidbody != null && original.Rigidbody.isKinematic;
                    var cursed = entry.Spawn(position);
                    if (cursed == null)
                        continue;
                    cursed.Rotation = rotation;
                    if (cursed.Rigidbody != null)
                        cursed.Rigidbody.isKinematic = kinematic;
                    pickups.Remove(original);
                    original.Destroy();
                    made++;
                }

                total += made;
            }

            return total;
        }
    }
}
