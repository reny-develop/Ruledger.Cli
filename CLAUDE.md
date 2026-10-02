## Project
Ruledger.Cli is [Ruledger](https://github.com/reny-develop/Ruledger) from a shell: `ruledger derive`, `ruledger diff` and `ruledger observe`. The walk, the test design and the diff are the `Ruledger` package's, taken from nuget.org like any host takes it; this tool prints what it answers. Nothing of a test design is worked out here — a second account of one could disagree with the one a host reads.

The `dotnet tool` in `src/Ruledger.Cli/` (`net10.0`, nullable + implicit usings), and an xUnit suite in `test/` holding what it prints and what it exits with. It was in the Ruledger repository until 1.3.0, when the library became a package of its own; the history before that is there.

Two documents must stay true: the README, and `doc/tutorial.md`, the half hour that shows what the tool is like. Every console transcript in them was captured from a run and has to stay a true prefix of what the command prints today. The tutorial downloads its rule sets from Ruledger's `verify/ruleset/`. The form of a test design is Ruledger's `doc/test-design.md`, linked and never restated.

## Fixtures
`test/ruleset/` holds copies of the rule sets in Ruledger's `verify/ruleset/` the tests run the tool on. **Do not change what is in there**; add one instead, copied from there.

## Releasing, in order
The README is packed *into* the package and nuget.org cannot replace it afterwards, so every document is correct on disk **before** `dotnet pack`. A release that takes a new `Ruledger` waits until that version is on nuget.org. Then: raise the `Ruledger` reference and `<Version>` in `src/Ruledger.Cli/Ruledger.Cli.csproj`, which is the only place carrying the tool's number; `dotnet pack src/Ruledger.Cli/Ruledger.Cli.csproj -c Release -o artifacts`; open the `.nupkg` and read the README and the nuspec out of it; install the package itself with `--add-source artifacts` and run the three commands; then push. No document names the current version.

## Building and testing
`dotnet test test/Ruledger.Cli.Tests.csproj`. A few seconds. The vocabularies come in as NuGet packages and land beside the test assembly, which is how the runtime finds them. Before a `Ruledger` it takes is on nuget.org, restore with `--source https://api.nuget.org/v3/index.json --source <Ruledger>/artifacts`.

The `doc-sync` machinery in the workspace covers folders named `Rulealize*` and does not reach here; the tutorial's `rulealize restore` transcript is the one fact registered in its `docmap.json`.

## Words
No new terms. **In Japanese, never write 文書 for a rule set.** A rule set is ルールセット, a state is 状態, an input is 入力.
