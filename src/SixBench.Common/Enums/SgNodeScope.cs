namespace SixBench.Common.Enums;

/// <summary>
/// Which SceneGraph nodes to dump from the running channel.
/// </summary>
public enum SgNodeScope
{
    /// <summary>Every node the channel created (ECP <c>/query/sgnodes/all</c>).</summary>
    All,
    /// <summary>Nodes with no parent, kept alive by BrightScript references (ECP <c>/query/sgnodes/roots</c>).</summary>
    Roots,
    /// <summary>Nodes whose <c>id</c> field matches a given id (ECP <c>/query/sgnodes/nodes</c>).</summary>
    Nodes,
}
