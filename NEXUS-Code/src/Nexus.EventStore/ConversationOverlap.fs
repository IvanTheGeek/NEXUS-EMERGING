namespace Nexus.EventStore

open System
open System.Collections.Generic
open System.Globalization
open System.IO

/// <summary>
/// Describes one conversation-level overlap candidate between two acquisition sources.
/// </summary>
/// <remarks>
/// This is a heuristic candidate report only. It does not reconcile or merge conversations.
/// Full notes: docs/decisions/0009-overlap-reconciliation-is-explicit.md
/// </remarks>
type ConversationOverlapCandidate =
    { LeftConversationId: string
      LeftProvider: string
      LeftTitle: string option
      LeftMessageCount: int
      LeftFirstOccurredAt: DateTimeOffset option
      LeftLastOccurredAt: DateTimeOffset option
      RightConversationId: string
      RightProvider: string
      RightTitle: string option
      RightMessageCount: int
      RightFirstOccurredAt: DateTimeOffset option
      RightLastOccurredAt: DateTimeOffset option
      Score: int
      Signals: string list }

/// <summary>
/// Summarizes heuristic overlap candidates between two providers' conversation projections.
/// </summary>
type ConversationOverlapCandidateReport =
    { LeftProvider: string
      RightProvider: string
      LeftConversationCount: int
      RightConversationCount: int
      CandidateCount: int
      ReportedCount: int
      Candidates: ConversationOverlapCandidate list }

/// <summary>
/// Builds conservative conversation-level overlap candidates across provider projections.
/// </summary>
/// <remarks>
/// This report preserves the NEXUS rule that overlap reconciliation stays explicit, traceable, and reversible.
/// It surfaces explainable candidates without collapsing history automatically.
/// Full workflow notes: docs/how-to/report-conversation-overlap-candidates.md
/// </remarks>
[<RequireQualifiedAccess>]
module ConversationOverlap =
    type private ConversationProjectionSummary =
        { ConversationId: string
          Providers: string list
          Title: string option
          MessageCount: int
          FirstOccurredAt: DateTimeOffset option
          LastOccurredAt: DateTimeOffset option
          NormalizedTitle: string option
          TitleTokens: Set<string> }

    let private tryParseInt (value: string option) =
        value
        |> Option.bind (fun (rawValue: string) ->
            match Int32.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture) with
            | true, parsedValue -> Some parsedValue
            | false, _ -> None)

    let private tryParseTimestamp (value: string option) =
        value
        |> Option.bind (fun (rawValue: string) ->
            match DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) with
            | true, parsedValue -> Some parsedValue
            | false, _ -> None)

    let private isAsciiUppercase (character: char) =
        character >= 'A' && character <= 'Z'

    let private isAsciiLowercase (character: char) =
        character >= 'a' && character <= 'z'

    let private isAsciiDigit (character: char) =
        character >= '0' && character <= '9'

    let private isExplicitSeparator (character: char) =
        character = ' ' || character = '-' || character = '_'

    let private normalizeTitle (value: string option) =
        value
        |> Option.bind (fun rawValue ->
            let builder = Text.StringBuilder()
            let mutable previousWasSeparator = true

            for character in rawValue.Trim() do
                if isAsciiLowercase character || isAsciiDigit character then
                    builder.Append(character) |> ignore
                    previousWasSeparator <- false
                elif isAsciiUppercase character then
                    builder.Append(Char.ToLowerInvariant(character)) |> ignore
                    previousWasSeparator <- false
                elif isExplicitSeparator character then
                    if not previousWasSeparator then
                        builder.Append(' ') |> ignore

                    previousWasSeparator <- true
                else
                    if not previousWasSeparator then
                        builder.Append(' ') |> ignore

                    previousWasSeparator <- true

            let normalized = builder.ToString().Trim()

            match String.IsNullOrWhiteSpace(normalized) with
            | true -> None
            | false -> Some normalized)

    let private titleTokens (normalizedTitle: string option) =
        match normalizedTitle with
        | Some value ->
            value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            |> Array.toList
            |> List.filter (fun token -> token.Length >= 2)
            |> Set.ofList
        | None -> Set.empty

    let private projectionRelativeRoot =
        Path.Combine("projections", "conversations")

    let private tryLoadSummary path =
        let document = File.ReadAllText(path) |> TomlDocument.parse
        let providers = TomlDocument.tryStringList "providers" document |> Option.defaultValue []

        match TomlDocument.tryScalar "conversation_id" document, providers with
        | Some conversationId, _ :: _ ->
            let title = TomlDocument.tryScalar "title" document
            let normalizedTitle = normalizeTitle title

            Some
                { ConversationId = conversationId
                  Providers = providers
                  Title = title
                  MessageCount = TomlDocument.tryScalar "message_count" document |> tryParseInt |> Option.defaultValue 0
                  FirstOccurredAt = TomlDocument.tryScalar "first_occurred_at" document |> tryParseTimestamp
                  LastOccurredAt = TomlDocument.tryScalar "last_occurred_at" document |> tryParseTimestamp
                  NormalizedTitle = normalizedTitle
                  TitleTokens = titleTokens normalizedTitle }
        | _ -> None

    let private loadSummaries eventStoreRoot =
        let projectionsRoot = Path.Combine(Path.GetFullPath(eventStoreRoot), projectionRelativeRoot)

        if Directory.Exists(projectionsRoot) then
            Directory.EnumerateFiles(projectionsRoot, "*.toml", SearchOption.TopDirectoryOnly)
            |> Seq.sort
            |> Seq.choose tryLoadSummary
            |> Seq.toList
        else
            []

    let private timeRange value =
        match value.FirstOccurredAt, value.LastOccurredAt with
        | Some firstOccurredAt, Some lastOccurredAt ->
            Some(min firstOccurredAt lastOccurredAt, max firstOccurredAt lastOccurredAt)
        | Some firstOccurredAt, None -> Some(firstOccurredAt, firstOccurredAt)
        | None, Some lastOccurredAt -> Some(lastOccurredAt, lastOccurredAt)
        | None, None -> None

    let private overlapOrDistance leftValue rightValue =
        match timeRange leftValue, timeRange rightValue with
        | Some (leftStart, leftFinish), Some (rightStart, rightFinish) ->
            if leftFinish < rightStart then
                false, Some(rightStart - leftFinish)
            elif rightFinish < leftStart then
                false, Some(leftStart - rightFinish)
            else
                true, Some TimeSpan.Zero
        | _ -> false, None

    let private evaluatePair leftProvider rightProvider leftValue rightValue =
        if leftValue.ConversationId = rightValue.ConversationId then
            None
        else
            let exactTitleMatch =
                match leftValue.NormalizedTitle, rightValue.NormalizedTitle with
                | Some leftTitle, Some rightTitle when leftTitle = rightTitle -> true
                | _ -> false

            let sharedTokenCount =
                Set.intersect leftValue.TitleTokens rightValue.TitleTokens |> Set.count

            let unionTokenCount =
                Set.union leftValue.TitleTokens rightValue.TitleTokens |> Set.count

            let strongTokenMatch =
                sharedTokenCount >= 2
                && unionTokenCount > 0
                && (float sharedTokenCount / float unionTokenCount) >= 0.6

            let timeOverlap, timeDistance = overlapOrDistance leftValue rightValue

            let within72Hours =
                match timeDistance with
                | Some value when value.TotalHours <= 72.0 -> true
                | _ -> false

            let messageDifference = abs (leftValue.MessageCount - rightValue.MessageCount)
            let messageCountsEqual = messageDifference = 0
            let messageCountsClose = messageDifference > 0 && messageDifference <= 2

            let hasTitleSignal = exactTitleMatch || strongTokenMatch
            let hasSupportingSignal = timeOverlap || within72Hours || messageCountsEqual || messageCountsClose

            if not hasTitleSignal || not hasSupportingSignal then
                None
            else
                let signals = ResizeArray<string>()
                let mutable score = 0

                if exactTitleMatch then
                    signals.Add("exact_title_slug")
                    score <- score + 60
                elif strongTokenMatch then
                    signals.Add($"shared_title_tokens={sharedTokenCount}/{unionTokenCount}")
                    score <- score + 40

                if timeOverlap then
                    signals.Add("time_window_overlap")
                    score <- score + 25
                elif within72Hours then
                    let hours =
                        timeDistance
                        |> Option.map (fun value -> Math.Round(value.TotalHours, 1))
                        |> Option.defaultValue 0.0

                    let formattedHours = hours.ToString("0.0", CultureInfo.InvariantCulture)
                    signals.Add($"time_window_within_hours={formattedHours}")
                    score <- score + 15

                if messageCountsEqual then
                    signals.Add("message_count_equal")
                    score <- score + 10
                elif messageCountsClose then
                    signals.Add("message_count_close")
                    score <- score + 5

                Some
                    { LeftConversationId = leftValue.ConversationId
                      LeftProvider = leftProvider
                      LeftTitle = leftValue.Title
                      LeftMessageCount = leftValue.MessageCount
                      LeftFirstOccurredAt = leftValue.FirstOccurredAt
                      LeftLastOccurredAt = leftValue.LastOccurredAt
                      RightConversationId = rightValue.ConversationId
                      RightProvider = rightProvider
                      RightTitle = rightValue.Title
                      RightMessageCount = rightValue.MessageCount
                      RightFirstOccurredAt = rightValue.FirstOccurredAt
                      RightLastOccurredAt = rightValue.LastOccurredAt
                      Score = score
                      Signals = signals |> Seq.toList }

    let private candidateSortKey value =
        -value.Score,
        value.LeftTitle |> Option.defaultValue value.LeftConversationId,
        value.RightTitle |> Option.defaultValue value.RightConversationId,
        value.LeftConversationId,
        value.RightConversationId

    /// <summary>
    /// Builds a conservative overlap-candidate report between two providers' conversation projections.
    /// </summary>
    /// <param name="eventStoreRoot">The event-store root that contains conversation projections.</param>
    /// <param name="leftProvider">The first provider slug to inspect.</param>
    /// <param name="rightProvider">The second provider slug to inspect.</param>
    /// <param name="limit">The maximum number of candidates to return.</param>
    /// <returns>A conservative list of explainable overlap candidates.</returns>
    let buildReport eventStoreRoot leftProvider rightProvider limit =
        let summaries = loadSummaries eventStoreRoot

        let leftValues =
            summaries
            |> List.filter (fun value -> value.Providers |> List.contains leftProvider)

        let rightValues =
            summaries
            |> List.filter (fun value -> value.Providers |> List.contains rightProvider)

        let candidates =
            [ for leftValue in leftValues do
                  for rightValue in rightValues do
                      match evaluatePair leftProvider rightProvider leftValue rightValue with
                      | Some value -> yield value
                      | None -> () ]
            |> List.sortBy candidateSortKey

        { LeftProvider = leftProvider
          RightProvider = rightProvider
          LeftConversationCount = leftValues.Length
          RightConversationCount = rightValues.Length
          CandidateCount = candidates.Length
          ReportedCount = candidates |> List.truncate limit |> List.length
          Candidates = candidates |> List.truncate limit }
