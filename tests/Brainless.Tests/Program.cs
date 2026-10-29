using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Brainless.Internal;

int passed = 0;
void Check(bool condition, string name)
{
    if (!condition)
        throw new Exception("FAILED: " + name);
    passed++;
    Console.WriteLine("PASS " + name);
}

void Near(float actual, float expected, string name) => Check(Math.Abs(actual - expected) < .001f, name);
var random = new Random(731);
Check(Rules.WeightedIndex(new[] { 0, 0, 0 }, random) == -1, "zero weights leave a plain coin flip");
Check(Rules.WeightedIndex(new[] { -20, 0 }, random) == -1, "negative weights cannot cause an exception");
Check(Enumerable.Range(0, 1000).All(_ => Rules.WeightedIndex(new[] { 0, 8, 0 }, random) == 1), "zero-weight effects never win");
Check(Rules.WeightedIndex(new[] { int.MaxValue, int.MaxValue }, random) >= 0, "large weights do not overflow");
int rare = Enumerable.Range(0, 100000).Count(_ => Rules.WeightedIndex(new[] { 1, 99 }, random) == 0);
Check(rare > 800 && rare < 1200, "rare effects retain their weighted probability");
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
Near(Rules.ShieldDamage(100, 0, 1.2f, .7f), 70, "Nullifier health-only damage");
Near(Rules.ShieldDamage(100, 300, 1.2f, .7f), 120, "Nullifier shield-only damage");
Near(Rules.ShieldDamage(100, 60, 1.2f, .7f), 95, "Nullifier spillover uses only the unspent half of the shot");
Near(Rules.ShieldDamage(100, 120, 1.2f, .7f), 120, "exact shield break does not also deal health damage");
Near(Rules.Falloff(0, 3, 80), 80, "corpse burst has the configured center damage");
Near(Rules.Falloff(1.5f, 3, 80), 40, "corpse burst falls off linearly");
Near(Rules.Falloff(3, 3, 80), 0, "corpse burst cannot damage outside its radius");
Near(Math.Min(75, 3 * Rules.Falloff(0, 2.5f, 35)), 75, "three cluster children share one human damage ceiling");
Near(Math.Min(200, 3 * Rules.Falloff(0, 2.5f, 90)), 200, "three cluster children share one SCP damage ceiling");
if (args.Length > 0)
{
    string root = args[0];
    var names = new[]
    {
        "Roles/Brainless.Roles.dll",
        "Items/Brainless.Items.dll",
        "Weapons/Brainless.Weapons.dll",
        "BetterCoinflips/BetterCoinflips.dll"
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
        Check(references.Contains("Exiled.API"), relative + " references the real EXILED API");
    }
}

Console.WriteLine($"{passed} checks passed.");
