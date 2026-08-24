using Xunit;

namespace ShiftSoftware.ADP.Hawta.Tests;

/// <summary>
/// The one xUnit collection every emulator-backed test class belongs to, so they never run at
/// the same time as each other.
///
/// <para>xUnit runs the methods of a single class serially but runs different classes in
/// parallel. Both emulator classes provision a database and containers at fixture time, and the
/// emulator answers 503 to container creation when two of them arrive together under full-suite
/// load. The fixtures treat that as "the environment cannot run this" and skip — so the arcs did
/// not fail, they silently did not run: a full suite reported 15 skipped while the same tests
/// passed one class at a time. A shared collection name makes xUnit serialize them, which is all
/// the emulator needed.</para>
///
/// <para>This is a name only — no collection fixture. Each class keeps its own fixture, its own
/// database and its own key prefixes; sharing state across them would trade a contention problem
/// for an isolation one.</para>
/// </summary>
[CollectionDefinition(Name)]
public sealed class CosmosEmulatorCollection
{
    public const string Name = "Cosmos emulator";
}
