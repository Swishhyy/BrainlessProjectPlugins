using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Brainless.Internal.Updates
{
    internal static class UpdateDiagnostics
    {
        internal static string Failure(Exception failure)
        {
            var types = new List<string>();
            Exception deepest = failure;
            for (int i = 0; deepest != null && i < 4; i++)
            {
                types.Add(deepest.GetType().Name);
                if (deepest.InnerException == null)
                    break;
                deepest = deepest.InnerException;
            }

            // Only method/type identifiers, never messages, file paths, request
            // URLs, headers or serialized configuration containing credentials.
            var method = new StackTrace(deepest ?? failure, false).GetFrames()?.Select(f => f.GetMethod()).FirstOrDefault(m => m?.DeclaringType != null);
            string location = method == null ? "" : "; at " + method.DeclaringType.FullName + "." + method.Name;
            if (location.Length > 200)
                location = location.Substring(0, 200);
            return "Update check failed (" + string.Join(" > ", types) + location + "). Gameplay continues; retrying later.";
        }
    }
}
