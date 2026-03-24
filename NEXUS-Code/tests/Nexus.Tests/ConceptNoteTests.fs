namespace Nexus.Tests

open System.IO
open Expecto
open Nexus.Curation
open Nexus.Domain
open Nexus.EventStore
open Nexus.Importers

[<RequireQualifiedAccess>]
module ConceptNoteTests =
    let private buildConversationProjectionStore tempRoot =
        let objectsRoot = Path.Combine(tempRoot, "objects")
        let eventStoreRoot = Path.Combine(tempRoot, "event-store")
        let zipPath = Path.Combine(tempRoot, "claude-fixture.zip")

        TestHelpers.createZipFromFixture "provider-export/claude" zipPath

        let _ =
            ImportWorkflow.run
                { Provider = Claude
                  SourceZipPath = zipPath
                  Window = Some Full
                  ObjectsRoot = objectsRoot
                  EventStoreRoot = eventStoreRoot }

        let projectionPaths = ConversationProjections.rebuild eventStoreRoot
        let conversationId = projectionPaths.Head |> Path.GetFileNameWithoutExtension
        eventStoreRoot, conversationId

    let tests =
        testList
            "concept notes"
            [ testCase "Concept note seed captures conversation provenance and excerpts" (fun () ->
                  TestHelpers.withTempDirectory "nexus-concept-note" (fun tempRoot ->
                      let eventStoreRoot, conversationId = buildConversationProjectionStore tempRoot
                      let docsRoot = Path.Combine(tempRoot, "docs")

                      let result =
                          ConceptNotes.create
                              { DocsRoot = docsRoot
                                EventStoreRoot = eventStoreRoot
                                Slug = "FnHCI"
                                Title = "FnHCI"
                                Domains = [ "interaction-design" ]
                                Tags = [ "seed" ]
                                SourceConversationIds = [ conversationId ] }

                      match result with
                      | Error error ->
                          failtestf "Expected concept note creation to succeed. %s" error
                      | Ok note ->
                          let text = File.ReadAllText(note.OutputPath)

                          Expect.isTrue (File.Exists(note.OutputPath)) "Expected the concept note file to exist."
                          Expect.equal note.NormalizedSlug "fnhci" "Expected the slug to normalize to lowercase kebab-case."
                          Expect.stringContains text "note_kind = \"concept_seed\"" "Expected TOML front matter for the note kind."
                          Expect.stringContains text "# FnHCI" "Expected the concept note title heading."
                          Expect.stringContains text "Mermaid sequence diagram for chat" "Expected the source conversation title."
                          Expect.stringContains text conversationId "Expected the canonical conversation ID in the note."
                          Expect.stringContains text "export-graphviz-dot --conversation-id" "Expected a graph slice command for the source conversation."
                          Expect.stringContains text "can you make a mermaid sequence diagram" "Expected message excerpts from the projection."))

              testCase "Concept note seed refuses to overwrite an existing note" (fun () ->
                  TestHelpers.withTempDirectory "nexus-concept-note-duplicate" (fun tempRoot ->
                      let eventStoreRoot, conversationId = buildConversationProjectionStore tempRoot
                      let docsRoot = Path.Combine(tempRoot, "docs")

                      let request =
                          { DocsRoot = docsRoot
                            EventStoreRoot = eventStoreRoot
                            Slug = "fnhci"
                            Title = "FnHCI"
                            Domains = []
                            Tags = []
                            SourceConversationIds = [ conversationId ] }

                      let firstResult = ConceptNotes.create request
                      let secondResult = ConceptNotes.create request

                      match firstResult with
                      | Ok _ -> ()
                      | Error error ->
                          failtestf "Expected the first concept note creation to succeed. %s" error

                      match secondResult with
                      | Ok _ -> failtest "Expected duplicate concept note creation to fail."
                      | Error error ->
                          Expect.stringContains error "Concept note already exists" "Expected a clear duplicate-note error.")) ]
