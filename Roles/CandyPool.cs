using System.Collections.Generic;
using System.Linq;

namespace Brainless.Roles
{
    internal static class CandyPool
    {
        internal static T[] Resolve<T>(IEnumerable<T> configured, IEnumerable<T> available, IEnumerable<T> defaults)
        {
            var registered = new HashSet<T>(available);
            var requested = configured.ToArray();
            return (requested.Length == 0 ? defaults : requested).Where(registered.Contains).Distinct().ToArray();
        }
    }
}
