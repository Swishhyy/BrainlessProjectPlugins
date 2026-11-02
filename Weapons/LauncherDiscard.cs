using System.Linq;
using Exiled.API.Features;

namespace Brainless.Weapons
{
    public sealed partial class Plugin
    {
        private void DiscardEmptyLauncher(int serial)
        {
            if (!Config.LauncherDiscardWhenEmpty || shots.Remaining(serial) > 0 || Find(serial)?.Definition.Id != 7207)
                return;
            foreach (var player in Player.List)
            {
                var item = player.Items.FirstOrDefault(i => i.Serial == serial);
                if (item != null)
                {
                    player.RemoveItem(item);
                    player.ShowHint("Empty grenade launcher discarded.", 3);
                }
            }

            var pickup = Exiled.API.Features.Pickups.Pickup.List.FirstOrDefault(p => p.Serial == serial);
            pickup?.Destroy();
            Find(serial)?.Definition.TrackedSerials.Remove((ushort)serial);
            states.Remove(serial);
            shots.Forget(serial);
        }
    }
}
