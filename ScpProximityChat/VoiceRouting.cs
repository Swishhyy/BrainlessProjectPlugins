using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mirror;
using VoiceChat.Networking;

namespace Brainless.ScpProximityChat
{
    [HarmonyPatch(typeof(VoiceTransceiver), "ServerReceiveMessage")]
    internal static class VoiceRouting
    {
        // Replace only the final, already-validated send. Sender identity, rate limit,
        // server mute, EXILED and LabAPI sending/receiving events remain upstream.
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            int count = 0;
            foreach (var instruction in code)
            {
                if ((instruction.opcode != OpCodes.Callvirt && instruction.opcode != OpCodes.Call) || !(instruction.operand is MethodInfo method) || method.Name != nameof(NetworkConnection.Send) || method.DeclaringType != typeof(NetworkConnection) || !method.IsGenericMethod || method.GetGenericArguments().Length != 1 || method.GetGenericArguments()[0] != typeof(VoiceMessage))
                    continue;
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(VoiceRouting), nameof(Send));
                count++;
            }

            if (count != 1)
                throw new InvalidOperationException("The native final voice send changed; nearby chat was not enabled.");
            return code;
        }

        private static void Send(NetworkConnection connection, VoiceMessage message, int channelId)
        {
            if (Plugin.Instance?.HandleLocalSend(connection, message, channelId) == true)
                return;
            connection.Send(message, channelId);
        }
    }
}
