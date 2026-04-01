---
title: Talkyard
comments: false
---

# Talkyard

Use this note for the current NEXUS public discussion layer.

## Purpose In NEXUS

Talkyard currently provides:

- public comments attached to published Markdown-first pages
- human discussion around canonical static content
- a later API surface for ingesting discussion back into NEXUS as separate LOGOS intake

## Official Docs To Check First

- [Embedded comments category targeting](https://forum.talkyard.io/-671/option-to-change-the-category-that-embedded-comments-go-to)
- [Embedded-comments-only / forum-shape setup](https://forum.talkyard.io/-502/how-to-run-a-talkyard-for-embedded-comments-only-disable-forum-features)
- [API auth overview](https://forum.talkyard.io/-382/talkyard-api-authentication)
- [Page and comment creation via API](https://forum.talkyard.io/-800/how-to-create-pages-and-comments-via-the-api)

## Local Usage Here

The current NEXUS docs site uses:

- server URL: `https://talkyard.ivanthegeek.com`
- category ref: `extid:nexus_site_comments`
- public subcategory name: `NEXUS Site Comments`
- discussion placement: bottom of the page, just above the footer
- discussion identity: stable explicit ID derived from the Markdown source path unless overridden by `discussion_id`

## Important Conventions

- Markdown in Git is the canonical public statement; Talkyard is the discussion layer around it
- comments are enabled by default across the NEXUS docs site
- use `comments: false` in YAML front matter to opt a page out
- use `discussion_id: ...` in YAML front matter only when a page needs an explicit stable override
- keep NEXUS comments in their own public stream instead of mixing them into unrelated categories
- future sibling sites such as CheddarBooks/LaundryLog should use sibling categories with their own external IDs

## Local Gotchas

- `mkdocs` is only installed in the repo-local `.venv` on this machine right now, so local preview and build checks should activate that environment first
- GitHub Pages and local preview both need to be allowlisted in Talkyard admin for embeds to appear
- API secrets, if used later, should live outside the repo under `/home/ivan/.config/nexus-secrets/`

## Related

- [`../../public-content-publishing-and-talkyard-comments.md`](../../public-content-publishing-and-talkyard-comments.md)
- [`../../decisions/0016-markdown-first-public-content-with-talkyard-discussion.md`](../../decisions/0016-markdown-first-public-content-with-talkyard-discussion.md)
- [`../../how-to/publish-docs-site.md`](../../how-to/publish-docs-site.md)
