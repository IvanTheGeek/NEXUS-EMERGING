---
title: Public Content Publishing And Talkyard Comments
discussion_id: nexus-emerging:talkyard-comments-policy
---

# Public Content Publishing And Talkyard Comments

This note describes the likely first public-content architecture for NEXUS.

## Goal

Publish curated NEXUS content in a way that is:

- static and easy to host
- versioned in Git
- public-safe by policy
- open to feedback and discussion
- later ingestible back into NEXUS

## High-Level Shape

The preferred first shape is:

1. author curated content in Markdown in the repository
2. render that Markdown into static HTML
3. publish the static site
4. attach Talkyard comments to the page
5. treat the page and the comments as related but distinct artifacts

## Why Markdown First

Markdown-first public content gives NEXUS:

- version control over the actual published statement
- review and diff visibility
- a clean path to public-safe export checks
- a place to attach attribution and rights metadata
- a stable canonical page even if discussion continues changing

## Why Talkyard Around It

Talkyard is valuable here because it can supply:

- discussion
- feedback
- clarifications
- refinement signals
- lightweight community response

without requiring the Talkyard thread itself to become the canonical article.

## Canonical Public Statement Vs Discussion Layer

The intended split is:

- static page
  - canonical public statement
  - versioned and rebuilt from Git
  - explicitly public-safe and rights-safe
- Talkyard discussion
  - conversation about the page
  - ongoing feedback and refinement
  - later ingestible as LOGOS intake

That separation should stay explicit.

## Safety And Rights Boundary

Only material that crosses the public-safe boundary should become a published page.

That means:

- handling policy must allow it
- rights policy must allow public distribution
- attribution requirements must be carried forward

Comments are separate:

- their visibility may differ by instance or access context
- their rights may differ from the page itself
- ingesting them later must preserve that metadata

## Likely First Hosting Shape

The current likely first publishing target is:

- static HTML generated from repo Markdown
- GitHub Pages or similar static hosting
- Talkyard embedded or attached per page for comments

This keeps the hosting simple while the architecture matures.

## Current NEXUS Site Shape

The current NEXUS docs site now uses:

- GitHub Pages for static site hosting
- `https://talkyard.ivanthegeek.com` for the discussion layer
- comments rendered at the bottom of each page just above the site footer
- comments enabled by default across the site
- stable explicit discussion identifiers derived from the source Markdown path

The current intended Talkyard category shape is:

- top-level category: `NEXUS`
- public subcategory: `NEXUS Site Comments`
- category external ID used by the embed: `nexus_site_comments`

That keeps site-attached comments public and replyable from either the embedded page or the Talkyard side without mixing them into unrelated human-first discussion areas.

## Future NEXUS Flow

Later, NEXUS should be able to model all of these explicitly:

- published page
- source Markdown
- site build artifact
- linked Talkyard thread
- Talkyard comments
- later refined changes to the published page

That would allow a feedback loop such as:

1. publish a public-safe page
2. receive Talkyard comments
3. ingest comments as LOGOS signals
4. refine the page or spawn concept/decision work
5. publish a revised page

## Data Shape To Preserve Later

When this becomes more concrete, we should preserve at least:

- page slug
- source Markdown path
- published URL
- title
- publication timestamp
- public-safe/export manifest reference
- rights and attribution metadata
- linked Talkyard instance
- linked Talkyard thread identifier or canonical URL

## Placement In NEXUS Concern Lines

This touches multiple concern lines:

- LOGOS intake and handling
- external integrations
- repository governance
- later FnHCI/FnUI publishing surfaces

But the dominant idea is:

- public content is a curated output
- discussion is a linked intake stream
- both must remain traceable and policy-aware

For later extracted sites:

- CheddarBooks/LaundryLog should get a sibling public subcategory under a `CheddarBooks` top-level category
- that downstream site should use its own external ID instead of sharing the NEXUS comment stream
