# 🪟 UltraDictate for Windows

> High-performance, offline-first push-to-talk speech dictation for Windows powered by DirectML and ONNX Runtime.

---

## ⚡ Features

- **Push-to-Talk Hotkey**: Hold `Right Control` (customizable) and speak.
- **DirectML Hardware Acceleration**: Works across NVIDIA GeForce/RTX, AMD Radeon, Intel Arc/Iris Xe/NPU, and multi-threaded CPU fallback.
- **Low-Latency WASAPI Capture**: Captures 16,000 Hz float mono audio directly from your default microphone.
- **Synthesized `Ctrl+V` Paste Transaction**: Instant clipboard insertion into any focused app with automated clipboard preservation.
- **Voice Commands**: Automatically transforms verbal commands (e.g., "новая строка" -> `\n`, "новый абзац" -> `\n\n`).
- **AI Cleanup**: Optional integration with local Ollama (`http://localhost:11434/v1`) or cloud models (Groq, OpenAI) for grammar fixes.
- **Zero Telemetry**: No third-party analytics or external network calls without explicit AI cleanup configuration.

---

## 🛠️ Build & Installation

### Requirements
- Windows 10/11 (64-bit)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Quick Build
```cmd
.\build.bat
```
This produces a self-contained, single-file executable: `windows\dist\UltraDictate.exe`.

### Install
```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```
