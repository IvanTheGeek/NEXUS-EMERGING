# Report LOGOS Catalog

Use this when you want to inspect the currently allowlisted LOGOS source vocabulary before seeding or modeling intake.

It reports:

- recognized source systems
- recognized access contexts
- recognized acquisition kinds
- recognized intake channels
- recognized signal kinds
- recognized rights policies
- recognized sensitivities
- recognized sharing scopes
- recognized sanitization statuses
- recognized retention classes

This keeps early LOGOS work explicit and deterministic rather than open-ended.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- report-logos-catalog
```

## What It Is For

Use this before:

- creating a LOGOS intake note
- choosing source-instance, access-context, acquisition-kind, or rights-policy values
- assigning or checking intake handling policy
- deciding how to classify a new source
- checking whether a needed source kind is already modeled explicitly

Current examples include both:

- generic categories like `forum`
- concrete current implementations like `talkyard`

## Notes

- This is an allowlist report, not a dynamic taxonomy browser.
- If a needed source system or signal kind is not listed, that is a modeling decision to add explicitly, not something to improvise ad hoc.
