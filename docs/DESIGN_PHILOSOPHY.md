# UltraDictate Design Philosophy

## 1. Core Principles

### 1.1 100% Local-First & Zero Telemetry
- Dictation happens completely on-device. Speech audio never touches external servers or third-party cloud infrastructure.
- Zero analytics, telemetry, or user tracking.
- The optional AI text-cleanup feature is strictly opt-in and supports local LLMs (Ollama / LM Studio) or user-configured cloud endpoints.

### 1.2 Latency-First User Experience (< 200 ms)
- Dictation must feel immediate. From the millisecond recording stops to the moment text appears in the active application, total elapsed time should be imperceptible.
- Key optimizations:
  - **Fast Fingerprint Cache**: Eliminates startup verification penalty down to `< 0.2 ms`.
  - **Apple Neural Engine (ANE) & DirectML Acceleration**: CoreML inference on macOS and DirectML GPU inference on Windows.
  - **Pre-warmed Audio Engine**: Lowers audio capture activation latency.

### 1.3 Memory Efficiency
- Strict `@autoreleasepool` boundaries during transcription on macOS.
- Immediate deallocation of intermediate audio sample buffers upon inference completion.
- Ultra-low memory footprint in idle state.

### 1.4 Native Look and Feel
- Rich modern aesthetics with glassmorphism, Dark Mode First (`#0D1117`), and fluid waveform rendering.
- Seamless Push-to-Talk integration across all applications and active text fields.
- True dual-language (RU / EN) native localization across all UI surfaces.

### 1.5 Modular & Resilient Code Architecture
- Code is decomposed into clean, decoupled domain modules with explicit single responsibilities (`Core`, `Audio`, `Speech`, `Text`, `UI`, `Localization`, `Diagnostics`).
- Comprehensive self-testing covers critical user journeys to prevent regressions.
