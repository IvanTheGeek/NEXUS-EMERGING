# Import LOGOS Blog Repo

Use this when you have an owner-controlled public Markdown blog repository and want to seed its published posts into durable LOGOS notes.

This is a bridge workflow:

- the source remains the Git repository
- the imported notes become durable public-writing memory inside NEXUS
- the resulting notes land directly in `docs/logos-intake/public-safe/`

This path is intentionally narrow right now.

It is aimed at Markdown repos whose files carry front matter like:

- `title`
- `slug`
- `datePublished`
- `cuid`
- `tags`

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  import-logos-blog-repo \
  --repo-root /tmp/blog.ivanrainbolt.com \
  --source-base-uri https://blog.ivanrainbolt.com
```

## Required Inputs

- `--repo-root <path>`
  root directory of the Markdown repository
- `--source-base-uri <uri>`
  public base URI used to build the source locator for each imported post

## Optional Inputs

- `--source-instance <slug>`
  explicit source-instance override
- `--tag <slug>` repeatable
  extra tags to apply to every imported note
- `--docs-root <path>`
  alternate docs root if you do not want to write into the repository default docs tree

If `--source-instance` is omitted, it is derived from the host in `--source-base-uri`.

For example:

- `https://blog.ivanrainbolt.com` -> `blog-ivanrainbolt-com`

## Resulting LOGOS Classification

Imported blog posts currently enter with:

- `source_system = blog`
- `intake_channel = published-article`
- `signal_kind = article`
- `access_context = owner`
- `acquisition_kind = git-sync`
- `rights_policy = owner-controlled`
- `entry_pool = public-safe`

The current intended use is owner-controlled public writing, not arbitrary third-party blog scraping.

## Output Location

The imported notes are written under:

- `docs/logos-intake/public-safe/`

One Markdown note is created per post.

Files that are missing the expected front matter are skipped rather than aborting the whole import.

## Example

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  import-logos-blog-repo \
  --repo-root /tmp/blog.ivanrainbolt.com \
  --source-base-uri https://blog.ivanrainbolt.com \
  --tag public-writing \
  --tag blog-archive
```

## Why This Exists

This gives NEXUS a clean way to preserve public writing as:

- durable repo memory
- public-safe LOGOS material
- input for later attribution-aware publication, linking, and refinement

It also fits the broader direction where canonical public content can live as Markdown in Git while discussion and feedback can live around it in systems like Talkyard.

## Related

- [`create-logos-intake-note.md`](create-logos-intake-note.md)
- [`report-logos-handling.md`](report-logos-handling.md)
- [`export-logos-public-notes.md`](export-logos-public-notes.md)
- [`../logos-source-model-v0.md`](../logos-source-model-v0.md)
