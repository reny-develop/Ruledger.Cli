// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using Rulealize.Abstraction.Plugin;
using Rulealize.Abstraction.Store;

namespace Ruledger.Cli.Tests
{
    /// <summary>The store <c>claim-month</c> and <c>claim-ledger</c> name, which nothing ever opens.</summary>
    /// <remarks>
    /// Public, so that the folder the tool is pointed at with <c>--plugins</c> holds it the way
    /// a restored store plugin would be held, and the tool finds it by sweeping. A walk keeps the
    /// documents in memory in its place; opening one fails, so a walk that reached for the real
    /// store would say so.
    /// </remarks>
    public sealed class FixtureStore : IStateStore
    {
        /// <inheritdoc />
        public Task<IStateStoreSession> OpenSessionAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Nothing should open the store a rule set names while it is walked.");
    }

    /// <summary>Declares <see cref="FixtureStore"/>, which is all a store plugin has to do.</summary>
    public sealed class FixtureStorePlugin : IRulealizePlugin
    {
        /// <inheritdoc />
        public PluginManifest Manifest { get; } = new("Ruledger.Fixture.Store", new Version(1, 0, 0), "fixture");

        /// <inheritdoc />
        public void Register(IPluginRegistry registry) => registry.AddStore<FixtureStore>();
    }
}
