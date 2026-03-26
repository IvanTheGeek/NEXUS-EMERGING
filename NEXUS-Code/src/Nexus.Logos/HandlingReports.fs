namespace Nexus.Logos

open System
open System.IO

/// <summary>
/// One LOGOS note discovered during a handling-policy audit.
/// </summary>
type LogosHandlingNote =
    { RelativePath: string
      NoteKind: string
      Slug: string
      Title: string
      EntryPool: LogosPool option
      SourceSystemId: SourceSystemId
      IntakeChannelId: IntakeChannelId
      SignalKindId: SignalKindId
      Policy: LogosHandlingPolicy }

/// <summary>
/// A compact count row used in LOGOS handling audits.
/// </summary>
type LogosHandlingCount =
    { Slug: string
      Count: int }

/// <summary>
/// A report over LOGOS note handling-policy state.
/// </summary>
type LogosHandlingReport =
    { Notes: LogosHandlingNote list
      NoteKinds: LogosHandlingCount list
      EntryPools: LogosHandlingCount list
      Sensitivities: LogosHandlingCount list
      SharingScopes: LogosHandlingCount list
      SanitizationStatuses: LogosHandlingCount list
      RetentionClasses: LogosHandlingCount list
      RawNotes: LogosHandlingNote list
      PersonalPrivateNotes: LogosHandlingNote list
      CustomerConfidentialNotes: LogosHandlingNote list
      ApprovedForSharingNotes: LogosHandlingNote list }

/// <summary>
/// Builds reports over the handling metadata carried by LOGOS notes.
/// </summary>
/// <remarks>
/// This is intended as an operational audit, not publication permission by itself.
/// Full notes: docs/decisions/0011-restricted-by-default-intake-and-explicit-publication.md
/// </remarks>
[<RequireQualifiedAccess>]
module LogosHandlingReports =
    let private tryGetFrontMatterBlock (text: string) =
        let normalized = text.Replace("\r\n", "\n")
        let delimiter = "+++"

        if not (normalized.StartsWith(delimiter + "\n", StringComparison.Ordinal)) then
            None
        else
            let secondDelimiterIndex = normalized.IndexOf("\n+++\n", delimiter.Length + 1, StringComparison.Ordinal)

            if secondDelimiterIndex < 0 then
                None
            else
                let startIndex = delimiter.Length + 1
                let length = secondDelimiterIndex - startIndex
                Some(normalized.Substring(startIndex, length))

    let private tomlUnescape (value: string) =
        value.Replace("\\\"", "\"").Replace("\\\\", "\\")

    let private tryReadFrontMatterString key (frontMatter: string) =
        let prefix = key + " = \""

        frontMatter.Split('\n', StringSplitOptions.None)
        |> Array.tryPick (fun rawLine ->
            let line = rawLine.Trim()

            if line.StartsWith(prefix, StringComparison.Ordinal) && line.EndsWith("\"", StringComparison.Ordinal) then
                let value = line.Substring(prefix.Length, line.Length - prefix.Length - 1)
                Some(tomlUnescape value)
            else
                None)

    let private requireFrontMatterString path key frontMatter =
        match tryReadFrontMatterString key frontMatter with
        | Some value -> value
        | None -> invalidArg "docsRoot" $"Missing required front-matter key '{key}' in {path}."

    let private normalizeRelativePath (docsRoot: string) (path: string) =
        Path.GetRelativePath(docsRoot, path).Replace('\\', '/')

    let private parseNote docsRoot path =
        let text = File.ReadAllText(path)

        match tryGetFrontMatterBlock text with
        | None -> None
        | Some frontMatter ->
            let noteKind = requireFrontMatterString path "note_kind" frontMatter
            let slug = requireFrontMatterString path "slug" frontMatter
            let title = requireFrontMatterString path "title" frontMatter
            let entryPool = tryReadFrontMatterString "entry_pool" frontMatter |> Option.bind LogosPool.tryParse
            let sourceSystem = requireFrontMatterString path "source_system" frontMatter |> SourceSystemId.parse
            let intakeChannel = requireFrontMatterString path "intake_channel" frontMatter |> IntakeChannelId.parse
            let signalKind = requireFrontMatterString path "signal_kind" frontMatter |> SignalKindId.parse
            let sensitivity = requireFrontMatterString path "sensitivity" frontMatter |> SensitivityId.parse
            let sharingScope = requireFrontMatterString path "sharing_scope" frontMatter |> SharingScopeId.parse
            let sanitizationStatus = requireFrontMatterString path "sanitization_status" frontMatter |> SanitizationStatusId.parse
            let retentionClass = requireFrontMatterString path "retention_class" frontMatter |> RetentionClassId.parse

            Some
                { RelativePath = normalizeRelativePath docsRoot path
                  NoteKind = noteKind
                  Slug = slug
                  Title = title
                  EntryPool = entryPool
                  SourceSystemId = sourceSystem
                  IntakeChannelId = intakeChannel
                  SignalKindId = signalKind
                  Policy =
                    LogosHandlingPolicy.create
                        sensitivity
                        sharingScope
                        sanitizationStatus
                        retentionClass }

    let private noteFilesUnder path =
        if Directory.Exists(path) then
            Directory.EnumerateFiles(path, "*.md", SearchOption.AllDirectories)
            |> Seq.sort
            |> Seq.toList
        else
            []

    let private countBySlug selector notes =
        notes
        |> List.countBy selector
        |> List.sortBy (fun (slug, count) -> -count, slug)
        |> List.map (fun (slug, count) ->
            { Slug = slug
              Count = count })

    /// <summary>
    /// Scans the current LOGOS note folders and builds a handling-policy audit report.
    /// </summary>
    let build docsRoot =
        try
            let notes =
                [ Path.Combine(docsRoot, "logos-intake")
                  Path.Combine(docsRoot, "logos-intake-derived") ]
                |> List.collect noteFilesUnder
                |> List.choose (parseNote docsRoot)
                |> List.sortBy (fun note -> note.RelativePath)

            Ok
                { Notes = notes
                  NoteKinds = notes |> countBySlug (fun note -> note.NoteKind)
                  EntryPools =
                    notes
                    |> countBySlug (fun note ->
                        note.EntryPool
                        |> Option.map LogosPool.value
                        |> Option.defaultValue "(unspecified)")
                  Sensitivities = notes |> countBySlug (fun note -> SensitivityId.value note.Policy.SensitivityId)
                  SharingScopes = notes |> countBySlug (fun note -> SharingScopeId.value note.Policy.SharingScopeId)
                  SanitizationStatuses = notes |> countBySlug (fun note -> SanitizationStatusId.value note.Policy.SanitizationStatusId)
                  RetentionClasses = notes |> countBySlug (fun note -> RetentionClassId.value note.Policy.RetentionClassId)
                  RawNotes =
                    notes
                    |> List.filter (fun note -> note.Policy.SanitizationStatusId = KnownSanitizationStatuses.raw)
                  PersonalPrivateNotes =
                    notes
                    |> List.filter (fun note -> note.Policy.SensitivityId = KnownSensitivities.personalPrivate)
                  CustomerConfidentialNotes =
                    notes
                    |> List.filter (fun note -> note.Policy.SensitivityId = KnownSensitivities.customerConfidential)
                  ApprovedForSharingNotes =
                    notes
                    |> List.filter (fun note -> note.Policy.SanitizationStatusId = KnownSanitizationStatuses.approvedForSharing) }
        with :? ArgumentException as ex ->
            Error ex.Message
