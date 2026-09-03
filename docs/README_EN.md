<div align="center">

# 🎙️ UltraDictate

**Lightning-fast, 100% on-device and private speech-to-text dictation for macOS and Windows.**

[![Release](https://img.shields.io/github/v/release/m0rvey/ultradictate?style=flat-square&color=2ea44f&label=Release)](https://github.com/m0rvey/ultradictate/releases/latest)
[![CI](https://img.shields.io/github/actions/workflow/status/m0rvey/ultradictate/build.yml?branch=main&style=flat-square&label=CI)](https://github.com/m0rvey/ultradictate/actions/workflows/build.yml)
[![macOS](https://img.shields.io/badge/macOS-Apple%20Silicon%20(M1--M4)-000000?style=flat-square&logo=apple&logoColor=white)](https://github.com/m0rvey/ultradictate/releases/download/v1.0.0/UltraDictate-macOS-arm64.zip)
[![Windows](https://img.shields.io/badge/Windows-10%2F11%20(x64)-0078D6?style=flat-square&logo=windows&logoColor=white)](https://github.com/m0rvey/ultradictate/releases/download/v1.0.0/UltraDictate-Windows-x64.zip)
[![Swift](https://img.shields.io/badge/Swift-6.0-F05138?style=flat-square&logo=swift&logoColor=white)](https://swift.org/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![DirectML](https://img.shields.io/badge/DirectML-Hardware%20Accelerated-0078D4?style=flat-square)](../windows/)
[![CoreML](https://img.shields.io/badge/CoreML-Apple%20Neural%20Engine-FF6F00?style=flat-square&logo=apple&logoColor=white)](https://developer.apple.com/documentation/coreml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](../LICENSE)

[Features](#-key-features) • [Download Releases](https://github.com/m0rvey/ultradictate/releases/tag/v1.0.0) • [Installation (macOS)](#-installation-macos) • [Installation (Windows)](#-installation-windows) • [Hotkeys](#-hotkeys) • [Building](#-building-from-source) • [Русская версия](../README.md)

</div>

---

## 📌 Overview

**UltraDictate** is a native cross-platform speech-to-text dictation application designed for speed, total privacy, and instant insertion of spoken text into any active app.

- **macOS:** Native Swift/SwiftUI application utilizing Apple Neural Engine (ANE) via CoreML for ultra-low latency (< 250 ms).
- **Windows:** Native .NET 8 / C# application powered by DirectML and ONNX Runtime running on NVIDIA RTX, AMD Radeon, Intel Arc/NPU, and multi-core CPU.

---

## ✨ Key Features

- 🔒 **100% On-Device & Private:** Audio stream never leaves your device. No telemetry, no tracking, zero third-party analytics.
- ⚡ **Instant Push-to-Talk:** Press and hold your preferred hotkey, speak naturally, and text is instantly inserted into any active editor or app.
- 🧠 **Local AI Cleanup:** Optional post-processing with local **Ollama** (`http://localhost:11434/v1`) or LM Studio without needing any external API keys.
- 🗣️ **Voice Commands & Punctuation:** Native voice commands for "new line", "new paragraph", and automated punctuation formatting.
- 🎨 **Glassmorphic Floating HUD:** Minimalist dark mode HUD displaying audio waveform levels and recording status.
- 🔋 **Optimized Resource Consumption:** Zero background CPU usage while idle.

---

## 🚀 Installation (macOS)

### Requirements
- Mac with **Apple Silicon** (M1/M2/M3/M4 or A18 Pro).
- **macOS 14 (Sonoma)** or later.

```bash
curl -fsSL https://raw.githubusercontent.com/m0rvey/ultradictate/v1.0.0/install.sh | /usr/bin/arch -arm64 /bin/bash
```

1. Launch UltraDictate and grant permissions: **Microphone**, **Accessibility**, and **Input Monitoring**.
2. Press and hold **Right Command** to dictate. Release to paste.

---

## 🪟 Installation (Windows)

### Requirements
- 64-bit **Windows 10 / 11**.
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0).

### Run
1. Download `UltraDictate-Windows-x64.zip` from [Releases](https://github.com/m0rvey/ultradictate/releases).
2. Extract the archive and launch `UltraDictate.exe`.
3. Press and hold **Right Control** to dictate.

---

## ⌨️ Hotkeys

| Platform | Default Hotkey | Action |
| :--- | :--- | :--- |
| **macOS** | `Right ⌘ (Right Command)` | Push-to-Talk: Hold to record, release to insert |
| **Windows** | `Right Ctrl (Right Control)` | Push-to-Talk: Hold to record, release to insert |
| **All Platforms** | `Escape` | Cancel recording without inserting text |

---

## 🛠️ Building from Source

### macOS
```bash
git clone https://github.com/m0rvey/ultradictate.git
cd ultradictate
./scripts/build-app.sh ./dist/UltraDictate.app
```

### Windows
```cmd
git clone https://github.com/m0rvey/ultradictate.git
cd ultradictate\windows
build.bat
```

---

## 📜 Attribution & License

- Author: **m0rvey** ([GitHub](https://github.com/m0rvey))
- Derived from SuperDictate and Parakey (MIT License).
- Distributed under the [MIT License](../LICENSE).
