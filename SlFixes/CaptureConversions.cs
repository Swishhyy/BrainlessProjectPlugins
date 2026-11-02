using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;

namespace Brainless.SlFixes
{
    public sealed partial class Plugin
    {
        private void CaptiveEscaping(EscapingEventArgs ev)
        {
            var player = ev.Player;
            if (!Round.IsStarted || !Real(player) || !player.IsAlive || !player.IsHuman)
                return;
            if (!Config.CaptureConversions.Enabled)
            {
                GuardEscaping(ev);
                return;
            }

            var captor = player.Cuffer;
            CaptureSide side = captor?.Role.Team == Team.ChaosInsurgency ? CaptureSide.Chaos : captor?.Role.Team == Team.FoundationForces ? CaptureSide.Foundation : CaptureSide.None;
            var destination = CaptureRules.Destination(side, player.Role.Team == Team.FoundationForces, player.Role.Team == Team.ChaosInsurgency, RoleMarkers.IsRogue(player), player.IsCuffed, captor != null && captor.IsConnected && captor.IsAlive, player.Role.Base.ActiveTime, Config.CaptureConversions.MinimumAliveSeconds);
            if (destination == CaptureSide.None)
            {
                GuardEscaping(ev);
                return;
            }

            // This is called from the game's actual escape-zone handler. Grant
            // our custom scenario early; later plugins can still cancel escape.
            ev.NewRole = destination == CaptureSide.Chaos ? Config.CaptureConversions.ChaosRole : Config.CaptureConversions.MtfRole;
            ev.EscapeScenario = EscapeScenario.CustomEscape;
            ev.IsAllowed = true;
        }

        // Facility Guards are natively unable to escape. An uncuffed, non-rogue guard
        // who reaches the surface escape zone joins MTF instead.
        private void GuardEscaping(EscapingEventArgs ev)
        {
            var player = ev.Player;
            var rule = Config.GuardEscape;
            if (rule == null || !rule.Enabled || player.Role.Type != RoleTypeId.FacilityGuard || player.IsCuffed || RoleMarkers.IsRogue(player) || player.Role.Base.ActiveTime < rule.MinimumAliveSeconds)
                return;
            ev.NewRole = rule.Role;
            ev.EscapeScenario = EscapeScenario.CustomEscape;
            ev.IsAllowed = true;
        }
    }
}
