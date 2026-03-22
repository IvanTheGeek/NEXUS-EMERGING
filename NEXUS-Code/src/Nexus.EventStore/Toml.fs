namespace Nexus.EventStore

open System
open System.Globalization
open System.Text

[<AutoOpen>]
module internal Toml =
    type Builder = ResizeArray<string>

    let create () = ResizeArray<string>()

    let private escapeString (value: string) =
        value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")

    let stringLiteral (value: string) = $"\"{escapeString value}\""

    let boolLiteral (value: bool) = if value then "true" else "false"

    let intLiteral (value: int) = value.ToString(CultureInfo.InvariantCulture)

    let int64Literal (value: int64) = value.ToString(CultureInfo.InvariantCulture)

    let decimalLiteral (value: decimal) = value.ToString(CultureInfo.InvariantCulture)

    let private fractionalPart (value: DateTimeOffset) =
        let fraction = value.ToUniversalTime().ToString("fffffff", CultureInfo.InvariantCulture)
        fraction.TrimEnd('0')

    let timestampLiteral (value: DateTimeOffset) =
        let utc = value.ToUniversalTime()
        let baseValue = utc.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
        let fraction = fractionalPart utc

        if String.IsNullOrEmpty(fraction) then
            stringLiteral $"{baseValue}Z"
        else
            stringLiteral $"{baseValue}.{fraction}Z"

    let stringListLiteral (values: string list) =
        values
        |> List.map stringLiteral
        |> String.concat ", "
        |> fun content -> $"[{content}]"

    let appendLine (builder: Builder) (line: string) =
        builder.Add(line)

    let appendBlank (builder: Builder) =
        appendLine builder ""

    let appendAssignment (builder: Builder) (key: string) (value: string) =
        appendLine builder $"{key} = {value}"

    let appendString (builder: Builder) (key: string) (value: string) =
        appendAssignment builder key (stringLiteral value)

    let appendStringOption (builder: Builder) (key: string) (value: string option) =
        value |> Option.iter (appendString builder key)

    let appendTimestamp (builder: Builder) (key: string) (value: DateTimeOffset) =
        appendAssignment builder key (timestampLiteral value)

    let appendTimestampOption (builder: Builder) (key: string) (value: DateTimeOffset option) =
        value |> Option.iter (appendTimestamp builder key)

    let appendInt (builder: Builder) (key: string) (value: int) =
        appendAssignment builder key (intLiteral value)

    let appendIntOption (builder: Builder) (key: string) (value: int option) =
        value |> Option.iter (appendInt builder key)

    let appendInt64Option (builder: Builder) (key: string) (value: int64 option) =
        value |> Option.iter (fun actual -> appendAssignment builder key (int64Literal actual))

    let appendBool (builder: Builder) (key: string) (value: bool) =
        appendAssignment builder key (boolLiteral value)

    let appendBoolOption (builder: Builder) (key: string) (value: bool option) =
        value |> Option.iter (appendBool builder key)

    let appendStringList (builder: Builder) (key: string) (values: string list) =
        appendAssignment builder key (stringListLiteral values)

    let appendTableHeader (builder: Builder) (path: string) =
        appendLine builder $"[{path}]"

    let appendArrayTableHeader (builder: Builder) (path: string) =
        appendLine builder $"[[{path}]]"

    let render (builder: Builder) =
        let text = StringBuilder()

        for line in builder do
            text.AppendLine(line) |> ignore

        text.ToString()
