namespace Nexus.Domain

open System

[<Struct>]
type OccurredAt = OccurredAt of DateTimeOffset

[<Struct>]
type ObservedAt = ObservedAt of DateTimeOffset

[<Struct>]
type ImportedAt = ImportedAt of DateTimeOffset
