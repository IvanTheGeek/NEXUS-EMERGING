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
