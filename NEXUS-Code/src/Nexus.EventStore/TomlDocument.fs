namespace Nexus.EventStore

open System
open System.Collections.Generic

type TomlDocument =
    { Scalars: Dictionary<string, string>
      Tables: Dictionary<string, Dictionary<string, string>>
      TableArrays: Dictionary<string, ResizeArray<Dictionary<string, string>>> }

[<RequireQualifiedAccess>]
module TomlDocument =
    let private parseTomlValue (rawValue: string) =
        let trimmed = rawValue.Trim()

        if trimmed.StartsWith("\"", StringComparison.Ordinal)
           && trimmed.EndsWith("\"", StringComparison.Ordinal)
           && trimmed.Length >= 2 then
            trimmed.Substring(1, trimmed.Length - 2)
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
        else
            trimmed

    let parse (text: string) =
        let scalars = Dictionary<string, string>(StringComparer.Ordinal)
        let tables = Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal)
        let tableArrays = Dictionary<string, ResizeArray<Dictionary<string, string>>>(StringComparer.Ordinal)
        let mutable currentTable: Dictionary<string, string> option = None
        let mutable currentArrayTable: Dictionary<string, string> option = None

        let ensureTable path =
            match tables.TryGetValue(path) with
            | true, table -> table
            | false, _ ->
                let table = Dictionary<string, string>(StringComparer.Ordinal)
                tables[path] <- table
                table

        for rawLine in text.Replace("\r\n", "\n").Split('\n') do
            let line = rawLine.Trim()

            if not (String.IsNullOrWhiteSpace(line)) && not (line.StartsWith("#", StringComparison.Ordinal)) then
                if line.StartsWith("[[", StringComparison.Ordinal) && line.EndsWith("]]", StringComparison.Ordinal) then
                    let path = line.Substring(2, line.Length - 4).Trim()
                    let target =
                        match tableArrays.TryGetValue(path) with
                        | true, arrayTables -> arrayTables
                        | false, _ ->
                            let arrayTables = ResizeArray()
                            tableArrays[path] <- arrayTables
                            arrayTables

                    let table = Dictionary<string, string>(StringComparer.Ordinal)
                    target.Add(table)
                    currentTable <- None
                    currentArrayTable <- Some table
                elif line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal) then
                    let path = line.Substring(1, line.Length - 2).Trim()
                    currentTable <- Some (ensureTable path)
                    currentArrayTable <- None
                else
                    let separatorIndex = line.IndexOf('=')

                    if separatorIndex > 0 then
                        let key = line.Substring(0, separatorIndex).Trim()
                        let value = line.Substring(separatorIndex + 1) |> parseTomlValue

                        match currentArrayTable, currentTable with
                        | Some arrayTable, _ -> arrayTable[key] <- value
                        | None, Some table -> table[key] <- value
                        | None, None -> scalars[key] <- value

        { Scalars = scalars
          Tables = tables
          TableArrays = tableArrays }

    let tryScalar key (document: TomlDocument) =
        match document.Scalars.TryGetValue(key) with
        | true, value -> Some value
        | false, _ -> None

    let tryTableValue path key (document: TomlDocument) =
        match document.Tables.TryGetValue(path) with
        | true, table ->
            match table.TryGetValue(key) with
            | true, value -> Some value
            | false, _ -> None
        | false, _ -> None

    let tableArray path (document: TomlDocument) =
        match document.TableArrays.TryGetValue(path) with
        | true, tables -> tables |> Seq.toList
        | false, _ -> []
