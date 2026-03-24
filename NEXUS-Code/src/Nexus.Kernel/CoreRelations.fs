namespace Nexus.Kernel

/// <summary>
/// Stable relation constants for the first ontology-kernel pass.
/// </summary>
/// <remarks>
/// These relations intentionally name only the smallest semantic spine needed by the current imprint alignment.
/// Full notes: docs/nexus-ontology-imprint-alignment.md
/// </remarks>
[<RequireQualifiedAccess>]
module CoreRelations =
    /// <summary>
    /// Relates a causal source to an imprint it produced.
    /// </summary>
    let producesImprint = RelationKindId.create "produces_imprint"

    /// <summary>
    /// Relates an interpretation process or perspective to the imprint it interprets.
    /// </summary>
    let interpretsFromImprint = RelationKindId.create "interprets_from_imprint"

    /// <summary>
    /// Relates an imprint to a derived projection produced from it.
    /// </summary>
    let derivesProjection = RelationKindId.create "derives_projection"
