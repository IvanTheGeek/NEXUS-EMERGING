namespace Nexus.Tests

open System.IO
open Expecto
open Nexus.Logos

[<RequireQualifiedAccess>]
module BlogImportTests =
    let tests =
        testList
            "blog import"
            [ testCase "LOGOS blog import writes public-safe notes from markdown front matter" (fun () ->
                  TestHelpers.withTempDirectory "nexus-logos-blog-import" (fun tempRoot ->
                      let docsRoot = Path.Combine(tempRoot, "docs")
                      let repoRoot = Path.Combine(tempRoot, "blog-repo")
                      TestHelpers.copyFixtureDirectory "blog/hashnode-repo" repoRoot

                      let result =
                          LogosBlogImports.importRepo
                              { DocsRoot = docsRoot
                                RepoRoot = repoRoot
                                SourceBaseUri = "https://blog.ivanrainbolt.com"
                                SourceInstanceId = None
                                ExtraTags = [ "public-writing" ] }

                      match result with
                      | Error error ->
                          failtestf "Expected blog import to succeed. %s" error
                      | Ok report ->
                          let loftyGoalsPath =
                              Path.Combine(docsRoot, "logos-intake", "public-safe", "blog-ivanrainbolt-com-lofty-goals.md")

                          let startingFromZeroPath =
                              Path.Combine(docsRoot, "logos-intake", "public-safe", "blog-ivanrainbolt-com-starting-from-zero.md")

                          let loftyGoalsText = File.ReadAllText(loftyGoalsPath)

                          Expect.equal (SourceInstanceId.value report.SourceInstanceId) "blog-ivanrainbolt-com" "Expected the source instance to derive from the blog host."
                          Expect.equal report.ImportedPosts.Length 2 "Expected the fixture blog posts to be imported."
                          Expect.equal report.SkippedPosts.Length 0 "Did not expect skipped files in the fixture repo."
                          Expect.isTrue (File.Exists(loftyGoalsPath)) "Expected the lofty-goals note to exist."
                          Expect.isTrue (File.Exists(startingFromZeroPath)) "Expected the starting-from-zero note to exist."
                          Expect.stringContains loftyGoalsText "source_system = \"blog\"" "Expected the blog source system."
                          Expect.stringContains loftyGoalsText "source_instance = \"blog-ivanrainbolt-com\"" "Expected the derived source instance."
                          Expect.stringContains loftyGoalsText "access_context = \"owner\"" "Expected the owner access context."
                          Expect.stringContains loftyGoalsText "acquisition_kind = \"git-sync\"" "Expected the git-sync acquisition kind."
                          Expect.stringContains loftyGoalsText "intake_channel = \"published-article\"" "Expected the published-article intake channel."
                          Expect.stringContains loftyGoalsText "signal_kind = \"article\"" "Expected the article signal kind."
                          Expect.stringContains loftyGoalsText "entry_pool = \"public-safe\"" "Expected public-safe entry."
                          Expect.stringContains loftyGoalsText "rights_policy = \"owner-controlled\"" "Expected owner-controlled rights."
                          Expect.stringContains loftyGoalsText "sensitivity = \"public\"" "Expected public sensitivity."
                          Expect.stringContains loftyGoalsText "sharing_scope = \"public\"" "Expected public sharing scope."
                          Expect.stringContains loftyGoalsText "sanitization_status = \"approved-for-sharing\"" "Expected approved-for-sharing status."
                          Expect.stringContains loftyGoalsText "source-uri:https://blog.ivanrainbolt.com/lofty-goals" "Expected the public source URI locator."
                          Expect.stringContains loftyGoalsText "I hate QuickBooks" "Expected the first body paragraph as the summary."
                          Expect.stringContains loftyGoalsText "public-writing" "Expected extra tags to be applied to imported notes."))

              testCase "CLI import-logos-blog-repo imports the fixture repo" (fun () ->
                  TestHelpers.withTempDirectory "nexus-logos-blog-cli" (fun tempRoot ->
                      let docsRoot = Path.Combine(tempRoot, "docs")
                      let repoRoot = Path.Combine(tempRoot, "blog-repo")
                      TestHelpers.copyFixtureDirectory "blog/hashnode-repo" repoRoot

                      let result =
                          TestHelpers.runCli
                              [ "import-logos-blog-repo"
                                "--docs-root"
                                docsRoot
                                "--repo-root"
                                repoRoot
                                "--source-base-uri"
                                "https://blog.ivanrainbolt.com"
                                "--tag"
                                "public-writing" ]

                      let loftyGoalsPath =
                          Path.Combine(docsRoot, "logos-intake", "public-safe", "blog-ivanrainbolt-com-lofty-goals.md")

                      Expect.equal result.ExitCode 0 "Expected CLI blog import to succeed."
                      Expect.equal result.StandardError "" "Did not expect stderr from the CLI blog import."
                      Expect.isTrue (File.Exists(loftyGoalsPath)) "Expected the CLI blog import to create the lofty-goals note."
                      Expect.stringContains result.StandardOutput "LOGOS blog repo imported." "Expected the CLI summary header."
                      Expect.stringContains result.StandardOutput "Source instance: blog-ivanrainbolt-com" "Expected the derived source instance in the CLI output."
                      Expect.stringContains result.StandardOutput "Source system: blog" "Expected the blog source system in the CLI output."
                      Expect.stringContains result.StandardOutput "Intake channel: published-article" "Expected the published-article intake channel in the CLI output."
                      Expect.stringContains result.StandardOutput "Signal kind: article" "Expected the article signal kind in the CLI output."
                      Expect.stringContains result.StandardOutput "Acquisition kind: git-sync" "Expected the git-sync acquisition kind in the CLI output."
                      Expect.stringContains result.StandardOutput "Imported posts: 2" "Expected the imported post count in the CLI output.")) ]
