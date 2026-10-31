using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class VoiceNativeContracts
{
    internal static void Run(PEReader pe, MetadataReader md, Action<bool, string> check)
    {
        var type = md.TypeDefinitions.Select(md.GetTypeDefinition).Single(t => md.GetString(t.Namespace) == "VoiceChat.Networking" && md.GetString(t.Name) == "VoiceTransceiver");
        var method = type.GetMethods().Select(md.GetMethodDefinition).Single(m => md.GetString(m.Name) == "ServerReceiveMessage");
        var calls = Calls(pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes(), md).ToArray();
        int Position(string name) => Array.IndexOf(calls, name);
        check(Position("CheckRateLimit") >= 0 && Position("CheckRateLimit") < Position("IsMuted") && Position("IsMuted") < Position("ValidateSend") && Position("ValidateSend") < Position("OnSendingVoiceMessage"), "the native voice path validates sender rate/mute/channel before sending events");
        check(Position("OnSendingVoiceMessage") < Position("ValidateReceive") && Position("ValidateReceive") < Position("OnReceivingVoiceMessage") && Position("OnReceivingVoiceMessage") < Position("Send") && calls.Count(c => c == "Send") == 1, "the spatial routing patch targets the sole native send after sending and recipient events");
        check(Position("Invoke") > Position("OnReceivingVoiceMessage") && Position("Invoke") < Position("Send"), "native voice observer callbacks stay before the replaced final send");
        var playback = md.TypeDefinitions.Select(md.GetTypeDefinition).Single(t => md.GetString(t.Namespace) == "VoiceChat.Playbacks" && md.GetString(t.Name) == "SpeakerToyPlaybackBase");
        var decode = playback.GetMethods().Select(md.GetMethodDefinition).Single(m => md.GetString(m.Name) == "DecodeSamples");
        check(Calls(pe.GetMethodBody(decode.RelativeVirtualAddress).GetILBytes(), md).Contains("Decode"), "native speaker playback decodes the original Opus packet without an external audio encoder");
        var toy = md.TypeDefinitions.Select(md.GetTypeDefinition).Single(t => md.GetString(t.Namespace) == "AdminToys" && md.GetString(t.Name) == "SpeakerToy");
        var volumeHook = toy.GetMethods().Select(md.GetMethodDefinition).Single(m => md.GetString(m.Name) == "OnVolumeChanged");
        var volumeMembers = Calls(pe.GetMethodBody(volumeHook.RelativeVirtualAddress).GetILBytes(), md, true).ToArray();
        check(volumeMembers.Contains("VolumeScale") && !volumeMembers.Contains("set_volume") && !volumeMembers.Contains("Clamp01"), "native speaker gain writes the unclamped PCM VolumeScale rather than Unity's 0-to-1 AudioSource volume");
        var playbackBase = md.TypeDefinitions.Select(md.GetTypeDefinition).Single(t => md.GetString(t.Namespace) == "VoiceChat.Playbacks" && md.GetString(t.Name) == "VoiceChatPlaybackBase");
        var filter = playbackBase.GetMethods().Select(md.GetMethodDefinition).Single(m => md.GetString(m.Name) == "OnAudioFilterRead");
        check(Calls(pe.GetMethodBody(filter.RelativeVirtualAddress).GetILBytes(), md, true).Contains("VolumeScale"), "native audio playback applies the synchronized speaker gain to decoded speech samples");
    }

    internal static IEnumerable<string> Calls(byte[] bytes, MetadataReader md, bool includeFields = false)
    {
        var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)).ToDictionary(o => unchecked((ushort)o.Value));
        int offset = 0;
        while (offset < bytes.Length)
        {
            ushort value = bytes[offset++];
            if (value == 0xfe)
                value = (ushort)(0xfe00 | bytes[offset++]);
            var opcode = opcodes[value];
            int size = opcode.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + BitConverter.ToInt32(bytes, offset) * 4,
                _ => 4,
            };
            if (opcode.OperandType == OperandType.InlineMethod || includeFields && opcode.OperandType == OperandType.InlineField)
            {
                var handle = MetadataTokens.EntityHandle(BitConverter.ToInt32(bytes, offset));
                if (handle.Kind == HandleKind.MethodSpecification)
                    handle = md.GetMethodSpecification((MethodSpecificationHandle)handle).Method;
                if (handle.Kind == HandleKind.MethodDefinition)
                    yield return md.GetString(md.GetMethodDefinition((MethodDefinitionHandle)handle).Name);
                else if (handle.Kind == HandleKind.MemberReference)
                    yield return md.GetString(md.GetMemberReference((MemberReferenceHandle)handle).Name);
                else if (handle.Kind == HandleKind.FieldDefinition)
                    yield return md.GetString(md.GetFieldDefinition((FieldDefinitionHandle)handle).Name);
            }

            offset += size;
        }
    }
}
