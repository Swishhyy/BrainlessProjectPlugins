using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Brainless.Internal;
using Brainless.Internal.Updates;

internal static class JsonAndTextTests
{
    [DataContract]
    public sealed class Sample
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "count")]
        public long Count { get; set; }

        [DataMember(Name = "flags")]
        public bool[] Flags { get; set; }

        [DataMember(Name = "stats")]
        public Dictionary<string, long> Stats { get; set; }

        [DataMember(Name = "labels")]
        public List<string> Labels { get; set; }
    }

    internal static void Run(Action<bool, string> check)
    {
        var original = new Sample
        {
            Name = "Name \"quote\" \\ slash\nemoji 🦴",
            Count = long.MaxValue,
            Flags = new[]
            {
                true,
                false
            },
            Stats = new Dictionary<string, long>
            {
                ["kills"] = long.MaxValue,
                ["deaths"] = 0
            },
            Labels = new List<string>
            {
                "one",
                "two"
            }
        };
        byte[] json = JsonContract.Write(original);
        using (var native = System.Text.Json.JsonDocument.Parse(json))
            check(native.RootElement.GetProperty("count").GetInt64() == long.MaxValue, "the Unity-safe JSON writer emits valid JSON without losing 64-bit counts");
        var copy = JsonContract.Read<Sample>(json);
        check(copy.Name == original.Name && copy.Count == original.Count && copy.Stats["kills"] == long.MaxValue && copy.Flags.SequenceEqual(original.Flags) && copy.Labels.SequenceEqual(original.Labels), "the updater/stats codec roundtrips Unicode, escapes, nested dictionaries, lists and array booleans");
        using (var stream = new MemoryStream())
        {
            new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Sample), new System.Runtime.Serialization.Json.DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true }).WriteObject(stream, original);
            check(JsonContract.Read<Sample>(stream.ToArray()).Stats["kills"] == long.MaxValue, "the new codec reads the former DataContract JSON format used by persisted server stats");
        }

        string[] invalid =
        {
            "{\"name\":\"a\",\"name\":\"b\"}",
            "{\"count\":01}",
            "{\"count\":1.}",
            "{\"count\":1e}",
            "{\"flags\":[true,]}",
            "{\"name\":\"bad\\q\"}",
            "{\"count\":9223372036854775808}",
            "{\"count\":null}",
            "{\"name\":\"unterminated}",
            "{} trailing"
        };
        bool Reject(string value)
        {
            try
            {
                JsonContract.Read<Sample>(Encoding.UTF8.GetBytes(value));
                return false;
            }
            catch (FormatException)
            {
                return true;
            }
        }

        check(invalid.All(Reject), "malformed, ambiguous, overflowing and type-invalid release/stats JSON is rejected");
        check(Reject(new string ('[', 66) + "0" + new string (']', 66)), "excessively nested JSON is rejected before model binding");
        bool badUtf8 = false;
        try
        {
            JsonContract.Read<Sample>(new byte[] { 0xff });
        }
        catch (DecoderFallbackException)
        {
            badUtf8 = true;
        }

        check(badUtf8, "release metadata must be valid UTF-8");
        var missing = JsonContract.Read<Sample>(Encoding.UTF8.GetBytes("{\"unknown\":true}"));
        check(missing.Name == null && missing.Count == 0 && missing.Stats == null, "missing fields retain contract defaults while harmless unknown release fields are ignored");
        string diagnostic = UpdateDiagnostics.Failure(new Exception("private-token-and-signed-url", new InvalidOperationException("another-secret")));
        check(diagnostic.Contains("Exception > InvalidOperationException") && !diagnostic.Contains("secret") && !diagnostic.Contains("private-token"), "updater diagnostics identify failure types without logging credentials or raw messages");
        using (var handler = ReleaseClient.CreateHttpHandler())
            check(!handler.UseProxy && !handler.AllowAutoRedirect && !handler.UseCookies && handler.ServerCertificateCustomValidationCallback == null, "updater avoids machine proxy configuration and preserves manual redirects and TLS verification");
        check(GameplayText.Clean("Configurable prototype; native body pending validation.", "Blocks enemy vision.") == "Blocks enemy vision.", "technical placeholder descriptions are replaced with short gameplay descriptions");
        check(GameplayText.Clean("A bag of real SCP-330 candy; candy count and colors are configurable.", "Carries SCP candy.") == "Carries SCP candy.", "old generated role hints are cleaned without deleting the server config");
        check(GameplayText.Clean("  Carries extra\n bandages. ", "fallback") == "Carries extra bandages.", "short custom gameplay descriptions survive whitespace cleanup");
        check(GameplayText.Clean(new string ('x', 111), "Short ability.") == "Short ability." && GameplayText.Clean(null, "Short ability.") == "Short ability.", "empty or overlong descriptions use the actual short ability summary");
    }
}
