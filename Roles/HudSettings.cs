using System;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using UserSettings.ServerSpecific;
using P = Exiled.Events.Handlers.Player;

namespace Brainless.Roles
{
    // Server Specific Settings for the role HUD: a keybind that fades it in/out and a
    // show/hide switch. Definitions are appended/removed by identity. The game sends the
    // whole menu to joining players; resending it from several plugins on join/status
    // multiplied traffic and lagged some players out, so only SCP voice resends (limited).
    public sealed partial class Plugin
    {
        private ServerSpecificSettingBase[] hudSettings;

        private void RegisterHudSettings()
        {
            int id = Config.HudSettingsIdBase;
            var existing = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
            if (id < 0 || id > int.MaxValue - 2 || existing.Any(s => s != null && s.SettingId >= id && s.SettingId <= id + 2))
            {
                Log.Warn("[BrainlessRoles] HUD setting IDs " + id + "-" + (id + 2) + " collide with another plugin; the HUD keybind and switch are disabled. Change hud_settings_id_base.");
                return;
            }

            hudSettings = new ServerSpecificSettingBase[]
            {
                new SSGroupHeader(id, "Project Brainless HUD", false, "The role panel and SCP proximity chat status on the side of your screen."),
                new SSKeybindSetting(id + 1, "Fade role HUD in/out", Config.HudToggleKey, true, true, "Press to hide or show the role panel and SCP chat status.", byte.MaxValue),
                new SSTwoButtonsSetting(id + 2, "Role HUD", "Hidden", "Shown", true, "Shows your role and SCP proximity chat status. The keybind flips this until you change it again.", byte.MaxValue, false),
            };
            ServerSpecificSettingsSync.DefinedSettings = existing.Concat(hudSettings).ToArray();
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += HudSettingReceived;
            ServerSpecificSettingsSync.SendToAll();
        }

        private void UnregisterHudSettings()
        {
            if (hudSettings == null)
                return;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= HudSettingReceived;
            ServerSpecificSettingsSync.DefinedSettings = (ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>()).Where(s => !hudSettings.Contains(s)).ToArray();
            ServerSpecificSettingsSync.SendToAll();
            hudSettings = null;
        }

        private void HudSettingReceived(ReferenceHub hub, ServerSpecificSettingBase setting) => Safe.Run(() =>
        {
            if (hudSettings == null || roleHud == null || !hudSettings.Contains(setting.OriginalDefinition))
                return;
            var player = Player.Get(hub);
            if (player == null || !player.IsConnected || player.IsHost)
                return;
            if (setting.SettingId == Config.HudSettingsIdBase + 1 && setting is SSKeybindSetting key)
                roleHud.KeyPressed(player, key.SyncIsPressed);
            else if (setting.SettingId == Config.HudSettingsIdBase + 2 && setting is SSTwoButtonsSetting shown)
                roleHud.SetVisible(player, shown.SyncIsB);
        });
    }
}
