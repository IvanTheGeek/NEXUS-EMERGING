# Report Conversation Overlap Candidates

Use this when you want a conservative, explainable first pass at possible cross-source overlap between two providers' conversation projections.

This command does not reconcile anything.

It reports heuristic candidates only.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-conversation-overlap-candidates \
  --left-provider codex \
  --right-provider chatgpt
```

## Optional Root And Limit

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-conversation-overlap-candidates \
  --event-store-root /path/to/NEXUS-EventStore \
  --left-provider codex \
  --right-provider claude \
  --limit 10
```

## What It Uses

The command reads rebuilt conversation projections under:

- `NEXUS-EventStore/projections/conversations`

So it is best used after:

- `rebuild-conversation-projections`

## Current Signals

Candidates are based on explicit, explainable signals such as:

- normalized title equality
- strong shared title-token overlap
- overlapping conversation time windows
- conversation time windows within a short distance
- equal or close message counts

The command prints the signals that caused each candidate to appear.

## Important Note

This is a candidate report, not reconciliation.

It exists to help future explicit overlap workflows, especially for cases like:

- local Codex capture vs later provider export
- provider export vs later API capture
- other multi-source intake paths inside LOGOS

Absence from this report does not prove that no overlap exists.

Presence in this report does not mean the conversations should be merged.
