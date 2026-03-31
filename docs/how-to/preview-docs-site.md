# Preview The Docs Site

This guide explains the current MkDocs proof-of-concept for projecting the repo docs into a browseable HTML site.

The current setup uses the built-in `mkdocs` theme with a custom light-tech stylesheet and a small JavaScript enhancement layer for quality-of-life behaviors such as code-block copy buttons.

## Why This Exists

- it gives the current Markdown docs a simple HTML projection with little tooling
- it keeps the source in the existing `docs/` tree
- it leaves room for later coexistence with `fsdocs` if F# API and literate docs become a stronger concern line

## Current Files

- `mkdocs.yml`
- `requirements-docs.txt`
- `docs/assets/stylesheets/light-matrix.css`
- `docs/assets/javascripts/code-copy.js`

## Local Preview

From the repository root:

```bash
python3 -m venv .venv
source .venv/bin/activate
python3 -m pip install -r requirements-docs.txt
mkdocs serve
```

Then open:

`http://127.0.0.1:8000/`

## Local Build

```bash
source .venv/bin/activate
mkdocs build
```

The generated static site is written to:

`site/`

## Notes

- `mkdocs` was not installed globally on this machine when this proof-of-concept was added, so the repo instructions assume a local virtualenv instead of a machine-global dependency
- a few docs still describe the root repo `README.md` as a stronger source than the docs site projection; this is intentional
- some research notes still contain links that point outside the docs tree and may need a later curation pass before public publishing
