# Ruledger.Cli

[Ruledger](https://github.com/reny-develop/Ruledger) from a command line: walk a
[Rulealize](https://github.com/reny-develop/Rulealize) rule set and write its test design down,
and apply one to a later version of the rule set, which is the diff.

```sh
dotnet tool install -g Rulealize.Cli
dotnet tool install -g Ruledger.Cli
```

```sh
rulealize restore reversi.json     # fetch the vocabularies the rule set draws on
ruledger derive reversi.json       # walk it, and write the test design down
ruledger diff reversi.test-design.json reversi.json   # apply that design to a later version
ruledger observe reversi.json      # what a position is compared on, and what is dropped
```

`diff` is meant for a build server: it exits 0 when there is nothing to report, 1 when it
could not be done, 2 when the command line was not understood, and 3 when the rules decided
differently, a choice could not be carried, or the two versions are not compared on the same
state.

**[doc/tutorial.md](doc/tutorial.md) is the half hour that shows what this is like** — reversi
changed one line at a time, then a shift roster, ending in a count of what did not happen.
[Ruledger's doc/test-design.md](https://github.com/reny-develop/Ruledger/blob/main/doc/test-design.md)
is the form of the document the two commands produce.

What the tool prints is what the `Ruledger` package answers, put into lines for a person. A
program that wants the answer itself — what a diff found, as values — takes the package, as this
tool does.

Requires `net10.0` and the .NET SDK, which a `dotnet tool` implies.

## License

Apache-2.0. See [LICENSE](LICENSE).
