// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using Rulealize;
using Rulealize.Abstraction;

namespace Ruledger.Cli
{
    /// <summary>The documents a rule set holds, gathered from the folders they sit in.</summary>
    /// <remarks>
    /// <para>
    /// A composite is walked as the one rule set it stands for, so every document it names
    /// through <c>uses</c> has to be here before anything starts. What a composite's guards
    /// decide runs through its components' guards, so a walk given a component short answers
    /// about a set of inputs with some of them missing — and nothing in the answer says any
    /// were. That is why nothing below carries on past a document it could not find.
    /// </para>
    /// <para>
    /// Two folders, in the order Rulealize.Cli established: the one the document sits in,
    /// where a component its author is writing lives, and then the one <c>rulealize restore</c>
    /// fetched into. The author's wins. Which document answers to an identifier is read out
    /// of the document's own <c>id</c> and never off its file name.
    /// </para>
    /// </remarks>
    internal static class Held
    {
        /// <summary>Where <c>rulealize restore</c> puts a fetched component, and where this reads.</summary>
        public const string Fetched = "component";

        /// <summary>Gathers what a rule set holds, and what those hold in turn.</summary>
        /// <param name="ruleSetPath">The document that was asked about.</param>
        /// <param name="ruleSet">Its text.</param>
        /// <param name="ruleSets">What <c>--rulesets</c> said, or null for the default.</param>
        /// <returns>
        /// The document of every rule set reachable through <c>uses</c>, by identifier, or
        /// null when why not has already been written to standard error. A rule set that
        /// holds nothing gets an empty map and no folder is read.
        /// </returns>
        public static IReadOnlyDictionary<string, string>? Gather(string ruleSetPath, string ruleSet, string? ruleSets)
        {
            Dictionary<string, string> documents = new(StringComparer.Ordinal);

            if (Uses(ruleSetPath, ruleSet) is not IReadOnlyList<RuleSetRequirement> uses)
            {
                return null;
            }

            if (uses.Count is 0)
            {
                return documents;
            }

            IReadOnlyList<string> where = Folders(ruleSetPath, ruleSets);
            Dictionary<string, (string Path, string Text)> index = Index(where);

            List<RuleSetRequirement> missing = [];
            Queue<RuleSetRequirement> pending = new(uses);

            while (pending.Count is not 0)
            {
                RuleSetRequirement requirement = pending.Dequeue();

                if (documents.ContainsKey(requirement.RuleSet))
                {
                    continue;
                }

                if (!index.TryGetValue(requirement.RuleSet, out (string Path, string Text) found))
                {
                    missing.Add(requirement);
                    continue;
                }

                documents[requirement.RuleSet] = found.Text;

                if (Uses(found.Path, found.Text) is not IReadOnlyList<RuleSetRequirement> nested)
                {
                    return null;
                }

                foreach (RuleSetRequirement further in nested)
                {
                    pending.Enqueue(further);
                }
            }

            if (missing.Count is not 0)
            {
                Console.Error.WriteLine(
                    $"'{ruleSetPath}' holds rule sets that are in neither {string.Join(" nor ", where.Select(Quoted))}:");
                foreach (RuleSetRequirement unfound in missing)
                {
                    Console.Error.WriteLine($"  {unfound}");
                }

                Console.Error.WriteLine();
                Console.Error.WriteLine("Without them the guards of the composite cannot be answered, and what would");
                Console.Error.WriteLine("come back is a set of legal inputs with some of them missing and no sign that");
                Console.Error.WriteLine("any were. 'rulealize restore <rule set>' fetches the published ones.");
                return null;
            }

            return documents;
        }

        /// <summary>Gathers the state documents a <c>many</c> names as where its instances start.</summary>
        /// <param name="ruleSetPath">The document that was asked about.</param>
        /// <param name="ruleSet">Its text.</param>
        /// <param name="held">What <see cref="Gather"/> found it holds.</param>
        /// <param name="ruleSets">What <c>--rulesets</c> said, or null for the default.</param>
        /// <returns>
        /// Each document by the name a <c>start</c> gives it, or null when why not has already
        /// been written to standard error. A rule set whose <c>uses</c> names no start gets an
        /// empty map and no folder is read.
        /// </returns>
        /// <remarks>
        /// A <c>start</c> names a state document, and the runtime is handed it by that name; it
        /// is not a rule set, so nothing fetches it, and it is written beside the rule set that
        /// names it. It is looked for as <c>&lt;name&gt;.json</c> in the same two folders as a
        /// held rule set, nearest first, and one that is not there is refused rather than walked
        /// without: a new instance would start somewhere its author did not say.
        /// </remarks>
        public static IReadOnlyDictionary<string, string>? Starts(
            string ruleSetPath,
            string ruleSet,
            IReadOnlyDictionary<string, string> held,
            string? ruleSets)
        {
            Dictionary<string, string> documents = new(StringComparer.Ordinal);
            List<string> names = [.. new[] { ruleSet }.Concat(held.Values).SelectMany(Named).Distinct(StringComparer.Ordinal)];

            if (names.Count is 0)
            {
                return documents;
            }

            IReadOnlyList<string> where = Folders(ruleSetPath, ruleSets);
            List<string> missing = [];

            foreach (string name in names)
            {
                string? found = where.Select(folder => Path.Combine(folder, name + ".json")).FirstOrDefault(File.Exists);
                if (found is null)
                {
                    missing.Add(name);
                    continue;
                }

                documents[name] = File.ReadAllText(found);
            }

            if (missing.Count is not 0)
            {
                Console.Error.WriteLine(
                    $"'{ruleSetPath}' starts instances in state documents that are in neither {string.Join(" nor ", where.Select(Quoted))}:");
                foreach (string name in missing)
                {
                    Console.Error.WriteLine($"  {name}.json");
                }

                Console.Error.WriteLine();
                Console.Error.WriteLine("A 'many' names where its new instances start, and without that document a walk");
                Console.Error.WriteLine("would start them somewhere nobody said. Write it beside the rule set that names it.");
                return null;
            }

            return documents;
        }

        // The `start` of every `many` a document's `uses` declares. Read off the JSON, unlike what
        // a document holds, because nothing Rulealize publishes says which names a document
        // starts its instances at: it is handed them by name. A reading here that disagreed with
        // the runtime's would show as the runtime refusing, by name, a start it was not given,
        // which is said in its words like any other reason a walk could not begin.
        private static IEnumerable<string> Named(string ruleSet)
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(ruleSet, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });
            }
            catch (JsonException)
            {
                yield break;
            }

            using (document)
            {
                if (!document.RootElement.TryGetProperty("uses", out JsonElement uses) || uses.ValueKind != JsonValueKind.Array)
                {
                    yield break;
                }

                foreach (JsonElement use in uses.EnumerateArray())
                {
                    if (use.ValueKind == JsonValueKind.Object
                        && use.TryGetProperty("many", out JsonElement many)
                        && many.ValueKind == JsonValueKind.Object
                        && many.TryGetProperty("start", out JsonElement start)
                        && start.ValueKind == JsonValueKind.String
                        && start.GetString() is { Length: > 0 } name)
                    {
                        yield return name;
                    }
                }
            }
        }

        private static IReadOnlyList<string> Folders(string ruleSetPath, string? ruleSets)
        {
            string own = Path.GetDirectoryName(ruleSetPath) is { Length: > 0 } directory ? directory : ".";
            string fetched = ruleSets ?? Fetched;

            return string.Equals(Path.GetFullPath(own), Path.GetFullPath(fetched), StringComparison.OrdinalIgnoreCase)
                ? [own]
                : [own, fetched];
        }

        private static IReadOnlyList<RuleSetRequirement>? Uses(string path, string ruleSet)
        {
            try
            {
                return RuleSetRequirement.ReadFrom(ruleSet);
            }
            catch (RuleSetBuildException failure)
            {
                Console.Error.WriteLine($"'{path}' does not say what it holds:");
                Console.Error.WriteLine($"  {failure.Message}");
                return null;
            }
        }

        private static Dictionary<string, (string Path, string Text)> Index(IReadOnlyList<string> folders)
        {
            Dictionary<string, (string Path, string Text)> index = new(StringComparer.Ordinal);

            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                foreach (string path in Directory.EnumerateFiles(folder, "*.json"))
                {
                    if (Identity(path) is not (string id, string text))
                    {
                        continue;
                    }

                    // Nearest first, so a component beside the document that holds it shadows
                    // a fetched one of the same identifier: it is the one its author meant.
                    _ = index.TryAdd(id, (path, text));
                }
            }

            return index;
        }

        // Speculative: the folder a rule set sits in holds states, outcomes and test designs
        // too, and none of them is a mistake to find. A document meant to be a rule set and
        // unreadable is not lost for long — it comes back above as an identifier nothing
        // answered to, named by the document that wanted it.
        private static (string Id, string Text)? Identity(string path)
        {
            try
            {
                string text = File.ReadAllText(path);
                return RuleSetIdentity.ReadFrom(text).Id is { Length: > 0 } id ? (id, text) : null;
            }
            catch (Exception exception)
                when (exception is JsonException or IOException or UnauthorizedAccessException or RuleSetBuildException)
            {
                return null;
            }
        }

        private static string Quoted(string folder) => $"'{folder}'";
    }
}
