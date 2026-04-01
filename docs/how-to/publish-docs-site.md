# Publish The Docs Site

This guide explains how the MkDocs docs-site projection publishes to GitHub Pages.

The repository now includes a GitHub Actions workflow at `.github/workflows/docs-site.yml` which builds the current `mkdocs.yml` configuration and deploys the generated `site/` directory to GitHub Pages.

## What Is Already Wired

- pushes to `main` that touch docs-site files trigger a Pages deploy
- the workflow can also be run manually with `workflow_dispatch`
- the build uses `requirements-docs.txt`
- the deployed artifact is the generated `site/` directory from `mkdocs build`

## One-Time GitHub Setup

In the GitHub repository:

1. Open `Settings`.
2. Open `Pages`.
3. Under `Build and deployment`, set `Source` to `GitHub Actions`.

After that, the checked-in workflow can publish the site.

## First Publish

Once the workflow file is on `main`:

1. Push the branch to GitHub.
2. Open the repository `Actions` tab.
3. Run or inspect the `Deploy docs site` workflow.
4. After the deploy job finishes, GitHub Pages will expose the site URL in the workflow and in `Settings` -> `Pages`.

## Local Verification Before Pushing

```bash
source .venv/bin/activate
mkdocs build
```

If you want to inspect the site in a browser before pushing:

```bash
source .venv/bin/activate
mkdocs serve --dev-addr 127.0.0.1:8000
```

## Notes

- the workflow does not require committing generated HTML into the repo
- the docs source remains in the existing `docs/` tree
- keep links to published docs pages relative inside `docs/`
- when a docs page needs to link to repo-root files or `NEXUS-Code/`, use explicit GitHub URLs instead of relative filesystem paths
- when a docs page links to event-store artifacts, prefer explicit URLs in `https://github.com/IvanTheGeek/NEXUS-EventStore/...`
- for historical `NEXUS-EventStore` citations, prefer commit-pinned GitHub blob URLs so source references stay stable over time
- if a later custom domain is desired, configure that separately in GitHub Pages once the base publish flow is stable
