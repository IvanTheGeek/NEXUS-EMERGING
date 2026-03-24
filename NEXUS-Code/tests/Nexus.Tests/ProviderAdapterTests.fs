namespace Nexus.Tests

open Expecto
open System.IO
open Nexus.Domain
open Nexus.Importers

[<RequireQualifiedAccess>]
module ProviderAdapterTests =
    let private fixtureLength path =
        FileInfo(path).Length

    let tests =
        testList
            "provider adapters"
            [ testCase "Claude fixture parses messages and artifact references" (fun () ->
                  let fixturePath = TestHelpers.fixturePath "provider-export/claude/conversations.json"

                  let parsedImport =
                      ProviderAdapters.parse Claude (Some Full) "conversations.json" (fixtureLength fixturePath) 1 Set.empty fixturePath

                  Expect.equal parsedImport.Provider Claude "Expected the Claude provider adapter."
                  Expect.equal parsedImport.Conversations.Length 1 "Expected one Claude fixture conversation."

                  let conversation = parsedImport.Conversations.Head
                  Expect.equal conversation.Title (Some "Claude Fixture Conversation") "Expected the Claude conversation title."
                  Expect.equal conversation.Messages.Length 2 "Expected two Claude fixture messages."

                  let firstMessage = conversation.Messages.Head
                  Expect.equal firstMessage.Role Human "Expected the first Claude message to be human-authored."
                  Expect.equal (firstMessage.Segments |> List.map (fun segment -> segment.Text)) [ "Hello from Claude fixture." ] "Expected normalized Claude text."

                  let secondMessage = conversation.Messages[1]
                  Expect.equal secondMessage.Role Assistant "Expected the second Claude message to be assistant-authored."
                  Expect.equal secondMessage.ArtifactReferences.Length 1 "Expected one Claude artifact reference."

                  let artifact = secondMessage.ArtifactReferences.Head
                  Expect.equal artifact.ProviderArtifactId (Some "claude-artifact-1") "Expected the provider artifact ID."
                  Expect.equal artifact.FileName (Some "fixture-note.txt") "Expected the Claude artifact file name."
                  Expect.equal artifact.Disposition PayloadMissing "Expected the artifact to be unresolved in the raw export fixture.")

              testCase "ChatGPT fixture parses ordered messages and model names" (fun () ->
                  let fixturePath = TestHelpers.fixturePath "provider-export/chatgpt/conversations.json"

                  let parsedImport =
                      ProviderAdapters.parse ChatGpt (Some Full) "conversations.json" (fixtureLength fixturePath) 1 Set.empty fixturePath

                  Expect.equal parsedImport.Provider ChatGpt "Expected the ChatGPT provider adapter."
                  Expect.equal parsedImport.Conversations.Length 1 "Expected one ChatGPT fixture conversation."

                  let conversation = parsedImport.Conversations.Head
                  Expect.equal conversation.Title (Some "ChatGPT Fixture Conversation") "Expected the ChatGPT conversation title."
                  Expect.equal conversation.Messages.Length 2 "Expected two ChatGPT fixture messages."
                  Expect.equal (conversation.Messages |> List.map (fun message -> message.Role)) [ Human; Assistant ] "Expected ordered human/assistant roles."
                  Expect.equal (conversation.Messages |> List.choose (fun message -> message.SequenceHint)) [ 1; 2 ] "Expected stable sequence hints."
                  Expect.equal conversation.Messages[1].ModelName (Some "gpt-4.1") "Expected the assistant model slug to be captured."
                  Expect.equal
                      (conversation.Messages[1].Segments |> List.map (fun segment -> segment.Text))
                      [ "ChatGPT fixture reply." ]
                      "Expected normalized ChatGPT assistant text.") ]
