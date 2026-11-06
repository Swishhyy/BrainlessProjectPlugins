using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using P = Exiled.Events.Handlers.Player;
using S = Exiled.Events.Handlers.Server;

namespace Brainless.ScpSwap
{
    public sealed class Plugin : Plugin<Config>
    {
        public override string Name => "BrainlessScpSwap";
        public override string Prefix => "brainless_scp_swap";
        public override string Author => "Brainless Project";
        public override Version Version => new Version(0, 1, 1);
        public override Version RequiredExiledVersion => new Version(9, 15, 3);
        public static Plugin Instance { get; private set; }

        private readonly Lifetime lifetime = new Lifetime();
        // Pending requests keyed by the SCP being asked.
        private readonly Dictionary<Player, Request> requests = new Dictionary<Player, Request>();
        private readonly Dictionary<string, int> swapsUsed = new Dictionary<string, int>();
        // SCPs whose current life came from the round-start/late-join spawn or a swap.
        // Event or staff force-classes are not swappable, so round events stay intact.
        private readonly HashSet<Player> eligible = new HashSet<Player>();
        private bool swapping;
        private IDisposable updates;

        public override void OnEnabled()
        {
            Instance = this;
            P.ChangingRole += ChangingRole;
            P.Left += Left;
            S.RoundStarted += RoundStarted;
            S.WaitingForPlayers += Reset;
            base.OnEnabled();
            updates = Brainless.Internal.Updates.PluginUpdates.Start(GetType().Assembly, "ScpSwap", Config.AutoUpdate);
        }

        public override void OnDisabled()
        {
            updates?.Dispose();
            updates = null;
            P.ChangingRole -= ChangingRole;
            P.Left -= Left;
            S.RoundStarted -= RoundStarted;
            S.WaitingForPlayers -= Reset;
            Reset();
            Instance = null;
            base.OnDisabled();
        }

        private void Reset()
        {
            lifetime.Clear();
            requests.Clear();
            swapsUsed.Clear();
            eligible.Clear();
        }

        private void Left(LeftEventArgs ev)
        {
            eligible.Remove(ev.Player);
            requests.Remove(ev.Player);
            foreach (var pair in requests.Where(r => r.Value.From == ev.Player).ToArray())
                requests.Remove(pair.Key);
        }

        private void ChangingRole(ChangingRoleEventArgs ev) => Safe.Run(() =>
        {
            if (!ev.IsAllowed || swapping)
                return;
            bool spawn = ev.Reason == SpawnReason.RoundStart || ev.Reason == SpawnReason.LateJoin;
            if (spawn && ev.NewRole.GetTeam() == Team.SCPs)
                eligible.Add(ev.Player);
            // A same-role reset (Roles' .scpreroll) keeps the player's swap eligibility.
            else if (!(ev.Reason == SpawnReason.ForceClass && ev.NewRole == ev.Player.Role.Type))
                eligible.Remove(ev.Player);
        });

        private void RoundStarted()
        {
            if (string.IsNullOrEmpty(Config.RoundStartBroadcast) || Config.SwapWindowSeconds <= 0)
                return;
            lifetime.Later(1.5f, () =>
            {
                string message = Config.RoundStartBroadcast.Replace("{seconds}", ((int)Config.SwapWindowSeconds).ToString());
                foreach (var player in Player.List.Where(p => eligible.Contains(p) && Swappable(p.Role.Type)))
                    player.Broadcast(Config.BroadcastSeconds, message, Broadcast.BroadcastFlags.Normal, true);
            });
        }

        internal bool Swappable(RoleTypeId role) => role != RoleTypeId.Scp0492 && Config.SwappableScps.Contains(role);
        internal double WindowLeft => Config.SwapWindowSeconds - Round.ElapsedTime.TotalSeconds;

        internal string List(Player viewer)
        {
            var lines = new List<string>();
            foreach (var role in Config.SwappableScps.Where(r => r != RoleTypeId.Scp0492).Distinct())
            {
                var holders = Player.List.Where(p => p.IsAlive && p.Role.Type == role).ToArray();
                if (holders.Length == 0)
                {
                    if (Config.AllowUnplayedScps)
                        lines.Add(SwapRules.Name(role) + ": free");
                    continue;
                }

                foreach (var holder in holders)
                {
                    string variant = RolesBridge.ProfileName(holder);
                    lines.Add(SwapRules.Name(role) + (variant != null ? " (" + variant + ")" : "") + ": " + holder.Nickname + (holder == viewer ? " [you]" : ""));
                }
            }

            string window = Round.IsStarted && WindowLeft > 0 ? "Swap window: " + (int)WindowLeft + "s left." : "The swap window is closed.";
            string pending = requests.TryGetValue(viewer, out var request) && !request.Expired ? "\n" + request.From.Nickname + " wants to swap their " + SwapRules.Name(request.FromRole) + " for your " + SwapRules.Name(request.TargetRole) + ": .scpswap yes / .scpswap no" : "";
            return string.Join("\n", lines) + "\n" + window + pending + "\nUse .scpswap <scp> (e.g. .scpswap 173).";
        }

        // A player's own request: swap with whoever holds that SCP, or take it if unplayed.
        internal bool RequestSwap(Player player, RoleTypeId wanted, out string response)
        {
            response = CanSwap(player, false);
            if (response != null)
                return false;
            if (!Swappable(wanted))
            {
                response = SwapRules.Name(wanted) + " is not swappable on this server.";
                return false;
            }

            if (player.Role.Type == wanted)
            {
                response = "You are already " + SwapRules.Name(wanted) + ".";
                return false;
            }

            var holder = Player.List.FirstOrDefault(p => p != player && p.IsAlive && p.Role.Type == wanted);
            if (holder == null)
            {
                if (!Config.AllowUnplayedScps)
                {
                    response = "Nobody is playing " + SwapRules.Name(wanted) + ", and switching to unplayed SCPs is disabled.";
                    return false;
                }

                TakeUnplayed(player, wanted);
                Spend(player);
                response = "You are now " + SwapRules.Name(wanted) + ".";
                return true;
            }

            string blocked = CanSwap(holder, true);
            if (blocked != null)
            {
                response = holder.Nickname + " cannot swap: " + blocked;
                return false;
            }

            foreach (var old in requests.Where(r => r.Value.From == player).ToArray())
                requests.Remove(old.Key);
            requests[holder] = new Request(player, player.Role.Type, wanted, Config.RequestTimeoutSeconds);
            holder.Broadcast(8, "<size=30>" + player.Nickname + " wants to swap their <b>" + Describe(player) + "</b> for your " + SwapRules.Name(wanted) + ".\nConsole (~): <b>.scpswap yes</b> or <b>.scpswap no</b></size>", Broadcast.BroadcastFlags.Normal, true);
            response = "Asked " + holder.Nickname + " to swap " + SwapRules.Name(wanted) + " with you. They have " + (int)Config.RequestTimeoutSeconds + "s to answer.";
            return true;
        }

        internal bool Answer(Player player, bool accept, out string response)
        {
            if (!requests.TryGetValue(player, out var request) || request.Expired)
            {
                requests.Remove(player);
                response = "You have no pending swap request.";
                return false;
            }

            requests.Remove(player);
            var from = request.From;
            if (!accept)
            {
                if (from.IsConnected)
                    from.Broadcast(5, player.Nickname + " declined your SCP swap.", Broadcast.BroadcastFlags.Normal, true);
                response = "Swap declined.";
                return true;
            }

            if (!from.IsConnected || !from.IsAlive || from.Role.Type != request.FromRole || player.Role.Type != request.TargetRole)
            {
                response = "The swap is no longer possible: one of you changed role.";
                return false;
            }

            string blocked = CanSwap(player, true) ?? CanSwap(from, true);
            if (blocked != null)
            {
                response = "The swap is no longer possible: " + blocked;
                return false;
            }

            Swap(from, player);
            Spend(from);
            Spend(player);
            from.Broadcast(5, player.Nickname + " accepted: you are now " + SwapRules.Name(request.TargetRole) + ".", Broadcast.BroadcastFlags.Normal, true);
            response = "Swapped: you are now " + SwapRules.Name(request.FromRole) + ".";
            return true;
        }

        internal bool Cancel(Player player, out string response)
        {
            var mine = requests.Where(r => r.Value.From == player).Select(r => r.Key).ToArray();
            foreach (var target in mine)
                requests.Remove(target);
            response = mine.Length > 0 ? "Swap request cancelled." : "You have no outgoing swap request.";
            return mine.Length > 0;
        }

        // Staff swap: ignores the window, eligibility and per-player limits.
        internal bool ForceSwap(Player player, Player other, RoleTypeId? role, out string response)
        {
            if (player == null || !player.IsAlive || player.Role.Team != Team.SCPs || player.Role.Type == RoleTypeId.Scp0492)
            {
                response = "The first player must be a living SCP (not SCP-049-2).";
                return false;
            }

            if (other != null)
            {
                if (other == player || !other.IsAlive || other.Role.Team != Team.SCPs || other.Role.Type == RoleTypeId.Scp0492)
                {
                    response = "The second player must be a different living SCP (not SCP-049-2).";
                    return false;
                }

                RoleTypeId first = player.Role.Type, second = other.Role.Type;
                Swap(player, other);
                response = player.Nickname + " is now " + SwapRules.Name(second) + "; " + other.Nickname + " is now " + SwapRules.Name(first) + ".";
                return true;
            }

            TakeUnplayed(player, role.Value);
            response = player.Nickname + " is now " + SwapRules.Name(role.Value) + ".";
            return true;
        }

        private string CanSwap(Player player, bool asTarget)
        {
            if (!Round.IsStarted)
                return "the round has not started.";
            if (!SwapRules.InWindow(Round.ElapsedTime.TotalSeconds, Config.SwapWindowSeconds))
                return "the swap window (" + (int)Config.SwapWindowSeconds + "s after round start) is closed.";
            if (!player.IsAlive || !Swappable(player.Role.Type))
                return (asTarget ? "they are" : "you are") + " not playing a swappable SCP.";
            if (!eligible.Contains(player))
                return (asTarget ? "their" : "your") + " SCP was assigned by staff or an event.";
            if (!SwapRules.HasSwapsLeft(swapsUsed.TryGetValue(player.UserId ?? "", out int used) ? used : 0, Config.MaxSwapsPerPlayer))
                return (asTarget ? "they have" : "you have") + " no swaps left this round.";
            return null;
        }

        private void Spend(Player player)
        {
            string key = player.UserId ?? "";
            swapsUsed[key] = (swapsUsed.TryGetValue(key, out int used) ? used : 0) + 1;
        }

        private string Describe(Player player)
        {
            string variant = RolesBridge.ProfileName(player);
            return SwapRules.Name(player.Role.Type) + (variant != null ? " (" + variant + ")" : "");
        }

        private void Swap(Player a, Player b)
        {
            RoleTypeId roleA = a.Role.Type, roleB = b.Role.Type;
            string profileA = RolesBridge.ProfileId(a), profileB = RolesBridge.ProfileId(b);
            SetRole(a, roleB);
            SetRole(b, roleA);
            // Each player takes over the other SCP, along with its variant in FollowScp mode.
            Restore(a, roleB, profileB, false);
            Restore(b, roleA, profileA, false);
            requests.Remove(a);
            requests.Remove(b);
        }

        private void TakeUnplayed(Player player, RoleTypeId role)
        {
            SetRole(player, role);
            requests.Remove(player);
            Restore(player, role, null, true);
        }

        private void SetRole(Player player, RoleTypeId role)
        {
            bool wasEligible = eligible.Contains(player);
            swapping = true;
            try
            {
                player.Role.Set(role, SpawnReason.ForceClass, RoleSpawnFlags.All);
            }
            finally
            {
                swapping = false;
            }

            if (wasEligible)
                eligible.Add(player);
        }

        // Custom variants are applied once the native spawn has settled. FollowScp hands over
        // the variant that was on the SCP (a plain SCP stays plain); an SCP nobody played
        // (fresh) gets a normal variant roll. Reroll always rolls; None never touches roles.
        private void Restore(Player player, RoleTypeId role, string carriedProfile, bool fresh)
        {
            if (Config.CustomRoleMode == CustomRoleMode.None || !RolesBridge.IsReady)
                return;
            lifetime.Later(.5f, () =>
            {
                if (!player.IsConnected || !player.IsAlive || player.Role.Type != role || RolesBridge.ProfileId(player) != null)
                    return;
                if (Config.CustomRoleMode == CustomRoleMode.Reroll || fresh)
                {
                    RolesBridge.RollProfile(player, false);
                    return;
                }

                if (carriedProfile == null)
                    return;
                string failure = RolesBridge.AssignProfile(player, carriedProfile);
                if (failure != null)
                    Log.Debug("SCP swap could not carry variant " + carriedProfile + " to " + player.Nickname + ": " + failure);
            });
        }

        private sealed class Request
        {
            internal readonly Player From;
            internal readonly RoleTypeId FromRole, TargetRole;
            private readonly DateTime expires;

            internal Request(Player from, RoleTypeId fromRole, RoleTypeId targetRole, float timeout)
            {
                From = from;
                FromRole = fromRole;
                TargetRole = targetRole;
                expires = DateTime.UtcNow.AddSeconds(Math.Max(1, timeout));
            }

            internal bool Expired => DateTime.UtcNow > expires || !From.IsConnected;
        }
    }
}
