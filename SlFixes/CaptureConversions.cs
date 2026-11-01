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
            if (!Config.CaptureConversions.Enabled || !Round.IsStarted || !Real(player) || !player.IsAlive || !player.IsHuman)
                return;
            var captor = player.Cuffer;
            CaptureSide side = captor?.Role.Team == Team.ChaosInsurgency ? CaptureSide.Chaos : captor?.Role.Team == Team.FoundationForces ? CaptureSide.Foundation : CaptureSide.None;
            var destination = CaptureRules.Destination(side, player.Role.Team == Team.FoundationForces, player.Role.Team == Team.ChaosInsurgency, RoleMarkers.IsRogue(player), player.IsCuffed, captor != null && captor.IsConnected && captor.IsAlive, player.Role.Base.ActiveTime, Config.CaptureConversions.MinimumAliveSeconds);
            if (destination == CaptureSide.None)
                return;
            // This is called from the game's actual escape-zone handler. Grant
            // our custom scenario early; later plugins can still cancel escape.
            ev.NewRole = destination == CaptureSide.Chaos ? Config.CaptureConversions.ChaosRole : Config.CaptureConversions.MtfRole;
            ev.EscapeScenario = EscapeScenario.CustomEscape;
            ev.IsAllowed = true;
        }
    }
}
