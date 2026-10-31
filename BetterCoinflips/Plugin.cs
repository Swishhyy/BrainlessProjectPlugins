using Exiled.API.Features;
using System;
using BetterCoinflips.Configs;
using Map = Exiled.Events.Handlers.Map;
using Player = Exiled.Events.Handlers.Player;

namespace BetterCoinflips
{
    public class Plugin : Plugin<Config, Configs.Translations>
    {
        public override Version RequiredExiledVersion => new(9, 15, 2);
        public override Version Version => new(5, 1, 4);
        public override string Author => "Miki_hero";
        public override string Name => "BetterCoinflips";

        public static Plugin Instance;
        private EventHandlers _eventHandler;
        private readonly Brainless.Internal.Lifetime _lifetime = new();
        private Brainless.Internal.Updates.PluginUpdates updates;
        public void Later(float delay, Action action) => _lifetime.Later(delay, action);
        public override void OnEnabled()
        {
            Instance = this;
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
            Player.Left += _eventHandler.OnLeft;
            Exiled.Events.Handlers.Server.WaitingForPlayers += ResetRound;
            Exiled.Events.Handlers.Server.RestartingRound += ResetRound;
        }

        private void UnRegisterEvents()
        {
            Player.FlippingCoin -= _eventHandler.OnCoinFlip;
            Map.SpawningItem -= _eventHandler.OnSpawningItem;
            Map.FillingLocker -= _eventHandler.OnFillingLocker;
            Player.Left -= _eventHandler.OnLeft;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= ResetRound;
            Exiled.Events.Handlers.Server.RestartingRound -= ResetRound;
            _eventHandler = null;
        }

        private void ResetRound()
        {
            _lifetime.Clear();
            _eventHandler?.Reset();
        }
    }
}
