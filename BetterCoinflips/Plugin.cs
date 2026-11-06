using Exiled.API.Features;
using System;
using BetterCoinflips.Configs;
using Map = Exiled.Events.Handlers.Map;
using Player = Exiled.Events.Handlers.Player;

namespace BetterCoinflips
{
    public class Plugin : Plugin<Config, Configs.Translations>
    {
        public override Version RequiredExiledVersion => new(9, 15, 3);
        public override Version Version => new(5, 3, 1);
        public override string Author => "Miki_hero";
        public override string Name => "BetterCoinflips";

        public static Plugin Instance;
        private EventHandlers _eventHandler;
        private readonly Brainless.Internal.Lifetime _lifetime = new();
        private IDisposable updates;
        public void Later(float delay, Action action) => _lifetime.Later(delay, action);
        internal void Run(System.Collections.Generic.IEnumerator<float> routine) => _lifetime.Run(routine);
        public override void OnEnabled()
        {
            Instance = this;
            Brainless.Internal.ConfigMigration.Tidy();
            if (Config.UpgradeBalance())
                Brainless.Internal.ConfigMigration.Save(this);
            RegisterEvents();
            base.OnEnabled();
            updates = Brainless.Internal.Updates.PluginUpdates.Start(GetType().Assembly, "BetterCoinflips", Config.AutoUpdate);
        }

        public override void OnDisabled()
        {
            updates?.Dispose();
            updates = null;
            UnRegisterEvents();
            _lifetime.Clear();
            Instance = null;
            base.OnDisabled();
        }

        private void RegisterEvents()
        {
            _eventHandler = new EventHandlers();
            Player.FlippingCoin += _eventHandler.OnCoinFlip;
            Map.SpawningItem += _eventHandler.OnSpawningItem;
            Map.FillingLocker += _eventHandler.OnFillingLocker;
            Exiled.Events.Handlers.Scp914.UpgradingPickup += _eventHandler.OnUpgradingPickup;
            Exiled.Events.Handlers.Scp914.UpgradingInventoryItem += _eventHandler.OnUpgradingInventoryItem;
            Player.Left += _eventHandler.OnLeft;
            Player.Hurting += Types.Modifiers.Hurting;
            Player.ChangingRole += Types.Modifiers.ChangingRole;
            Exiled.Events.Handlers.Server.WaitingForPlayers += ResetRound;
            Exiled.Events.Handlers.Server.RestartingRound += ResetRound;
        }

        private void UnRegisterEvents()
        {
            Player.FlippingCoin -= _eventHandler.OnCoinFlip;
            Map.SpawningItem -= _eventHandler.OnSpawningItem;
            Map.FillingLocker -= _eventHandler.OnFillingLocker;
            Exiled.Events.Handlers.Scp914.UpgradingPickup -= _eventHandler.OnUpgradingPickup;
            Exiled.Events.Handlers.Scp914.UpgradingInventoryItem -= _eventHandler.OnUpgradingInventoryItem;
            Player.Left -= _eventHandler.OnLeft;
            Player.Hurting -= Types.Modifiers.Hurting;
            Player.ChangingRole -= Types.Modifiers.ChangingRole;
            Types.Modifiers.Clear();
            Exiled.Events.Handlers.Server.WaitingForPlayers -= ResetRound;
            Exiled.Events.Handlers.Server.RestartingRound -= ResetRound;
            _eventHandler = null;
        }

        private void ResetRound()
        {
            _lifetime.Clear();
            Types.Modifiers.Clear();
            _eventHandler?.Reset();
        }
    }
}
