using PlayerRoles.PlayableScps.Scp3114;
using PlayerStatsSystem;

namespace Brainless.RoundEvents
{
    internal static class NativeAttack
    {
        internal static InfectionAttack Classify(DamageHandlerBase handler)
        {
            if (!(handler is Scp3114DamageHandler skeleton) || skeleton.StartingRagdoll)
                return InfectionAttack.Other;
            return skeleton.Subtype == Scp3114DamageHandler.HandlerType.Slap ? InfectionAttack.Slap : skeleton.Subtype == Scp3114DamageHandler.HandlerType.Strangulation ? InfectionAttack.Strangulation : InfectionAttack.Other;
        }
    }
}
