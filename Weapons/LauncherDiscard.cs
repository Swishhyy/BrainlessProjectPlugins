using System.Linq;
using Exiled.API.Features;

namespace Brainless.Weapons
{
    public sealed partial class Plugin
    {
        private void DiscardEmptyLauncher(int serial)
        {
            var id = Find(serial)?.Definition.Id;
            if (!Config.LauncherDiscardWhenEmpty || shots.Remaining(serial) > 0 || id == null || !HasFixedShots(id.Value))
                return;
            string name = id == 7207 ? "grenade launcher" : "stun gun";
            foreach (var player in Player.List)
            {
                var item = player.Items.FirstOrDefault(i => i.Serial == serial);
                if (item != null)
                {
                    player.RemoveItem(item);
                    player.ShowHint("Empty " + name + " discarded.", 3);
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
