namespace Brainless.Items
{
    internal sealed class ImpactTrigger
    {
        private readonly float armedAt;
        private readonly bool onPlayers;
        private bool fired;
        internal ImpactTrigger(float armedAt, bool onPlayers)
        {
            this.armedAt = armedAt;
            this.onPlayers = onPlayers;
        }

        internal bool TryContact(float now, bool player, bool owner)
        {
            if (fired || float.IsNaN(now) || float.IsInfinity(now) || now < armedAt || owner || (player && !onPlayers))
                return false;
            fired = true;
            return true;
        }
    }
}
