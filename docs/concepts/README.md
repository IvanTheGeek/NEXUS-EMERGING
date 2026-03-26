# Concept Notes

This folder is for curated concept notes harvested from imported history and later refined into durable project memory.

Use concept notes when:

- an idea keeps recurring across conversations
- a domain seed like `FnHCI` needs a stable home in the repo
- humans and AI collaborators need a shared summary with provenance back to source conversations

Concept notes are:

- human-editable Markdown
- seeded from canonical conversation provenance when possible
- allowed to evolve as understanding deepens

Concept notes are not:

- canonical event history
- the final ontology
- projections pretending to be source truth

The current seed workflow writes notes under this folder with:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- create-concept-note --slug fnhci --title FnHCI --conversation-id <uuid>
```

Related guide:

- [`docs/how-to/create-concept-note.md`](../how-to/create-concept-note.md)
