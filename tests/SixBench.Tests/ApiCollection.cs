namespace SixBench.Tests;

/// <summary>
/// Tests that start the web host. They must not run in parallel: each host freezes Serilog's static bootstrap logger.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection
{
    public const string Name = "Api host";
}
