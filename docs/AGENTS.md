# UltraDictate Development Invariants

## One Installed Application

- The primary runnable installed bundle is `/Applications/UltraDictate.app`.
- Always use bundle identifier `com.m0rvey.ultradictate` (or local agent `com.local.ultradictate.agent`).
- Local installation must atomically replace that bundle and restart only
  `com.local.ultradictate.agent`.
- Never launch a copied, test, smoke, or temporary `.app` bundle. Exercise
  diagnostics through the command-line self-tests instead.
- Never change the signing identity during an installation.
- Never call `tccutil reset` automatically. Permission removal is an explicit
  user action.
- Never open more than one macOS privacy pane for a permission request.
