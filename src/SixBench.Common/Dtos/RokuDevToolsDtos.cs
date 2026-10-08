using SixBench.Common.Enums;

namespace SixBench.Common.Dtos;

/// <summary>
/// One SceneGraph node from <c>/query/sgnodes/*</c>.
/// </summary>
/// <param name="Type">Node type (the XML element name, e.g. <c>Poster</c> or a component name).</param>
/// <param name="Attributes">Every field the Roku reported, as strings (e.g. <c>bounds</c>, <c>focused</c>, <c>osref</c>).</param>
/// <param name="Children">Child nodes.</param>
public sealed record SgNodeDto(
    string Type,
    IReadOnlyDictionary<string, string> Attributes,
    IReadOnlyList<SgNodeDto> Children);

/// <summary>
/// A SceneGraph node dump.
/// </summary>
/// <param name="Scope">Which query produced it.</param>
/// <param name="Nodes">Top-level nodes.</param>
/// <param name="TotalNodes">Number of nodes in the whole tree.</param>
/// <param name="RetrievedUtc">When the dump was taken.</param>
public sealed record SgNodesDto(SgNodeScope Scope, IReadOnlyList<SgNodeDto> Nodes, int TotalNodes, DateTime RetrievedUtc);

/// <summary>
/// CPU and memory use of the foreground channel, from <c>/query/chanperf</c>.
/// </summary>
/// <param name="AppId">Channel id (<c>dev</c> for a sideloaded channel).</param>
/// <param name="TimestampMs">Roku clock, Unix milliseconds, if reported.</param>
/// <param name="CpuDurationSeconds">Window the CPU percentages cover.</param>
/// <param name="CpuUserPercent">User-mode CPU percent.</param>
/// <param name="CpuSysPercent">Kernel-mode CPU percent.</param>
/// <param name="MemoryUsedBytes">Total memory used.</param>
/// <param name="MemoryResidentBytes">Resident set size.</param>
/// <param name="MemoryAnonBytes">Anonymous (heap) memory.</param>
/// <param name="MemoryFileBytes">File-backed memory.</param>
/// <param name="MemorySharedBytes">Shared memory.</param>
/// <param name="MemorySwapBytes">Swapped-out memory.</param>
/// <param name="ProcessId">Channel process id, if reported.</param>
public sealed record ChanPerfDto(
    string? AppId,
    long? TimestampMs,
    double? CpuDurationSeconds,
    double? CpuUserPercent,
    double? CpuSysPercent,
    long? MemoryUsedBytes,
    long? MemoryResidentBytes,
    long? MemoryAnonBytes,
    long? MemoryFileBytes,
    long? MemorySharedBytes,
    long? MemorySwapBytes,
    int? ProcessId);

/// <summary>
/// A channel's persistent registry, from <c>/query/registry/{appId}</c>.
/// </summary>
/// <param name="AppId">The channel id that was queried.</param>
/// <param name="DevId">Developer id the channel is signed with.</param>
/// <param name="SpaceAvailableBytes">Registry space left, in bytes.</param>
/// <param name="Sections">Registry sections.</param>
public sealed record RokuRegistryDto(
    string AppId,
    string? DevId,
    int? SpaceAvailableBytes,
    IReadOnlyList<RokuRegistrySectionDto> Sections);

/// <summary>
/// One registry section.
/// </summary>
/// <param name="Name">Section name.</param>
/// <param name="Items">Key/value pairs.</param>
public sealed record RokuRegistrySectionDto(string Name, IReadOnlyList<RokuRegistryItemDto> Items);

/// <summary>
/// One registry entry.
/// </summary>
/// <param name="Key">Key.</param>
/// <param name="Value">Value.</param>
public sealed record RokuRegistryItemDto(string Key, string Value);
