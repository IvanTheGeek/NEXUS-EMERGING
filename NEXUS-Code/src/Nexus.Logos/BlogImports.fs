namespace Nexus.Logos

open System
open System.IO
open System.Text

/// <summary>
/// The input used to import one public blog repository into LOGOS intake notes.
/// </summary>
/// <remarks>
/// Full workflow notes: docs/how-to/import-logos-blog-repo.md
/// </remarks>
type ImportLogosBlogRepoRequest =
    { DocsRoot: string
      RepoRoot: string
      SourceBaseUri: string
      SourceInstanceId: SourceInstanceId option
      ExtraTags: string list }

/// <summary>
/// One blog post that was imported into a LOGOS intake note.
/// </summary>
type LogosImportedBlogPost =
    { RelativeSourcePath: string
      Title: string
      PostSlug: string
      NoteSlug: string
      OutputPath: string
      CapturedAt: DateTimeOffset option }

/// <summary>
/// One blog file that was skipped during import.
/// </summary>
type LogosSkippedBlogPost =
    { RelativeSourcePath: string
      Reason: string }

/// <summary>
/// The result of importing a public blog repository into LOGOS intake notes.
/// </summary>
type ImportLogosBlogRepoResult =
    { DocsRoot: string
      RepoRoot: string
      SourceBaseUri: string
      SourceInstanceId: SourceInstanceId
      ImportedPosts: LogosImportedBlogPost list
      SkippedPosts: LogosSkippedBlogPost list }

[<RequireQualifiedAccess>]
module LogosBlogImports =
    type private ParsedBlogPost =
        { RelativeSourcePath: string
          Title: string
          Slug: string
          NativeItemId: string option
          CapturedAt: DateTimeOffset option
          Summary: string option
          Tags: string list
          SourceUri: string }

    let private defaultPolicy =
        LogosHandlingPolicy.create
            KnownSensitivities.publicData
            KnownSharingScopes.publicAudience
            KnownSanitizationStatuses.approvedForSharing
            KnownRetentionClasses.durable

    let private defaultRights =
        LogosRightsContext.create KnownRightsPolicies.ownerControlled None

    let private normalizeRequiredPath (name: string) (value: string) =
        let normalized = value.Trim()

        if String.IsNullOrWhiteSpace(normalized) then
            invalidArg name $"{name} cannot be blank."

        Path.GetFullPath(normalized)

    let private normalizeRequiredUri (name: string) (value: string) =
        let normalized = value.Trim()

        if String.IsNullOrWhiteSpace(normalized) then
            invalidArg name $"{name} cannot be blank."

        let candidate =
            if normalized.EndsWith("/", StringComparison.Ordinal) then
                normalized
            else
                normalized + "/"

        match Uri.TryCreate(candidate, UriKind.Absolute) with
        | true, uri -> uri
        | _ -> invalidArg name $"{name} must be an absolute URI."

    let private trimWrappedQuotes (value: string) =
        let normalized = value.Trim()

        if normalized.Length >= 2 && normalized.StartsWith("\"", StringComparison.Ordinal) && normalized.EndsWith("\"", StringComparison.Ordinal) then
            normalized.Substring(1, normalized.Length - 2)
        else
            normalized

    let private normalizeStableSlugCandidate (value: string) =
        let builder = StringBuilder()
        let lower = value.Trim().ToLowerInvariant()

        let appendDashIfNeeded () =
            if builder.Length > 0 && builder.[builder.Length - 1] <> '-' then
                builder.Append('-') |> ignore

        for character in lower do
            if (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') then
                builder.Append(character) |> ignore
            elif character = '-' || character = '.' || character = '_' || Char.IsWhiteSpace(character) then
                appendDashIfNeeded ()
            else
                appendDashIfNeeded ()

        let normalized = builder.ToString().Trim('-')
        StableSlugs.validate "stableSlug" normalized

    let private deriveSourceInstanceId (sourceBaseUri: Uri) =
        sourceBaseUri.Host
        |> normalizeStableSlugCandidate
        |> SourceInstanceId.create

    let private parseTags (value: string option) =
        value
        |> Option.defaultValue String.Empty
        |> fun raw -> raw.Split(',', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries)
        |> Array.toList
        |> List.map normalizeStableSlugCandidate
        |> List.filter (fun tag -> not (String.IsNullOrWhiteSpace(tag)))
        |> List.distinct

    let private tryParseTimestamp (value: string option) =
        match value |> Option.map trimWrappedQuotes with
        | Some candidate ->
            match DateTimeOffset.TryParse(candidate) with
            | true, parsed -> Some parsed
            | _ -> None
        | None -> None

    let private tryReadFrontMatter (content: string) =
        let normalized = content.Replace("\r\n", "\n")

        if not (normalized.StartsWith("---\n", StringComparison.Ordinal)) then
            None
        else
            let closingIndex = normalized.IndexOf("\n---\n", 4, StringComparison.Ordinal)

            if closingIndex < 0 then
                None
            else
                let frontMatter = normalized.Substring(4, closingIndex - 4)
                let body = normalized.Substring(closingIndex + 5)

                let fields =
                    frontMatter.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    |> Array.choose (fun line ->
                        let separatorIndex = line.IndexOf(':')

                        if separatorIndex <= 0 then
                            None
                        else
                            let key = line.Substring(0, separatorIndex).Trim()
                            let value = line.Substring(separatorIndex + 1).Trim()
                            Some(key, value))
                    |> Map.ofArray

                Some(fields, body.Trim())

    let private tryExtractSummary (body: string) =
        body.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun block -> block.Trim())
        |> Array.tryFind (fun block ->
            not (String.IsNullOrWhiteSpace(block))
            && not (block.StartsWith("```", StringComparison.Ordinal))
            && not (block.StartsWith("![", StringComparison.Ordinal))
            && not (block.StartsWith("<hr", StringComparison.OrdinalIgnoreCase)))

    let private enumerateMarkdownFiles repoRoot =
        Directory.EnumerateFiles(repoRoot, "*.md", SearchOption.AllDirectories)
        |> Seq.filter (fun path ->
            let relativePath = Path.GetRelativePath(repoRoot, path).Replace('\\', '/')
            not (relativePath.StartsWith(".git/", StringComparison.Ordinal))
            && not (String.Equals(relativePath, "README.md", StringComparison.OrdinalIgnoreCase)))
        |> Seq.sort
        |> Seq.toList

    let private tryParseBlogPost repoRoot sourceBaseUri path =
        let relativePath = Path.GetRelativePath(repoRoot, path).Replace('\\', '/')
        let content = File.ReadAllText(path)

        match tryReadFrontMatter content with
        | None ->
            Error
                { RelativeSourcePath = relativePath
                  Reason = "Missing expected front matter." }
        | Some(fields, body) ->
            let tryField name = Map.tryFind name fields |> Option.map trimWrappedQuotes

            match tryField "title", tryField "slug" with
            | Some title, Some slug ->
                try
                    let normalizedPostSlug = normalizeStableSlugCandidate slug
                    let sourceUri = Uri(sourceBaseUri, normalizedPostSlug).AbsoluteUri

                    Ok
                        { RelativeSourcePath = relativePath
                          Title = title
                          Slug = normalizedPostSlug
                          NativeItemId = tryField "cuid"
                          CapturedAt = tryParseTimestamp (tryField "datePublished")
                          Summary = tryExtractSummary body
                          Tags = parseTags (tryField "tags")
                          SourceUri = sourceUri }
                with :? ArgumentException as ex ->
                    Error
                        { RelativeSourcePath = relativePath
                          Reason = ex.Message }
            | _ ->
                Error
                    { RelativeSourcePath = relativePath
                      Reason = "Missing required blog front-matter fields: title and slug." }

    let private buildNoteSlug (sourceInstanceId: SourceInstanceId) (postSlug: string) =
        StableSlugs.validate "noteSlug" $"{SourceInstanceId.value sourceInstanceId}-{postSlug}"

    /// <summary>
    /// Imports a public owner-controlled Markdown blog repository into public-safe LOGOS intake notes.
    /// </summary>
    let importRepo (request: ImportLogosBlogRepoRequest) =
        try
            let docsRoot = normalizeRequiredPath "DocsRoot" request.DocsRoot
            let repoRoot = normalizeRequiredPath "RepoRoot" request.RepoRoot
            let sourceBaseUri = normalizeRequiredUri "SourceBaseUri" request.SourceBaseUri

            let sourceInstanceId =
                defaultArg request.SourceInstanceId (deriveSourceInstanceId sourceBaseUri)

            let accessContext =
                LogosAccessContext.create
                    (Some sourceInstanceId)
                    KnownAccessContexts.owner
                    KnownAcquisitionKinds.gitSync

            let extraTags =
                request.ExtraTags
                |> List.map normalizeStableSlugCandidate
                |> List.distinct

            let importedPosts, skippedPosts =
                enumerateMarkdownFiles repoRoot
                |> List.fold
                    (fun (imported, skipped) path ->
                        match tryParseBlogPost repoRoot sourceBaseUri path with
                        | Error skippedPost ->
                            (imported, skippedPost :: skipped)
                        | Ok post ->
                            let locators =
                                [ match post.NativeItemId with
                                  | Some nativeItemId -> LogosLocator.nativeItemId nativeItemId
                                  | None -> ()
                                  LogosLocator.sourceUri post.SourceUri ]

                            let noteRequest =
                                { DocsRoot = docsRoot
                                  Slug = buildNoteSlug sourceInstanceId post.Slug
                                  Title = post.Title
                                  SourceSystemId = KnownSourceSystems.blog
                                  AccessContext = accessContext
                                  IntakeChannelId = CoreIntakeChannels.publishedArticle
                                  SignalKindId = CoreSignalKinds.article
                                  EntryPool = LogosPool.PublicSafe
                                  Policy = defaultPolicy
                                  RightsContext = defaultRights
                                  Locators = locators
                                  CapturedAt = post.CapturedAt
                                  Summary = post.Summary
                                  Tags = [ "blog"; "blog-post" ] @ post.Tags @ extraTags }

                            match LogosIntakeNotes.create noteRequest with
                            | Ok result ->
                                let importedPost =
                                    { RelativeSourcePath = post.RelativeSourcePath
                                      Title = post.Title
                                      PostSlug = post.Slug
                                      NoteSlug = result.NormalizedSlug
                                      OutputPath = result.OutputPath
                                      CapturedAt = post.CapturedAt }

                                (importedPost :: imported, skipped)
                            | Error error when error.Contains("already exists", StringComparison.OrdinalIgnoreCase) ->
                                let skippedPost =
                                    { RelativeSourcePath = post.RelativeSourcePath
                                      Reason = error }

                                (imported, skippedPost :: skipped)
                            | Error error ->
                                let skippedPost =
                                    { RelativeSourcePath = post.RelativeSourcePath
                                      Reason = error }

                                (imported, skippedPost :: skipped))
                    ([], [])

            Ok
                { DocsRoot = docsRoot
                  RepoRoot = repoRoot
                  SourceBaseUri = sourceBaseUri.AbsoluteUri
                  SourceInstanceId = sourceInstanceId
                  ImportedPosts = importedPosts |> List.rev
                  SkippedPosts = skippedPosts |> List.rev }
        with :? ArgumentException as ex ->
            Error ex.Message
