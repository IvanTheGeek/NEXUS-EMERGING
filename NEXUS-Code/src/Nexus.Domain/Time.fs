namespace Nexus.Domain

open System

[<Struct>]
/// <summary>
/// The time an event or message is considered to have happened in the source world.
/// </summary>
type OccurredAt = OccurredAt of DateTimeOffset

[<Struct>]
/// <summary>
/// The time NEXUS observed a record during acquisition or reconciliation.
/// </summary>
type ObservedAt = ObservedAt of DateTimeOffset

[<Struct>]
/// <summary>
/// The time an import or capture action was appended into canonical history.
/// </summary>
type ImportedAt = ImportedAt of DateTimeOffset
