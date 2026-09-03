# UltraDictate Privacy Policy

UltraDictate is designed for local dictation.

## Local Processing
- Audio input is processed locally on your machine.
- Dictation audio is not sent to external servers.
- Transcription models run entirely on-device (Apple Neural Engine on macOS, DirectML/GPU on Windows).
- Settings, local logs, and cached statistics are stored locally on your system.

## Network Access
UltraDictate uses the network strictly for:
1. Downloading the speech recognition model on initial setup.
2. Checking for software updates (via GitHub Releases) if enabled.
3. Optional user-configured AI cleanup requests (such as local Ollama on `localhost:11434` or cloud AI provider if an API key is explicitly configured by the user).

No personal information, audio streams, or background telemetry are collected or uploaded.
