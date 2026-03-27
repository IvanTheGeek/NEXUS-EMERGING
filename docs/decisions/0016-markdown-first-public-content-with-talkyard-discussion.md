# 0016 Markdown-First Public Content With Talkyard Discussion

## Status

Accepted

## Context

NEXUS needs a public-facing way to share summaries, learnings, and refined material without turning the discussion platform itself into the source of truth for that content.

There is also a clear need to:

- keep public content versioned in Git
- keep public content reviewable and diffable
- separate the published statement from the feedback around it
- preserve rights, attribution, and public-safe boundaries
- leave room to ingest feedback and discussion back into NEXUS later

Talkyard is attractive because it can add discussion and feedback around static published pages instead of forcing the forum thread to be the article itself.

## Decision

NEXUS will prefer a Markdown-first public content model:

- the primary public content is authored and maintained as Markdown in the repository
- that Markdown is rendered into static HTML for publication
- Talkyard is attached as the discussion/comment layer around that published content
- the published page remains the canonical public statement
- the Talkyard thread remains the conversational layer around that statement

## Consequences

### Benefits

- public content stays versioned and reviewable in Git
- the publishing pipeline can respect public-safe pool and rights boundaries before material is exposed
- attribution and licensing can be carried in page metadata and surfaced prominently in the site shell
- GitHub Pages or similar static hosting becomes a viable first publishing target
- Talkyard feedback can later be ingested as a distinct LOGOS source stream rather than being confused with the article itself

### Rules

- import permission is not publication permission
- public-safe handling is necessary but not sufficient; rights policy must also allow public distribution
- comments and feedback are not merged silently into the published page
- Talkyard comments, web-scraped discussion, and API-derived discussion remain explicit acquisition streams

### Modeling Implications

- published article/page and discussion thread are linked but distinct
- article revisions are Git-visible and rebuildable
- Talkyard comments are later ingestible as LOGOS intake with their own:
  - source instance
  - access context
  - acquisition kind
  - rights policy
  - publication limits

## Near-Term Direction

The first likely shape is:

- Markdown in the repo
- static site generation into HTML
- GitHub Pages or similar static hosting
- Talkyard comment integration per page

Later, NEXUS should support:

- explicit mapping from a published page to its Talkyard discussion thread
- ingestion of the discussion layer as feedback
- attribution/help/about surfaces driven from published-content metadata and export manifests
