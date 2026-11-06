using System;
using Brainless.Internal;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Scp079;
using PlayerRoles;

namespace Brainless.Roles
{
    // SCP-079 roles: power regen, action costs, experience, ability cooldowns and the
    // starting tier. 079 has no body, so these replace the usual health/speed stats.
    public sealed partial class Plugin
    {
        private void Subscribe079()
        {
            Exiled.Events.Handlers.Scp079.ChangingCamera += Camera079;
            Exiled.Events.Handlers.Scp079.TriggeringDoor += Door079;
            Exiled.Events.Handlers.Scp079.InteractingTesla += Tesla079;
            Exiled.Events.Handlers.Scp079.LockingDown += Lockdown079;
            Exiled.Events.Handlers.Scp079.RoomBlackout += RoomBlackout079;
            Exiled.Events.Handlers.Scp079.ZoneBlackout += ZoneBlackout079;
            Exiled.Events.Handlers.Scp079.ElevatorTeleporting += Elevator079;
            Exiled.Events.Handlers.Scp079.Pinging += Ping079;
            Exiled.Events.Handlers.Scp079.GainingExperience += Experience079;
        }

        private void Unsubscribe079()
        {
            Exiled.Events.Handlers.Scp079.ChangingCamera -= Camera079;
            Exiled.Events.Handlers.Scp079.TriggeringDoor -= Door079;
            Exiled.Events.Handlers.Scp079.InteractingTesla -= Tesla079;
            Exiled.Events.Handlers.Scp079.LockingDown -= Lockdown079;
            Exiled.Events.Handlers.Scp079.RoomBlackout -= RoomBlackout079;
            Exiled.Events.Handlers.Scp079.ZoneBlackout -= ZoneBlackout079;
            Exiled.Events.Handlers.Scp079.ElevatorTeleporting -= Elevator079;
            Exiled.Events.Handlers.Scp079.Pinging -= Ping079;
            Exiled.Events.Handlers.Scp079.GainingExperience -= Experience079;
        }

        private ScpTraits Traits079(Player player) => player?.Role.Type == RoleTypeId.Scp079 ? Traits(player) : null;
        private float Cost(Player player, float cost, float extra = 1)
        {
            var traits = Traits079(player);
            return traits == null ? cost : Math.Max(0, cost * traits.AuxCostMultiplier * extra);
        }

        private void Camera079(ChangingCameraEventArgs ev) => Safe.Run(() => ev.AuxiliaryPowerCost = Cost(ev.Player, ev.AuxiliaryPowerCost, Traits079(ev.Player)?.CameraCostMultiplier ?? 1));
        private void Door079(TriggeringDoorEventArgs ev) => Safe.Run(() => ev.AuxiliaryPowerCost = Cost(ev.Player, ev.AuxiliaryPowerCost, Traits079(ev.Player)?.DoorCostMultiplier ?? 1));
        private void Tesla079(InteractingTeslaEventArgs ev) => Safe.Run(() => ev.AuxiliaryPowerCost = Cost(ev.Player, ev.AuxiliaryPowerCost));
        private void Elevator079(ElevatorTeleportingEventArgs ev) => Safe.Run(() => ev.AuxiliaryPowerCost = Cost(ev.Player, ev.AuxiliaryPowerCost));
        private void Ping079(PingingEventArgs ev) => Safe.Run(() => ev.AuxiliaryPowerCost = Cost(ev.Player, ev.AuxiliaryPowerCost));

        private void Lockdown079(LockingDownEventArgs ev) => Safe.Run(() =>
        {
            ev.AuxiliaryPowerCost = Cost(ev.Player, ev.AuxiliaryPowerCost);
            float multiplier = Traits079(ev.Player)?.AbilityCooldownMultiplier ?? 1;
            if (!ev.IsAllowed || multiplier == 1)
                return;
            var player = ev.Player;
            lifetime.Later(.1f, () =>
            {
                if (player.Role is Exiled.API.Features.Roles.Scp079Role computer && computer.RoomLockdownCooldown > 0)
                    computer.RoomLockdownCooldown *= multiplier;
            });
        });

        private void RoomBlackout079(RoomBlackoutEventArgs ev) => Safe.Run(() =>
        {
            ev.AuxiliaryPowerCost = Cost(ev.Player, ev.AuxiliaryPowerCost);
            ev.Cooldown *= Traits079(ev.Player)?.AbilityCooldownMultiplier ?? 1;
        });

        private void ZoneBlackout079(ZoneBlackoutEventArgs ev) => Safe.Run(() =>
        {
            ev.AuxiliaryPowerCost = Cost(ev.Player, ev.AuxiliaryPowerCost);
            ev.Cooldown *= Traits079(ev.Player)?.AbilityCooldownMultiplier ?? 1;
        });

        private void Experience079(GainingExperienceEventArgs ev) => Safe.Run(() =>
        {
            float multiplier = Traits079(ev.Player)?.ExperienceMultiplier ?? 1;
            if (multiplier != 1)
                ev.Amount = (int)Math.Round(ev.Amount * multiplier);
        });

        // Runs on the 0.2-second role tick: scales the native regeneration up or down.
        private static void Update079(Player player, ActiveRole state)
        {
            var traits = state.Definition.Traits;
            if (traits.AuxRegenMultiplier == 1 || !(player.Role is Exiled.API.Features.Roles.Scp079Role computer) || computer.Energy >= computer.MaxEnergy || computer.IsLost)
                return;
            float change = computer.EnergyRegenerationSpeed * (traits.AuxRegenMultiplier - 1) * .2f;
            computer.Energy = Math.Max(0, Math.Min(computer.MaxEnergy, computer.Energy + change));
        }

        private static void StartTier079(Player player, RoleDefinition definition)
        {
            int tier = definition.Traits.StartTier;
            if (tier > 0 && player.Role is Exiled.API.Features.Roles.Scp079Role computer && computer.Level < tier)
                computer.Level = tier;
        }
    }
}
