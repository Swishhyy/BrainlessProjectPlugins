using System;
using System.Linq;
using Brainless.Roles;

internal static class CandyTests
{
    internal static void Run(Action<bool, string> check)
    {
        var standard = new[]
        {
            1,
            2,
            3,
            4,
            5,
            6
        };
        var available = standard.Concat(new[] { 7, 8, 9 }).ToArray();
        check(CandyPool.Resolve(Array.Empty<int>(), available, standard).SequenceEqual(standard), "an empty candy allowlist selects real standard candies rather than medical supplies");
        check(!CandyPool.Resolve(Array.Empty<int>(), available, standard).Contains(7), "explosive or seasonal candy is absent from the default palette");
        check(CandyPool.Resolve(new[] { 7 }, available, standard).SequenceEqual(new[] { 7 }), "an explicitly configured registered special candy remains selectable");
        check(CandyPool.Resolve(new[] { 3, 3, 5 }, available, standard).SequenceEqual(new[] { 3, 5 }), "a candy allowlist is deduplicated without adding unrequested colors");
        check(CandyPool.Resolve(Array.Empty<int>(), new[] { 2, 6 }, standard).SequenceEqual(new[] { 2, 6 }), "default candy choices are restricted to the native registry for the installed build");
        check(CandyPool.Resolve(new[] { 99 }, available, standard).Length == 0, "an unavailable explicit candy cannot silently become another item or color");
        check(CandyPool.Resolve(Array.Empty<int>(), Array.Empty<int>(), standard).Length == 0, "an empty native candy registry is handled without inventing a pill fallback");
    }
}
