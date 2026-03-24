# Render Graphviz DOT

This command renders an existing DOT file into an actual image output using Graphviz on the local machine.

Use it after `export-graphviz-dot` when you want something directly viewable like SVG or PNG.

## Command

Default render:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  render-graphviz-dot \
  --input NEXUS-EventStore/graph/exports/nexus-graph.dot
```

Explicit engine and format:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  render-graphviz-dot \
  --input NEXUS-EventStore/graph/exports/nexus-graph__provider-codex.dot \
  --engine sfdp \
  --format svg
```

PNG output:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  render-graphviz-dot \
  --input /tmp/nexus-graph.dot \
  --output /tmp/nexus-graph.png \
  --format png
```

## Allowed Values

Engines:

- `dot`
- `sfdp`

Formats:

- `svg`
- `png`

These are intentionally explicit allowlists.

## Defaults

- engine: `dot`
- format: `svg`
- output path: the input DOT path with the selected format extension

## Notes

- This command renders an existing DOT file. It does not generate the DOT source.
- Use `export-graphviz-dot` first if you still need to create the DOT file.
- `sfdp` is often a better layout choice for larger or more organic graph slices.
