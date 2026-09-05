<div align="center">

# 🎙️ UltraDictate

**Lightning-fast, 100% on-device and private speech-to-text dictation for macOS and Windows.**

[![Release](https://img.shields.io/github/v/release/m0rvey/ultradictate?style=flat-square&color=2ea44f&label=Release)](https://github.com/m0rvey/ultradictate/releases/latest)
[![CI](https://img.shields.io/github/actions/workflow/status/m0rvey/ultradictate/build.yml?branch=main&style=flat-square&label=CI)](https://github.com/m0rvey/ultradictate/actions/workflows/build.yml)
[![macOS](https://img.shields.io/badge/macOS-Apple%20Silicon%20(M1--M5%2C%20A18%20Pro)-000000?style=flat-square&logo=apple&logoColor=white)](https://github.com/m0rvey/ultradictate/releases/latest)
[![Windows](https://img.shields.io/badge/Windows-10%2F11%20(x64)-0078D6?style=flat-square&logo=windows&logoColor=white)](https://github.com/m0rvey/ultradictate/releases/latest)
[![Swift](https://img.shields.io/badge/Swift-6.0-F05138?style=flat-square&logo=swift&logoColor=white)](https://swift.org/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![DirectML](https://img.shields.io/badge/DirectML-Hardware%20Accelerated-0078D4?style=flat-square)](../windows/)
[![CoreML](https://img.shields.io/badge/CoreML-Apple%20Neural%20Engine-FF6F00?style=flat-square&logo=apple&logoColor=white)](https://developer.apple.com/documentation/coreml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](../LICENSE)

[Features](#-key-features) • [Speech Models](#-whisper-model-profiles-small-vs-base) • [Privacy & Security](#-privacy--zero-telemetry-100-offline) • [Installation](#-installation) • [Hotkeys](#-hotkeys) • [Building](#-building-from-source) • [Русская версия](../README.md)

</div>

---

## 📌 Overview

**UltraDictate** is a native cross-platform speech-to-text dictation application designed for speed, total privacy, and instant insertion of spoken text into any active app (browsers, Telegram, Discord, Word, code editors, and terminals).

- **macOS:** Native Swift 6 / SwiftUI application utilizing Apple Neural Engine (ANE) via CoreML for ultra-low latency (< 250 ms) on M1–M5 and A18 Pro.
- **Windows:** Native .NET 8 standalone application powered by **Whisper.net**, DirectML GPU acceleration, and AVX2/AVX512 CPU multi-threading.

---

## 🔒 Privacy & Zero Telemetry (100% Offline)

1. **Zero Audio Transmission:** Microphone capture (WASAPI on Windows, CoreAudio on macOS) is streamed directly into an in-memory buffer. Audio is never stored to disk or transmitted to any server.
2. **Local Neural Weights:** All Whisper models are stored locally:
   - **Windows:** `%APPDATA%\UltraDictate\models\`
   - **macOS:** `~/Library/Application Support/UltraDictate/`
3. **No Telemetry or Tracking:** No user tracking, analytics, or keylogging.
4. **Local AI Cleanup:** Optional grammar and punctuation post-processing can connect to local **Ollama** (`localhost:11434`) or LM Studio without external cloud API calls.

---

## 🧠 Whisper Model Profiles: Small vs Base

When you launch UltraDictate on Windows, the First-Run Setup Wizard lets you pick your preferred model:

| Parameter | 🌟 Whisper Small (Recommended) | ⚡ Whisper Base (Lightweight) |
| :--- | :--- | :--- |
| **Disk Footprint** | ~465 MB (`ggml-small.bin`) | ~140 MB (`ggml-base.bin`) |
| **Model Parameters** | 244 Million | 74 Million |
| **Accuracy** | **Mac-grade accuracy** with complex Russian & English vocabulary | Standard conversational speech |
| **Punctuation & Grammar** | Pristine case detection, terms, and punctuation | Basic capitalization |
| **Latency** | ~0.8 – 1.4s per sentence | < 0.4s per sentence |
| **RAM Footprint** | ~1.0 GB | ~400 MB |
| **Best For** | Professional daily workflow, programming, writing | Slower laptops, quick short notes |

---

## 📦 Installation

### 🪟 Windows 10 / 11 (64-bit)
1. Download `UltraDictate-Windows-x64.zip` from [GitHub Releases](https://github.com/m0rvey/ultradictate/releases/latest).
2. Extract the archive and launch **`UltraDictate.exe`** (standalone single-file bundle; no .NET 8 installation required).
3. Follow the first-run welcome wizard to select your model (**Whisper Small** recommended).
4. Press and hold **Right Control** to dictate.

### 🍏 macOS (Apple Silicon M1–M5, A18 Pro)
```bash
curl -fsSL https://raw.githubusercontent.com/m0rvey/ultradictate/main/install.sh | /usr/bin/arch -arm64 /bin/bash
```
Or download `UltraDictate-macOS-arm64.zip` from [Releases](https://github.com/m0rvey/ultradictate/releases/latest), drag `UltraDictate.app` into `/Applications`, grant Microphone, Accessibility, and Input Monitoring permissions, and hold **Right Command** to dictate.

---

## ⌨️ Hotkeys

| Platform | Default Hotkey | Action |
| :--- | :--- | :--- |
| **Windows** | `Right Ctrl (Right Control)` | Hold to record, release to insert text |
| **macOS** | `Right ⌘ (Right Command)` | Hold to record, release to insert text |
| **All Platforms** | `Escape` | Cancel recording without inserting |

---

## 🛠️ Building from Source

### Windows
```cmd
git clone https://github.com/m0rvey/ultradictate.git
cd ultradictate
windows\build.bat
```

### macOS
```bash
git clone https://github.com/m0rvey/ultradictate.git
cd ultradictate
./scripts/build-app.sh ./dist/UltraDictate.app
```

---

## 📜 Attribution & License

- Author: **m0rvey** ([GitHub](https://github.com/m0rvey))
- Distributed under the [MIT License](../LICENSE).
