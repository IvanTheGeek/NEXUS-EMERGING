namespace Nexus.Logos

/// <summary>
/// Stable source-system identifiers currently recognized as concrete LOGOS sources.
/// </summary>
/// <remarks>
/// This list stays intentionally small. Additional systems can be introduced as explicit slugs when they become real intake paths.
/// Full notes: docs/logos-source-model-v0.md
/// </remarks>
[<RequireQualifiedAccess>]
module KnownSourceSystems =
    /// <summary>
    /// ChatGPT provider capture and export sources.
    /// </summary>
    let chatgpt = SourceSystemId.create "chatgpt"

    /// <summary>
    /// Claude provider capture and export sources.
    /// </summary>
    let claude = SourceSystemId.create "claude"

    /// <summary>
    /// Grok provider capture and export sources.
    /// </summary>
    let grok = SourceSystemId.create "grok"

    /// <summary>
    /// Local Codex session capture sources.
    /// </summary>
    let codex = SourceSystemId.create "codex"

    /// <summary>
    /// Forum-originated intake sources.
    /// </summary>
    let forum = SourceSystemId.create "forum"

    /// <summary>
    /// Email-originated intake sources.
    /// </summary>
    let email = SourceSystemId.create "email"

    /// <summary>
    /// Issue-tracker or bug-tracker intake sources.
    /// </summary>
    let issueTracker = SourceSystemId.create "issue-tracker"

    /// <summary>
    /// Deployed-app feedback surfaces.
    /// </summary>
    let appFeedbackSurface = SourceSystemId.create "app-feedback-surface"

    /// <summary>
    /// The explicit allowlist of recognized LOGOS source systems.
    /// </summary>
    let all =
        [ chatgpt
          claude
          grok
          codex
          forum
          email
          issueTracker
          appFeedbackSurface ]

    let private catalog =
        [ chatgpt, "ChatGPT provider capture and export sources."
          claude, "Claude provider capture and export sources."
          grok, "Grok provider capture and export sources."
          codex, "Local Codex session capture sources."
          forum, "Forum-originated intake sources."
          email, "Email-originated intake sources."
          issueTracker, "Issue-tracker or bug-tracker intake sources."
          appFeedbackSurface, "Deployed-app feedback surfaces." ]

    let private bySlug =
        catalog
        |> List.map (fun (identifier, _) -> SourceSystemId.value identifier, identifier)
        |> Map.ofList

    /// <summary>
    /// Looks up a source system by its explicit allowlisted slug.
    /// </summary>
    let tryFind (value: string) =
        let normalized = value.Trim().ToLowerInvariant()

        if System.String.IsNullOrWhiteSpace(normalized) then
            None
        else
            Map.tryFind normalized bySlug

    /// <summary>
    /// Lists the allowlisted source systems with human-facing summaries.
    /// </summary>
    let described =
        catalog |> List.map (fun (identifier, summary) -> SourceSystemId.value identifier, summary)

/// <summary>
/// Stable intake-channel identifiers for early LOGOS work.
/// </summary>
/// <remarks>
/// Intake channels describe how a signal entered NEXUS, not the full ontology meaning of the signal.
/// Full notes: docs/logos-source-model-v0.md
/// </remarks>
[<RequireQualifiedAccess>]
module CoreIntakeChannels =
    /// <summary>
    /// AI conversational intake such as chat or session transcripts.
    /// </summary>
    let aiConversation = IntakeChannelId.create "ai-conversation"

    /// <summary>
    /// Forum-thread intake.
    /// </summary>
    let forumThread = IntakeChannelId.create "forum-thread"

    /// <summary>
    /// Email-thread intake.
    /// </summary>
    let emailThread = IntakeChannelId.create "email-thread"

    /// <summary>
    /// Bug-report intake.
    /// </summary>
    let bugReport = IntakeChannelId.create "bug-report"

    /// <summary>
    /// Deployed-app feedback intake.
    /// </summary>
    let appFeedback = IntakeChannelId.create "app-feedback"

    /// <summary>
    /// The explicit allowlist of recognized LOGOS intake channels.
    /// </summary>
    let all =
        [ aiConversation
          forumThread
          emailThread
          bugReport
          appFeedback ]

    let private catalog =
        [ aiConversation, "AI conversational intake such as chat or session transcripts."
          forumThread, "Forum-thread intake."
          emailThread, "Email-thread intake."
          bugReport, "Bug-report intake."
          appFeedback, "Deployed-app feedback intake." ]

    let private bySlug =
        catalog
        |> List.map (fun (identifier, _) -> IntakeChannelId.value identifier, identifier)
        |> Map.ofList

    /// <summary>
    /// Looks up an intake channel by its explicit allowlisted slug.
    /// </summary>
    let tryFind (value: string) =
        let normalized = value.Trim().ToLowerInvariant()

        if System.String.IsNullOrWhiteSpace(normalized) then
            None
        else
            Map.tryFind normalized bySlug

    /// <summary>
    /// Lists the allowlisted intake channels with human-facing summaries.
    /// </summary>
    let described =
        catalog |> List.map (fun (identifier, summary) -> IntakeChannelId.value identifier, summary)

/// <summary>
/// Stable signal-kind identifiers for early LOGOS work.
/// </summary>
/// <remarks>
/// Signal kinds classify intake meaning at a coarse level without pretending to model the full refined knowledge layer yet.
/// Full notes: docs/logos-source-model-v0.md
/// </remarks>
[<RequireQualifiedAccess>]
module CoreSignalKinds =
    /// <summary>
    /// A conversational thread or session viewed as a LOGOS signal.
    /// </summary>
    let conversation = SignalKindId.create "conversation"

    /// <summary>
    /// An individual message viewed as a LOGOS signal.
    /// </summary>
    let message = SignalKindId.create "message"

    /// <summary>
    /// A bug report viewed as a LOGOS signal.
    /// </summary>
    let bugReport = SignalKindId.create "bug-report"

    /// <summary>
    /// A feedback submission viewed as a LOGOS signal.
    /// </summary>
    let feedback = SignalKindId.create "feedback"

    /// <summary>
    /// A support question viewed as a LOGOS signal.
    /// </summary>
    let supportQuestion = SignalKindId.create "support-question"

    /// <summary>
    /// The explicit allowlist of recognized LOGOS signal kinds.
    /// </summary>
    let all =
        [ conversation
          message
          bugReport
          feedback
          supportQuestion ]

    let private catalog =
        [ conversation, "A conversational thread or session viewed as a LOGOS signal."
          message, "An individual message viewed as a LOGOS signal."
          bugReport, "A bug report viewed as a LOGOS signal."
          feedback, "A feedback submission viewed as a LOGOS signal."
          supportQuestion, "A support question viewed as a LOGOS signal." ]

    let private bySlug =
        catalog
        |> List.map (fun (identifier, _) -> SignalKindId.value identifier, identifier)
        |> Map.ofList

    /// <summary>
    /// Looks up a signal kind by its explicit allowlisted slug.
    /// </summary>
    let tryFind (value: string) =
        let normalized = value.Trim().ToLowerInvariant()

        if System.String.IsNullOrWhiteSpace(normalized) then
            None
        else
            Map.tryFind normalized bySlug

    /// <summary>
    /// Lists the allowlisted signal kinds with human-facing summaries.
    /// </summary>
    let described =
        catalog |> List.map (fun (identifier, summary) -> SignalKindId.value identifier, summary)
