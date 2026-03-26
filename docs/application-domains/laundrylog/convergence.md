# LaundryLog Convergence

This note captures the current convergence and sync pressure for LaundryLog.

## Convergence Requirements

LaundryLog must leave room for later convergence across devices or nodes.

The exact mechanism is not settled yet.

Current direction:

- local-first capture
- later sync or convergence
- Git is a plausible convergence seam to explore, but not yet a locked requirement

## Why Convergence Matters Early

LaundryLog is likely to be used:

- on a phone while away from home
- on more than one device over time
- sometimes offline and later reconnected

That means the app should not paint itself into a corner where one-device-only assumptions are baked too deeply into the early model.

## Deferred Or Open Convergence Decisions

Not yet settled:

- exact sync/convergence mechanism
- whether Git becomes the actual seam or only a learning/prototyping seam
- how shared location or community data should converge
