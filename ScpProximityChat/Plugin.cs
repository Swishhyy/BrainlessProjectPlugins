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

namespace Brainless.ScpProximityChat
{
    public sealed class Plugin : Plugin<Config>
    {
        public override string Name => "BrainlessScpProximityChat";
        public override string Prefix => "brainless_scp_proximity_chat";
        public override string Author => "Brainless Project";
        public override Version Version => new Version(0, 2, 0);
        public override Version RequiredExiledVersion => new Version(9, 15, 2);
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
                P.Verified += Verified;
                P.Left += Left;
                P.ChangingRole += ChangingRole;
                P.Died += Died;
                S.WaitingForPlayers += Reset;
                S.RestartingRound += Reset;
                Instance = this;
                tracker = Timing.RunCoroutine(TrackSpeakers());
                ServerSpecificSettingsSync.SendToAll();
                updates = PluginUpdates.Start(typeof(Plugin).Assembly, "ScpProximityChat", Config.AutoUpdate);
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
            P.Verified -= Verified;
            P.Left -= Left;
            P.ChangingRole -= ChangingRole;
            P.Died -= Died;
            S.WaitingForPlayers -= Reset;
            S.RestartingRound -= Reset;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= SettingReceived;
            ServerSpecificSettingsSync.ServerOnStatusReceived -= StatusReceived;
            Reset();
            states.Clear();
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

        internal bool Eligible(Player player) => player != null && player.IsConnected && player.IsAlive && !player.IsHost && player.IsScp && player.Role.Type != RoleTypeId.Scp079 && player.Role.Type != RoleTypeId.Scp3114 && Config.AllowedRoles?.Contains(player.Role.Type) == true;
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
                updates = PluginUpdates.Start(typeof(Plugin).Assembly, "ScpProximityChat", Config.AutoUpdate);
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
        private void Verified(VerifiedEventArgs ev) => Sync(ev.Player);
        private void StatusReceived(ReferenceHub hub, SSSUserStatusReport _)
        {
            if (ServerSpecificSettingsSync.GetUserVersion(hub) != ServerSpecificSettingsSync.Version)
                Sync(Player.Get(hub));
        }

        private void Sync(Player player) => Guard(() =>
        {
            if (settings != null && player != null && player.IsConnected && !player.IsHost)
                ServerSpecificSettingsSync.SendToPlayer(player.ReferenceHub);
        });
        private void Left(LeftEventArgs ev)
        {
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
            return State(receiver).CanHear(sender.UserId, receiver.IsAlive, sender == receiver, nativeAllowed, (role.VoiceModule.ReceiveFlags & GroupMuteFlags.Alive) != 0, (receiver.Position - sender.Position).sqrMagnitude, Config.VoiceRange);
        }

        private void Receiving(ReceivingVoiceMessageEventArgs ev)
        {
            if (!ev.IsAllowed || !IsLocal(ev.Sender))
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
                speaker.Position = sender.CameraTransform.position;
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
                            pair.Value.Position = pair.Key.CameraTransform.position;
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
