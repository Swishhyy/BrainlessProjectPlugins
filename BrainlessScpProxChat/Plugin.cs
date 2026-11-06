using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal.Updates;
using Exiled.API.Features;
using Exiled.API.Features.Toys;
using Exiled.Events.EventArgs.Player;
using HarmonyLib;
using MEC;
using Mirror;
using PlayerRoles;
using PlayerRoles.Voice;
using UnityEngine;
using UserSettings.ServerSpecific;
using VoiceChat;
using VoiceChat.Networking;
using P = Exiled.Events.Handlers.Player;
using S = Exiled.Events.Handlers.Server;

namespace Brainless.ScpProxChat
{
    public sealed class Plugin : Plugin<Config>
    {
        public override string Name => "BrainlessScpProxChat";
        public override string Prefix => "brainless_scp_proximity_chat";
        public override string Author => "Swishhyy";
        public override Version Version => new Version(0, 3, 2);
        public override Version RequiredExiledVersion => new Version(9, 15, 3);
        public static Plugin Instance { get; private set; }

        private readonly Dictionary<Player, VoiceState> states = new Dictionary<Player, VoiceState>();
        private readonly Dictionary<Player, Speaker> speakers = new Dictionary<Player, Speaker>();
        private ServerSpecificSettingBase[] settings;
        private Harmony harmony;
        private CoroutineHandle tracker;
        private IDisposable updates;
        private float lastError;
        public override void OnEnabled()
        {
            if (Config.SettingsIdBase < 0 || Config.SettingsIdBase > int.MaxValue - 3)
                throw new InvalidOperationException("settings_id_base must reserve four positive integer IDs.");
            var existing = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
            if (existing.Any(s => s != null && s.SettingId >= Config.SettingsIdBase && s.SettingId <= Config.SettingsIdBase + 3))
                throw new InvalidOperationException("Nearby chat setting IDs collide with another plugin. Change settings_id_base.");
            bool migrateConfig = Config.AudioBalanceRevision < VoiceAudio.Revision;
            Config.UpgradeAudio();
            Config.VoiceRange = Bounded(Config.VoiceRange, 5, 30, 16);
            Config.FullVolumeRange = Bounded(Config.FullVolumeRange, .1f, Config.VoiceRange, VoiceAudio.DefaultFullVolumeRange);
            Config.Volume = Bounded(Config.Volume, 0, VoiceAudio.MaximumVolume, VoiceAudio.DefaultVolume);
            Config.HintDurationSeconds = Bounded(Config.HintDurationSeconds, .5f, 10, 2);
            harmony = new Harmony("brainless.scp-proximity-chat");
            try
            {
                harmony.PatchAll(typeof(Plugin).Assembly);
                settings = new ServerSpecificSettingBase[]
                {
                    new SSGroupHeader(Config.SettingsIdBase, "Project Brainless SCP voice", false, "Tap the nearby key once to enable proximity, again for team voice. Use your normal voice key to speak."),
                    new SSKeybindSetting(Config.SettingsIdBase + 1, "Toggle SCP proximity chat", Config.SuggestedKey, true, false, "Press once for nearby chat, again for team chat. Releasing this key never turns nearby chat off.", byte.MaxValue),
                    // base+2 is retired; preserve the existing receive-control ID.
                    new SSTwoButtonsSetting(Config.SettingsIdBase + 3, "Hear nearby SCP voice", "Off", "On", true, "For individual mutes use .scpchat mute <playerID> in the F1 console.", byte.MaxValue, false),
                };
                ServerSpecificSettingsSync.DefinedSettings = existing.Concat(settings).ToArray();
                ServerSpecificSettingsSync.ServerOnSettingValueReceived += SettingReceived;
                ServerSpecificSettingsSync.ServerOnStatusReceived += StatusReceived;
                P.ReceivingVoiceMessage.Subscribe(Receiving, -1000);
                P.Left += Left;
                P.ChangingRole += ChangingRole;
                P.Died += Died;
                S.WaitingForPlayers += Reset;
                S.RestartingRound += Reset;
                Instance = this;
                tracker = Timing.RunCoroutine(TrackSpeakers());
                ServerSpecificSettingsSync.SendToAll();
                updates = PluginUpdates.Start(typeof(Plugin).Assembly, "BrainlessScpProxChat", Config.AutoUpdate);
                Brainless.Internal.ConfigMigration.Tidy();
                if (migrateConfig)
                    Brainless.Internal.ConfigMigration.Save(this);
                base.OnEnabled();
            }
            catch
            {
                OnDisabled();
                throw;
            }
        }

        public override void OnDisabled()
        {
            Instance = null;
            updates?.Dispose();
            updates = null;
            Timing.KillCoroutines(tracker);
            P.ReceivingVoiceMessage -= Receiving;
            P.Left -= Left;
            P.ChangingRole -= ChangingRole;
            P.Died -= Died;
            S.WaitingForPlayers -= Reset;
            S.RestartingRound -= Reset;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= SettingReceived;
            ServerSpecificSettingsSync.ServerOnStatusReceived -= StatusReceived;
            Reset();
            states.Clear();
            resends.Clear();
            if (settings != null)
            {
                // Remove our exact definitions; never restore a stale snapshot over other plugins.
                ServerSpecificSettingsSync.DefinedSettings = (ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>()).Where(s => !settings.Contains(s)).ToArray();
                ServerSpecificSettingsSync.SendToAll();
                settings = null;
            }

            harmony?.UnpatchAll(harmony.Id);
            harmony = null;
            base.OnDisabled();
        }

        internal VoiceState State(Player player)
        {
            if (!states.TryGetValue(player, out var state))
            {
                state = new VoiceState();
                states.Add(player, state);
            }

            return state;
        }

        // SCP-079 and SCP-3114 keep their native voice mechanics. SCP-939 can use nearby
        // chat: only its normal SCP channel is rerouted, never its mimicry channel, and
        // other 939s keep hearing team voice natively (their client records voices).
        internal static bool Excluded(RoleTypeId role) => role == RoleTypeId.Scp079 || role == RoleTypeId.Scp3114;

        // Text for the role HUD's status line, or null when the player has no SCP voice choice.
        internal string HudStatus(Player player)
        {
            if (!Config.ShowHudStatus || !Eligible(player))
                return null;
            if (!IsLocal(player))
                return "team";
            return MimicPoint(player).HasValue ? "mimic" : "nearby";
        }

        // Where a nearby SCP's voice plays from: its head, or SCP-939's active mimic point.
        private Vector3 Origin(Player player) => MimicPoint(player) ?? player.CameraTransform.position;

        private Vector3? MimicPoint(Player player)
        {
            if (!Config.Scp939VoiceFromMimicPoint || !(player.Role.Base is PlayerRoles.PlayableScps.Scp939.Scp939Role scp939))
                return null;
            if (!scp939.SubroutineModule.TryGetSubroutine(out PlayerRoles.PlayableScps.Scp939.Mimicry.MimicPointController point) || !point.Active || point.MimicPointTransform == null)
                return null;
            return point.MimicPointTransform.position;
        }
        internal bool Eligible(Player player) => player != null && player.IsConnected && player.IsAlive && !player.IsHost && player.IsScp && !Excluded(player.Role.Type) && Config.AllowedRoles?.Contains(player.Role.Type) == true;
        private bool IsLocal(Player player) => Eligible(player) && states.TryGetValue(player, out var state) && state.Local;
        internal string AudioStatus(Player player)
        {
            string active = player == null ? "active speakers: " + speakers.Count : "active speaker gain: " + (speakers.TryGetValue(player, out var speaker) && speaker.Base != null ? speaker.Volume.ToString("0.##") + "x" : "inactive");
            return $"Plugin {Version}; configured gain: {Config.Volume:0.##}x; {active}; full-volume distance: {Config.FullVolumeRange:0.#}m; range: {Config.VoiceRange:0.#}m.";
        }

        internal bool SetVolume(float volume, out string response)
        {
            if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0 || volume > VoiceAudio.MaximumVolume)
            {
                response = "Use a volume from 0 to 20. Default: 10.";
                return false;
            }

            Config.Volume = volume;
            RefreshAudio();
            response = AudioStatus(null) + " Live change only; put volume in the config to keep it after restart.";
            return true;
        }

        internal bool ReloadAudio(out string response)
        {
            try
            {
                var next = Brainless.Internal.ConfigReload.Read(this);
                if (!next.IsEnabled || next.SettingsIdBase != Config.SettingsIdBase || next.SuggestedKey != Config.SuggestedKey)
                    throw new InvalidOperationException("Enabled state and keybind registration changes require a restart.");
                next.UpgradeAudio();
                if (float.IsNaN(next.Volume) || float.IsInfinity(next.Volume) || next.Volume < 0 || next.Volume > VoiceAudio.MaximumVolume || float.IsNaN(next.VoiceRange) || float.IsInfinity(next.VoiceRange) || next.VoiceRange < 5 || next.VoiceRange > 30 || float.IsNaN(next.FullVolumeRange) || float.IsInfinity(next.FullVolumeRange) || next.FullVolumeRange < .1f || next.FullVolumeRange > next.VoiceRange)
                    throw new InvalidOperationException("Volume must be 0-20, range 5-30m, and full_volume_range between 0.1 and voice_range.");
                Brainless.Internal.ConfigReload.Apply(Config, next);
                RefreshAudio();
                updates?.Dispose();
                updates = PluginUpdates.Start(typeof(Plugin).Assembly, "BrainlessScpProxChat", Config.AutoUpdate);
                response = "SCP voice reloaded. " + AudioStatus(null);
                return true;
            }
            catch (Exception exception)
            {
                response = "Voice reload rejected: " + exception.Message;
                return false;
            }
        }

        private void RefreshAudio()
        {
            foreach (var pair in speakers.ToArray())
            {
                if (!IsLocal(pair.Key) || pair.Value.Base == null)
                {
                    ForgetSpeaker(pair.Key);
                    continue;
                }

                pair.Value.Volume = Config.Volume;
                pair.Value.MinDistance = Config.FullVolumeRange;
                pair.Value.MaxDistance = Config.VoiceRange;
            }
        }

        internal bool Select(Player player, bool local, out string response)
        {
            if (!Eligible(player))
            {
                response = "Nearby voice is available only to the enabled living SCP roles.";
                return false;
            }

            bool ready = true;
            if (local)
            {
                try
                {
                    ready = EnsureSpeaker(player);
                }
                catch (Exception exception)
                {
                    ready = false;
                    Log.Error("[Brainless SCP voice] Speaker allocation failed: " + exception);
                }
            }

            if (!ready)
            {
                State(player).Select(false);
                DestroySpeaker(player);
                response = "No native speaker channel is available. Team chat is still selected.";
                Notify(player, response);
                return false;
            }

            State(player).Select(local);
            if (!local)
                DestroySpeaker(player);
            response = "SCP voice: " + (local ? "NEARBY" : "TEAM") + ". Hold your normal voice key to speak.";
            Notify(player, local ? Config.EnabledHint ?? "Proximity chat enabled" : Config.DisabledHint ?? "Proximity chat disabled");
            return true;
        }

        private void Notify(Player player, string text)
        {
            // Explicit input feedback must be visible even when a role HUD owns
            // the current hint. The HUD can resume after this short message.
            if (Config.ShowHints && player.IsConnected)
                player.ShowHint(text, Config.HintDurationSeconds);
        }

        private void SettingReceived(ReferenceHub hub, ServerSpecificSettingBase setting) => Guard(() =>
        {
            if (settings == null || !settings.Contains(setting.OriginalDefinition))
                return;
            Player player = Player.Get(hub);
            if (player == null || !player.IsConnected || player.IsHost)
                return;
            var state = State(player);
            if (setting.SettingId == Config.SettingsIdBase + 3 && setting is SSTwoButtonsSetting hear)
                state.HearNearby = hear.SyncIsB;
            else if (setting.SettingId == Config.SettingsIdBase + 1 && setting is SSKeybindSetting key)
            {
                if (!Eligible(player))
                {
                    state.ResetInput();
                    return;
                }

                if (state.KeyPressed(key.SyncIsPressed))
                    Select(player, !state.Local, out _);
            }
        });
        // The game already sends the whole settings menu (every plugin's entries) to joining
        // players. Resend only when a client reports a stale menu version, and at most once
        // per 5 s / 3 times per connection: several plugins answering every status report
        // with a full resend made the traffic grow each round trip and lagged players out.
        private readonly Dictionary<ReferenceHub, (float last, int count)> resends = new Dictionary<ReferenceHub, (float last, int count)>();

        private void StatusReceived(ReferenceHub hub, SSSUserStatusReport _)
        {
            if (hub == null || ServerSpecificSettingsSync.GetUserVersion(hub) == ServerSpecificSettingsSync.Version)
                return;
            resends.TryGetValue(hub, out var sent);
            if (sent.count >= 3 || Time.realtimeSinceStartup - sent.last < 5)
                return;
            resends[hub] = (Time.realtimeSinceStartup, sent.count + 1);
            Sync(Player.Get(hub));
        }

        private void Sync(Player player) => Guard(() =>
        {
            if (settings != null && player != null && player.IsConnected && !player.IsHost)
                ServerSpecificSettingsSync.SendToPlayer(player.ReferenceHub);
        });
        private void Left(LeftEventArgs ev)
        {
            if (ev.Player?.ReferenceHub != null)
                resends.Remove(ev.Player.ReferenceHub);
            ForgetSpeaker(ev.Player);
            states.Remove(ev.Player);
        }

        private void ChangingRole(ChangingRoleEventArgs ev)
        {
            if (ev.IsAllowed)
                ForgetSpeaker(ev.Player);
        }

        private void Died(DiedEventArgs ev) => ForgetSpeaker(ev.Player);
        private void ForgetSpeaker(Player player)
        {
            if (states.TryGetValue(player, out var state))
                state.ResetInput();
            DestroySpeaker(player);
        }

        private bool CanReceive(Player sender, Player receiver)
        {
            if (receiver == null || !receiver.IsConnected || receiver.IsHost || !(receiver.Role.Base is IVoiceRole role))
                return false;
            bool nativeAllowed = role.VoiceModule.ValidateReceive(sender.ReferenceHub, VoiceChatChannel.Proximity) == VoiceChatChannel.Proximity;
            // The native proximity check measures from SCP-939's body; a mimic-point voice
            // is judged by the mimic point's distance instead (still alive receivers only).
            var mimic = MimicPoint(sender);
            if (mimic.HasValue && receiver.IsAlive)
                nativeAllowed = true;
            return State(receiver).CanHear(sender.UserId, receiver.IsAlive, sender == receiver, nativeAllowed, (role.VoiceModule.ReceiveFlags & GroupMuteFlags.Alive) != 0, (receiver.Position - (mimic ?? sender.Position)).sqrMagnitude, Config.VoiceRange);
        }

        private void Receiving(ReceivingVoiceMessageEventArgs ev)
        {
            if (!ev.IsAllowed || !IsLocal(ev.Sender) || ev.Player?.Role.Type == RoleTypeId.Scp939)
                return;
            State(ev.Sender).RouteChecks++;
            // Only the normal SCP team channel is rerouted, never SCP-939 mimicry,
            // intercom/global admin voice, or SCP-079's speaker ability.
            if (ev.VoiceModule.CurrentChannel != VoiceChatChannel.ScpChat)
                return;
            var message = ev.VoiceMessage;
            message.Channel = !VoiceChatMutes.IsMuted(ev.Sender.ReferenceHub, false) && speakers.ContainsKey(ev.Sender) && CanReceive(ev.Sender, ev.Player) ? VoiceChatChannel.Proximity : VoiceChatChannel.None;
            ev.VoiceMessage = message;
        }

        internal bool HandleLocalSend(NetworkConnection connection, VoiceMessage message, int channelId)
        {
            Player sender = Player.Get(message.Speaker);
            if (!IsLocal(sender) || !(sender.Role.Base is IVoiceRole voice) || voice.VoiceModule.CurrentChannel != VoiceChatChannel.ScpChat)
                return false;
            // SCP-939 listeners stay on the untouched native path (normal team voice), so
            // their client never receives an SCP's voice it could record for mimicry.
            if (Player.Get(connection.identity)?.Role.Type == RoleTypeId.Scp939)
                return false;
            if (message.Channel != VoiceChatChannel.Proximity)
                return true;
            // A normal VoiceMessage would play globally from an SCP's client-side
            // GlobalVoiceModule. Replace it with the native spatial speaker packet.
            // No decoding, encoder, audio library or second copy of the voice stream.
            Guard(() =>
            {
                Player receiver = Player.Get(connection.identity);
                if (VoiceChatMutes.IsMuted(sender.ReferenceHub, false) || !CanReceive(sender, receiver) || !speakers.TryGetValue(sender, out var speaker) || speaker.Base == null || message.Data == null || message.DataLength <= 0 || message.DataLength > message.Data.Length)
                    return;
                speaker.Position = Origin(sender);
                connection.Send(new AudioMessage(speaker.ControllerId, message.Data, message.DataLength), channelId);
                State(sender).AudioSends++;
            });
            // Fail closed for a handled local packet, never fall back to global SCP audio.
            return true;
        }

        private bool EnsureSpeaker(Player player)
        {
            if (speakers.TryGetValue(player, out var current) && current.Base != null)
                return true;
            var occupied = new HashSet<byte>(AdminToy.List.OfType<Speaker>().Where(s => s.Base != null).Select(s => s.ControllerId));
            for (int id = 1; id < 180; id++)
            {
                if (occupied.Contains((byte)id))
                    continue;
                var speaker = Speaker.Create(player.CameraTransform.position, Vector3.zero, Vector3.one, false);
                try
                {
                    speaker.ControllerId = (byte)id;
                    speaker.IsSpatial = true;
                    speaker.Volume = Config.Volume;
                    speaker.MinDistance = Config.FullVolumeRange;
                    speaker.MaxDistance = Config.VoiceRange;
                    speaker.Spawn();
                    speakers[player] = speaker;
                    return true;
                }
                catch
                {
                    speaker.Destroy();
                    throw;
                }
            }

            return false;
        }

        private void DestroySpeaker(Player player)
        {
            if (!speakers.TryGetValue(player, out var speaker))
                return;
            speakers.Remove(player);
            Guard(() =>
            {
                if (speaker.Base != null)
                    speaker.Destroy();
            });
        }

        private IEnumerator<float> TrackSpeakers()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(.1f);
                foreach (var pair in speakers.ToArray())
                    Guard(() =>
                    {
                        if (!IsLocal(pair.Key) || pair.Value.Base == null)
                            ForgetSpeaker(pair.Key);
                        else
                            pair.Value.Position = Origin(pair.Key);
                    });
            }
        }

        private void Reset()
        {
            foreach (var player in speakers.Keys.ToArray())
                DestroySpeaker(player);
            foreach (var state in states.Values)
                state.ResetInput();
        }

        private void Guard(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                if (Time.realtimeSinceStartup - lastError >= 2)
                {
                    lastError = Time.realtimeSinceStartup;
                    Log.Error("[Brainless SCP voice] " + exception);
                }
            }
        }

        private static float Bounded(float value, float min, float max, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));
    }
}
