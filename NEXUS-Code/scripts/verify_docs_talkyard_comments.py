#!/usr/bin/env python3

from pathlib import Path
import sys


REPO_ROOT = Path(__file__).resolve().parents[2]
SITE_ROOT = REPO_ROOT / "site"


def read_html(relative_path: str) -> str:
    path = SITE_ROOT / relative_path
    if not path.exists():
        raise SystemExit(f"Missing built page: {path}")
    return path.read_text(encoding="utf-8")


def expect_contains(text: str, needle: str, context: str) -> None:
    if needle not in text:
        raise SystemExit(f"Expected {context} to contain: {needle}")


def expect_not_contains(text: str, needle: str, context: str) -> None:
    if needle in text:
        raise SystemExit(f"Expected {context} not to contain: {needle}")


def main() -> int:
    if not SITE_ROOT.exists():
        raise SystemExit(f"Missing site output directory: {SITE_ROOT}")

    home = read_html("index.html")
    policy = read_html("public-content-publishing-and-talkyard-comments/index.html")
    package_note = read_html("reference/packages/talkyard/index.html")

    expect_contains(home, "assets/javascripts/talkyard-comments.js", "home page")
    expect_contains(home, "nexus-discussion-block", "home page")
    expect_contains(home, "data-talkyard-category=\"extid:nexus_site_comments\"", "home page")
    expect_contains(home, "data-talkyard-namespace=\"nexus-emerging\"", "home page")
    expect_contains(home, "data-source-path=\"index.md\"", "home page")
    expect_contains(home, "Discussion</h2>", "home page")

    expect_contains(policy, "data-discussion-id=\"nexus-emerging:talkyard-comments-policy\"", "policy page")
    expect_contains(policy, "talkyard.ivanthegeek.com", "policy page")

    expect_not_contains(package_note, "nexus-discussion-block", "Talkyard package note")
    expect_not_contains(package_note, "talkyard-comments.min.js", "Talkyard package note")

    full_site = "\n".join(
        path.read_text(encoding="utf-8")
        for path in SITE_ROOT.rglob("*.html")
    )

    for forbidden in [
        "api-secret",
        "api_secret",
        "apiSecret",
        "Authorization:",
    ]:
        expect_not_contains(full_site, forbidden, "generated site")

    print("Talkyard docs-site verification passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
