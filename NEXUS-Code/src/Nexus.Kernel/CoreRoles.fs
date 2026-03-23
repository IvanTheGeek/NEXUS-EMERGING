namespace Nexus.Kernel

/// <summary>
/// Stable role constants for the first ontology-kernel pass.
/// </summary>
/// <remarks>
/// The kernel stays intentionally small. These are the roles that currently look stable enough to name without freezing a larger ontology too early.
/// Full notes: docs/nexus-ontology-imprint-alignment.md
/// </remarks>
[<RequireQualifiedAccess>]
module CoreRoles =
    /// <summary>
    /// The domain-neutral role for a persistent structural result of causality that later interpretation can consume.
    /// </summary>
    let imprint = RoleId.create "imprint"
