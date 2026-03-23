namespace Nexus.Kernel

open System

[<Struct>]
/// <summary>
/// Identifies a semantic role or meaning classification applied to structure without changing the structure itself.
/// </summary>
type RoleId = private RoleId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Kernel.RoleId" />.
/// </summary>
[<RequireQualifiedAccess>]
module RoleId =
    /// <summary>
    /// Creates a role identifier from a stable non-blank slug.
    /// </summary>
    let create (value: string) =
        let normalized = value.Trim()

        if String.IsNullOrWhiteSpace(normalized) then
            invalidArg "value" "RoleId cannot be blank."

        RoleId normalized

    /// <summary>
    /// Parses a persisted role identifier.
    /// </summary>
    let parse (value: string) = create value

    /// <summary>
    /// Extracts the underlying role slug.
    /// </summary>
    let value (RoleId value) = value

[<Struct>]
/// <summary>
/// Identifies a stable semantic relation kind in the kernel ontology layer.
/// </summary>
type RelationKindId = private RelationKindId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Kernel.RelationKindId" />.
/// </summary>
[<RequireQualifiedAccess>]
module RelationKindId =
    /// <summary>
    /// Creates a relation-kind identifier from a stable non-blank slug.
    /// </summary>
    let create (value: string) =
        let normalized = value.Trim()

        if String.IsNullOrWhiteSpace(normalized) then
            invalidArg "value" "RelationKindId cannot be blank."

        RelationKindId normalized

    /// <summary>
    /// Parses a persisted relation-kind identifier.
    /// </summary>
    let parse (value: string) = create value

    /// <summary>
    /// Extracts the underlying relation-kind slug.
    /// </summary>
    let value (RelationKindId value) = value
