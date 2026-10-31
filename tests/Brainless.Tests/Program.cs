using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Brainless.Internal;
using Brainless.Internal.Updates;

if (args.Length == 4 && args[0] == "--verify-assembly")
{
    var assembly = System.Reflection.AssemblyName.GetAssemblyName(args[1]);
    if (assembly.Name != args[2] || UpdatePolicy.Normalize(assembly.Version) != UpdatePolicy.ParseVersion(args[3]))
        throw new Exception("Release DLL identity/version does not match its project and manifest.");
    Console.WriteLine("Verified release DLL: " + assembly.Name + " " + assembly.Version);
    return;
}

int passed = 0;
void Check(bool condition, string name)
{
    if (!condition)
        throw new Exception("FAILED: " + name);
    passed++;
    Console.WriteLine("PASS " + name);
}

void Near(float actual, float expected, string name) => Check(Math.Abs(actual - expected) < .001f, name);
if (args.Length == 2 && args[0] == "--verify-native")
{
    MetadataContracts.Run(args[1], Check);
#if NATIVE_CONFIG_TESTS
    DistributionConfigTests.Run(Check);
#endif
    Console.WriteLine($"{passed} native contracts passed.");
    return;
}

var random = new Random(731);
LockpickTests.Run(Check);
VoiceTests.Run(Check);
CandyTests.Run(Check);
AudioTests.Run(Check);
TuningTests.Run(Check);
DistributionTests.Run(Check);
Check(Rules.WeightedIndex(new[] { 0, 0, 0 }, random) == -1, "zero weights leave a plain coin flip");
Check(Rules.WeightedIndex(new[] { -20, 0 }, random) == -1, "negative weights cannot cause an exception");
Check(Enumerable.Range(0, 1000).All(_ => Rules.WeightedIndex(new[] { 0, 8, 0 }, random) == 1), "zero-weight effects never win");
Check(Rules.WeightedIndex(new[] { int.MaxValue, int.MaxValue }, random) >= 0, "large weights do not overflow");
int rare = Enumerable.Range(0, 100000).Count(_ => Rules.WeightedIndex(new[] { 1, 99 }, random) == 0);
Check(rare > 800 && rare < 1200, "rare effects retain their weighted probability");
var availableRoles = new[]
{
    "missing weapon",
    "disabled profile",
    "ordinary valid role"
};
Check(Enumerable.Range(0, 1000).All(_ => Rules.ChooseEligible(availableRoles, r => r == "ordinary valid role", _ => 10, random) == "ordinary valid role"), "unavailable roles cannot consume an otherwise successful custom-role draw");
Check(Rules.ChooseEligible(availableRoles, _ => false, _ => 10, random) == null, "no eligible profiles cleanly preserve the native role");
Check(Rules.ChooseEligible(availableRoles, _ => true, _ => 0, random) == null, "zero-weight profiles never become an accidental fallback role");
Check(Rules.ChooseEligible(Array.Empty<string>(), _ => true, _ => 10, random) == null, "an empty faction roster safely keeps the native role");
var budget = new LimitedBudget();
budget.Reset(2);
Check(budget.TryIssue("gas", 1), "first item gets a supply slot");
Check(!budget.TryIssue("gas", 1), "per-type cap blocks duplicate minting");
Check(budget.TryIssue("filter", 2), "paired filter consumes the second slot");
Check(!budget.TryIssue("dressing", 4), "no item can exceed global supply");
Check(budget.Total == 2, "failed issuance does not spend supply");
budget.Reset(6);
Check(budget.Total == 0 && budget.Count("gas") == 0, "a new round resets supply");
Check(!budget.TryIssue("invalid", 2, -1), "negative counts cannot refund supply");
budget.Reset(0);
Check(!budget.TryIssue("cluster", 0), "a small-lobby cluster is blocked by normal issuance");
Check(budget.TryIssue("cluster", 0, ignoreLimits: true), "admin issuance ignores a small-lobby cluster supply cap");
Check(budget.Total == 0 && budget.Count("cluster") == 0, "admin issuance does not spend the normal round supply");
Check(!budget.TryIssue("cluster", 0, -1, ignoreLimits: true), "admin issuance cannot mint negative quantities");
budget.Reset(1);
Check(budget.TryIssue("rifle", 1), "normal firearm issuance spends the available slot");
Check(budget.TryIssue("rifle", 1, ignoreLimits: true), "admin issuance works after natural firearm supply is exhausted");
Check(budget.Total == 1 && !budget.TryIssue("rifle", 1), "an admin firearm does not unlock or refill natural supply");
Check(budget.RevokeIssue("rifle") && budget.Total == 0, "a failed native delivery refunds the uncreated firearm's supply");
Check(!budget.RevokeIssue("rifle") && budget.Total == 0, "a duplicate rollback cannot create extra supply");
Check(!budget.RevokeIssue("rifle", -1), "a negative rollback cannot mint supply");
var deniedGrant = new GrantReport();
deniedGrant.Record("Player 1", "Clusters require at least 12 players.");
Check(!deniedGrant.AnySucceeded, "a rejected custom give returns command failure");
Check(deniedGrant.Describe("Cluster Grenade").Contains("given to 0/1") && deniedGrant.Describe("Cluster Grenade").Contains("12 players"), "a rejected custom give reports zero deliveries and the actual reason");
var mixedGrant = new GrantReport();
mixedGrant.Record("Player 1", null);
mixedGrant.Record("Player 2", "Inventory is full.");
Check(mixedGrant.AnySucceeded && mixedGrant.Succeeded == 1 && mixedGrant.Attempted == 2, "bulk custom gives count only confirmed deliveries");
Check(mixedGrant.Describe("Nullifier").Contains("1/2") && mixedGrant.Describe("Nullifier").Contains("Inventory is full."), "partial grants report actual counts and per-player failures");
Check(Rules.ClusterCap(11) == 0 && Rules.ClusterCap(12) == 1 && Rules.ClusterCap(24) == 2, "small lobbies receive no clusters");
Check(Rules.WeaponCap(23) == 1 && Rules.WeaponCap(24) == 2, "weapon supply scales at the correct boundary");
var ledger = new ShotLedger();
ledger.Register(77, 6);
Check(ledger.Consume(77), "Last Word spends its first shot");
ledger.Register(77, 6);
Check(ledger.Remaining(77) == 5, "a drop or reacquisition cannot refill Last Word");
for (int i = 0; i < 5; i++)
    Check(ledger.Consume(77), "Last Word remaining shot " + i);
Check(!ledger.Consume(77), "Last Word blocks its seventh shot");
Check(!ledger.Consume(78), "an unissued serial cannot fire");
ledger.Clear();
Check(ledger.Remaining(77) == 0, "old shot state cannot enter a new round");
ledger.Register(90, 16);
Check(Enumerable.Range(0, 16).All(_ => ledger.Consume(90)), "a launcher can spend all sixteen grenades");
Check(!ledger.Consume(90), "a launcher cannot fire a seventeenth grenade");
ledger.Register(90, 16);
Check(ledger.Remaining(90) == 0, "dropping and reacquiring a launcher cannot refill its grenades");
ledger.Register(91, 16);
Check(ledger.Remaining(91) == 16 && ledger.Remaining(90) == 0, "a fresh launcher has its own independent sixteen-shot budget");
Near(Rules.ShieldDamage(100, 0, 1.2f, .7f), 70, "Nullifier health-only damage");
Near(Rules.ShieldDamage(100, 300, 1.2f, .7f), 120, "Nullifier shield-only damage");
Near(Rules.ShieldDamage(100, 60, 1.2f, .7f), 95, "Nullifier spillover uses only the unspent half of the shot");
Near(Rules.ShieldDamage(100, 120, 1.2f, .7f), 120, "exact shield break does not also deal health damage");
Near(Rules.Falloff(0, 3, 80), 80, "corpse burst has the configured center damage");
Near(Rules.Falloff(1.5f, 3, 80), 40, "corpse burst falls off linearly");
Near(Rules.Falloff(3, 3, 80), 0, "corpse burst cannot damage outside its radius");
var blast = new BlastBudget();
Near(Enumerable.Range(0, 4).Sum(_ => blast.Take("human", false, 35)), 75, "four cluster children share one human damage ceiling");
Near(Enumerable.Range(0, 4).Sum(_ => blast.Take("scp", true, 90)), 200, "four cluster children share one SCP damage ceiling");
Near(blast.Take("human", false, 35), 0, "extra cluster events cannot exceed an exhausted damage ceiling");
Near(blast.Take("other human", false, 35), 35, "each player has a separate cluster damage allowance");
Near(blast.Take("negative", false, -20), 0, "negative blast damage cannot refill a shared allowance");
Near(Rules.Falloff(2.5f, 2.5f, 45), 0, "launcher damage ends at its reduced blast radius");
Near(Rules.Falloff(1.25f, 2.5f, 45), 22.5f, "launcher damage decreases within its reduced blast radius");
await UpdaterTests.Run(Check);
if (args.Length > 0)
{
    string root = args[0];
    MetadataContracts.Run(root, Check);
#if NATIVE_CONFIG_TESTS
    DistributionConfigTests.Run(Check);
#endif
    var names = new[]
    {
        "Roles/Brainless.Roles.dll",
        "Items/Brainless.Items.dll",
        "Weapons/Brainless.Weapons.dll",
        "BetterCoinflips/BetterCoinflips.dll",
        "ScpProximityChat/Brainless.ScpProximityChat.dll"
    };
    var ownAssemblies = names.Select(p => Path.GetFileNameWithoutExtension(p)).ToArray();
    foreach (string relative in names)
    {
        string folder = relative.Split('/')[0];
        string file = Path.Combine(root, folder, "bin", "Release", Path.GetFileName(relative));
        using var stream = File.OpenRead(file);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        var references = reader.AssemblyReferences.Select(h => reader.GetString(reader.GetAssemblyReference(h).Name)).ToArray();
        Check(!references.Intersect(ownAssemblies).Any(), relative + " has no dependency on another suite DLL");
        Check(System.Linq.Enumerable.Contains(references, "Exiled.API"), relative + " references the real EXILED API");
        if (folder == "ScpProximityChat")
            Check(!references.Any(name => name.Contains("NAudio", StringComparison.OrdinalIgnoreCase) || name.Contains("SecretAPI", StringComparison.OrdinalIgnoreCase) || name.Contains("AudioPlayer", StringComparison.OrdinalIgnoreCase)), "SCP proximity chat has no external audio or settings DLL dependency");
    }

    using var nativeCommands = File.OpenRead(Path.Combine(root, ".build", "references", "Exiled.CustomItems.dll"));
    using var nativePe = new PEReader(nativeCommands);
    var nativeReader = nativePe.GetMetadataReader();
    foreach (string command in new[]
    {
        "Give",
        "Spawn"
    }

    )
    {
        var type = nativeReader.TypeDefinitions.Select(h => nativeReader.GetTypeDefinition(h)).Single(t => nativeReader.GetString(t.Namespace) == "Exiled.CustomItems.Commands" && nativeReader.GetString(t.Name) == command);
        var execute = type.GetMethods().Select(h => nativeReader.GetMethodDefinition(h)).Single(m => nativeReader.GetString(m.Name) == "Execute");
        var parameters = execute.GetParameters().Select(h => nativeReader.GetParameter(h)).Where(p => p.SequenceNumber > 0).OrderBy(p => p.SequenceNumber).Select(p => nativeReader.GetString(p.Name)).ToArray();
        Check(parameters.SequenceEqual(new[] { "arguments", "sender", "response" }), command + " command patch matches the official EXILED 9.15.2 target");
    }
}

Console.WriteLine($"{passed} checks passed.");
