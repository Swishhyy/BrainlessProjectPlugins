using System;

namespace Brainless.RoundEvents
{
    // Text for the Juggernaut tracker shown to hunters.
    internal static class TrackerRules
    {
        private static readonly string[] Arrows = { "↑", "↗", "→", "↘", "↓", "↙", "←", "↖" };

        // Arrow pointing from the viewer's facing direction (fx, fz) toward the
        // offset (dx, dz) on the floor plane; ↑ means straight ahead.
        internal static string Arrow(double fx, double fz, double dx, double dz)
        {
            if (Math.Abs(dx) < .01 && Math.Abs(dz) < .01)
                return "•";
            double angle = Math.Atan2(dx, dz) - Math.Atan2(fx, fz);
            double degrees = (angle * 180 / Math.PI % 360 + 360) % 360;
            return Arrows[(int)Math.Round(degrees / 45) % 8];
        }

        internal static string Height(double dy) => dy > 8 ? " above you" : dy < -8 ? " below you" : "";

        internal static string Describe(string zone, string room, double distance, string arrow, string height, double health) =>
            $"<color=#e74c3c><b>JUGGERNAUT</b></color> {arrow} <b>{Math.Round(distance)} m</b>{height}\n{zone}{(string.IsNullOrEmpty(room) ? "" : " · " + room)} · {Math.Round(health)} HP";
    }
}
