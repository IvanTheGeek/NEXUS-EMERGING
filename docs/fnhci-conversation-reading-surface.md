# FnHCI Conversation Reading Surface

This note defines the first requirements for a human-readable conversation surface over canonical conversation projection files.

Example motivating artifact:

- [`019d174e-ea99-76b0-8ed6-1b124c5fe938.toml`](https://github.com/IvanTheGeek/NEXUS-EMERGING/blob/d0da2b9d1130c85ede291ede2cf0a7863653fd25/NEXUS-EventStore/projections/conversations/019d174e-ea99-76b0-8ed6-1b124c5fe938.toml)

## Purpose

The canonical conversation projection TOML files are excellent durable records, but they are not a comfortable reading surface for a human trying to follow a conversation.

NEXUS needs a reading surface that can:

- open a projection file
- render it as a chat-style transcript
- preserve provenance and metadata
- let a human follow the thread without reading raw TOML

## Why This Matters

This requirement is important for:

- reviewing imported chat history
- harvesting concepts and discoveries
- understanding project memory in context
- following design and requirement discussions such as LaundryLog, FnHCI, and LOGOS

## Core Requirement

Given a canonical conversation projection TOML file, FnUI must be able to render a human-readable conversation view.

That view should present:

- conversation title
- provider list
- provider conversation identifiers
- message count
- first and last occurrence times
- ordered messages in a chat-like flow

## Message Rendering Requirements

The reading surface should present each message with:

- role
- occurred-at time
- sequence position
- excerpt or body text when available
- artifact reference count when relevant

The message flow should be readable as a conversation first, not as a table dump.

## Provenance Requirements

The reading surface must preserve traceability.

That means the user should be able to move from the reading surface back toward:

- canonical conversation id
- message ids
- projection file location
- related graph scope exports or later graph views

## UX Requirements

The first reading surface should favor:

- readable chat chronology
- easy scanning
- collapsible metadata instead of metadata-first overload
- mobile-friendly rendering where practical
- desktop readability for long conversations

## Non-Goals For The First Pass

The first pass does not need:

- rich markdown re-rendering of every provider feature
- provider-perfect visual imitation
- inline raw event editing
- full search across every projection at once

The first goal is a good reading surface, not a full conversation IDE.

## Relationship To FnUI

This belongs under the FnUI line because it is one of the clearest examples of:

- TOML as durable truth-shaped storage
- a human-facing visual surface over that stored projection
- provenance-preserving GUI design

## Likely First Use

LaundryLog-related history, FnHCI/FnUI history, and other design conversations are good first use cases because they are actively shaping the repo and are already being read by hand from projection files.
