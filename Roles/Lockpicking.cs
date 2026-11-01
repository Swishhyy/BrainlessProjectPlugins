using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.Events.EventArgs.Player;
using Interactables.Interobjects.DoorUtils;
using PlayerRoles;
using UnityEngine;

namespace Brainless.Roles
{
    internal sealed class Lockpicking
    {
        private readonly Config config;
        private readonly Lifetime lifetime;
        private readonly Func<Player, string> roleId;
        private readonly LockpickRules timers = new LockpickRules();
        private readonly Dictionary<Player, Attempt> attempts = new Dictionary<Player, Attempt>();
        private Attempt completing;
        private float nextHint;
        internal Lockpicking(Config config, Lifetime lifetime, Func<Player, string> roleId)
        {
            this.config = config;
            this.lifetime = lifetime;
            this.roleId = roleId;
        }

        internal void DoorInteraction(InteractingDoorEventArgs ev)
        {
            if (!ev.CanInteract || !IsLocksmith(ev.Player))
                return;
            if (completing != null && completing.Player == ev.Player && completing.Door == ev.Door && completing.ColliderId == ev.ColliderId)
            {
                // Grant access only for this synchronous native interaction. Door
                // permissions and lock flags are never edited or permanently cleared.
                if (Failure(completing) == null)
                    ev.IsAllowed = true;
                return;
            }

            // A valid card/ordinary unlocked door keeps its instant native behavior.
            if (ev.IsAllowed || ev.Door == null || ev.Door.IsOpen)
                return;
            lifetime.Later(0, () =>
            {
                // Observe the final event after other EXILED handlers finish.
                if (!ev.CanInteract || ev.IsAllowed || !IsLocksmith(ev.Player) || ev.Door == null || ev.Door.Base == null || ev.Door.IsOpen)
                    return;
                Begin(ev);
            });
        }

        private bool IsLocksmith(Player player) => player != null && player.IsConnected && player.IsAlive && !player.IsCuffed && player.Role.Type == RoleTypeId.ClassD && roleId(player) == "D06";
        private void Begin(InteractingDoorEventArgs ev)
        {
            Player player = ev.Player;
            int level = RequiredLevel(ev.Door);
            var collider = ev.Collider != null ? ev.Collider.GetComponent<Collider>() : null;
            var attempt = new Attempt
            {
                Player = player,
                User = player.UserId,
                Role = player.Role.Base,
                Door = ev.Door,
                ColliderId = ev.ColliderId,
                Point = collider != null ? collider.ClosestPoint(player.CameraTransform.position) : ev.Collider != null ? ev.Collider.transform.position : ev.Door.Position + Vector3.up,
                Level = level,
                Duration = LockpickRules.Duration(level, config.LockpickDurationSeconds, config.LockpickExtraSecondsPerLevel),
                Cooldown = LockpickRules.Cooldown(level, config.LockpickCooldownSeconds, config.LockpickExtraCooldownPerLevel)
            };
            string failure = Failure(attempt);
            if (failure != null)
            {
                player.ShowHint("Cannot lockpick: " + failure, 3);
                return;
            }

            if (attempts.TryGetValue(player, out var previous))
            {
                if (previous.Door == attempt.Door)
                    return;
                Cancel(player);
            }

            float remaining = timers.Remaining(attempt.User, Time.time);
            if (remaining > 0)
            {
                player.ShowHint("Lockpick ready in " + Mathf.CeilToInt(remaining) + " seconds.", 3);
                return;
            }

            if (!timers.TryBegin(attempt.User, Time.time))
                return;
            attempts.Add(player, attempt);
            Progress(attempt, true);
        }

        internal void Update()
        {
            bool hint = Time.time >= nextHint;
            if (hint)
                nextHint = Time.time + .5f;
            foreach (var attempt in attempts.Values.ToArray())
            {
                string failure = Failure(attempt);
                if (failure != null)
                {
                    Cancel(attempt.Player, failure);
                    continue;
                }

                if (timers.CanFinish(attempt.User, Time.time, attempt.Duration))
                    Finish(attempt);
                else if (hint)
                    Progress(attempt, false);
            }
        }

        private string Failure(Attempt attempt)
        {
            Player player = attempt.Player;
            Door door = attempt.Door;
            if (!IsLocksmith(player) || player.Role.Base != attempt.Role)
                return "role changed, restrained, or no longer alive";
            if (door == null || door.Base == null)
                return "door no longer exists";
            if (RequiredLevel(door) != attempt.Level)
                return "door access requirement changed; try again";
            if (door.IsNonInteractable)
                return "door has no interaction mechanism";
            if (door.IsOpen || door.IsMoving)
                return "door opened or started moving";
            if (!InReach(attempt))
                return "stay within reach of the door control";
            return null;
        }

        private static int RequiredLevel(Door door)
        {
            if (door == null)
                return 0;
            int permissions = (int)door.KeycardPermissions | (int)(door.ParentCheckpointDoor?.KeycardPermissions ?? KeycardPermissions.None);
            return new KeycardLevels((DoorPermissionFlags)permissions).HighestLevelValue;
        }

        private static bool InReach(Attempt attempt)
        {
            Vector3 from = attempt.Player.CameraTransform.position;
            Vector3 delta = attempt.Point - from;
            if (delta.sqrMagnitude > 3.5f * 3.5f)
                return false;
            foreach (var hit in Physics.RaycastAll(from, delta.normalized, Mathf.Max(0, delta.magnitude - .1f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == null || hit.collider.GetComponentInParent<ReferenceHub>() != null || hit.collider.GetComponentInParent<AdminToys.AdminToyBase>() != null)
                    continue;
                var door = hit.collider.GetComponentInParent<DoorVariant>();
                if (door != null && (door == attempt.Door.Base || door == attempt.Door.ParentCheckpointDoor?.Base))
                    continue;
                return false;
            }

            return true;
        }

        private void Finish(Attempt attempt)
        {
            completing = attempt;
            try
            {
                // Use native gate/checkpoint/door behavior and run normal events.
                attempt.Door.Base.ServerInteract(attempt.Player.ReferenceHub, attempt.ColliderId);
            }
            finally
            {
                completing = null;
            }

            if (attempt.Door.Base != null && attempt.Door.IsOpen && timers.Succeed(attempt.User, Time.time, attempt.Duration, attempt.Cooldown))
            {
                attempts.Remove(attempt.Player);
                attempt.Player.ShowHint("Lockpick successful. Ready again in " + Mathf.CeilToInt(attempt.Cooldown) + " seconds.", 4);
                return;
            }

            Cancel(attempt.Player, "the door interaction was blocked; no cooldown used");
        }

        private void Progress(Attempt attempt, bool force)
        {
            if (!force && attempt.Player.HasHint && !ReferenceEquals(attempt.Player.CurrentHint, attempt.Hint))
                return;
            float elapsed = Mathf.Min(attempt.Duration, timers.Elapsed(attempt.User, Time.time));
            attempt.Player.ShowHint("<b>Lockpicking</b>\n" + elapsed.ToString("0.0") + " / " + attempt.Duration.ToString("0.0") + " seconds\nStay nearby. Taking damage interrupts.", .8f);
            attempt.Hint = attempt.Player.CurrentHint;
        }

        internal void Cancel(Player player, string reason = null)
        {
            if (!attempts.TryGetValue(player, out var attempt))
                return;
            attempts.Remove(player);
            timers.Cancel(attempt.User);
            if (!player.IsConnected)
                return;
            if (reason != null && player.IsAlive)
                player.ShowHint("Lockpick cancelled: " + reason, 3);
            else if (ReferenceEquals(player.CurrentHint, attempt.Hint))
                player.ShowHint("", .01f);
        }

        internal void Clear()
        {
            foreach (var player in attempts.Keys.ToArray())
                Cancel(player);
            timers.Clear();
            completing = null;
            nextHint = 0;
        }

        private sealed class Attempt
        {
            internal Player Player;
            internal string User;
            internal PlayerRoleBase Role;
            internal Door Door;
            internal byte ColliderId;
            internal Vector3 Point;
            internal float Duration, Cooldown;
            internal int Level;
            internal Hint Hint;
        }
    }
}
