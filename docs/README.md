<div align="center">

# 🎙️ UltraDictate

**Сверхбыстрая, полностью локальная и приватная система голосовой диктовки для macOS и Windows.**

[![macOS](https://img.shields.io/badge/macOS-Apple%20Silicon%20(M1--M5%2C%20A18%20Pro)-000000?style=flat-square&logo=apple&logoColor=white)](https://www.apple.com/macos/)
[![Windows](https://img.shields.io/badge/Windows-10%2F11%20(x64)-0078D6?style=flat-square&logo=windows&logoColor=white)](windows/)
[![Swift](https://img.shields.io/badge/Swift-6.0-F05138?style=flat-square&logo=swift&logoColor=white)](https://swift.org/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![DirectML](https://img.shields.io/badge/DirectML-Hardware%20Accelerated-0078D4?style=flat-square)](windows/)
[![CoreML](https://img.shields.io/badge/CoreML-Neural%20Engine-FF6F00?style=flat-square&logo=apple&logoColor=white)](https://developer.apple.com/documentation/coreml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

[Возможности](#-ключевые-возможности) • [Установка для macOS](#-быстрая-установка-macos) • [Установка для Windows](#-установка-windows) • [Горячие клавиши](#-горячие-клавиши) • [Архитектура](#-архитектура-проекта) • [English Version](docs/README_EN.md)

</div>

---

## 📌 Обзор

**UltraDictate** — кроссплатформенная система диктовки нового поколения, разработанная для мгновенного набора текста голосом в любых приложениях без задержек и без отправки аудиоданных в облако.

- **macOS:** Нативное Swift/SwiftUI приложение, использующее Apple Neural Engine (ANE) и CoreML для ультранизкой задержки (< 250 мс).
- **Windows:** Нативное .NET 8 приложение с аппаратным ускорением DirectML и ONNX Runtime, работающее на GPU NVIDIA RTX, AMD Radeon, Intel Arc/NPU и многопоточном CPU.

---

## ✨ Ключевые возможности

- 🔒 **100% On-Device и Приватно:** Распознавание выполняется локально на вашем компьютере. Нулевая несанкционированная телеметрия.
- ⚡ **Мгновенный Push-to-Talk:** Зажмите горячую клавишу, продиктуйте мысль — и готовый текст мгновенно появится в активном окне (браузер, мессенджер, IDE, Word).
- 🧠 **Локальный AI Cleanup:** Опциональная постобработка через локальные модели **Ollama** (`http://localhost:11434/v1`) или облачные провайдеры (Groq, OpenAI) для исправления пунктуации и опечаток.
- 🗣️ **Голосовые команды и пунктуация:** Поддержка команд «новая строка» (`\n`), «новый абзац» (`\n\n`), автоматическая расстановка знаков препинания.
- 🎨 **Премиальный Dark Mode & HUD:** Плавающая капсула записи со стекломорфизмом и динамической визуализацией аудиоволны.
- 📚 **Словарь и автозамены:** Пользовательские правила для замены профессиональных терминов, акронимов и сниппетов.

---

## 🚀 Быстрая установка (macOS)

### Требования
- Mac с процессором **Apple Silicon** (M1-M5, A18 Pro).
- **macOS 14 (Sonoma)** или новее.

```bash
curl -fsSL https://raw.githubusercontent.com/m0rvey/ultradictate/v1.0.0/install.sh | /usr/bin/arch -arm64 /bin/bash
```

1. Запустите UltraDictate и предоставьте системные разрешения: **Микрофон**, **Универсальный доступ** и **Мониторинг ввода**.
2. Зажмите **Правый Command** и говорите. Отпустите — текст сразу вставится.

---

## 🪟 Установка (Windows)

### Требования
- 64-битная **Windows 10 / 11**.
- Установленный [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0).

### Запуск
1. Скачайте `UltraDictate-Windows-x64.zip` из [Releases](https://github.com/m0rvey/ultradictate/releases).
2. Распакуйте архив и запустите `UltraDictate.exe`.
3. Зажмите **Правый Control** и диктуйте текст.

Для сборки из исходников запустите в папке проекта:
```cmd
windows\build.bat
```

---

## ⌨️ Горячие клавиши

| Платформа | Клавиша по умолчанию | Поведение |
| :--- | :--- | :--- |
| **macOS** | `Правый ⌘ (Right Command)` | Удерживайте для записи, отпустите для вставки |
| **Windows** | `Правый Ctrl (Right Control)` | Удерживайте для записи, отпустите для вставки |
| **Все ОС** | `Escape` | Мгновенная отмена диктовки без вставки |

---

## 🏗️ Архитектура проекта

```
ultradictate/
├── swift/                     # Нативный движок для macOS (Apple Silicon)
│   ├── Package.swift          # Модульная сборка SwiftPM
│   ├── FluidAudio/            # Локальная библиотека CoreML/ANE инференса
│   └── Sources/UltraDictate/
│       ├── Audio/             # Захват и конвертация звука (AVAudioEngine)
│       ├── Core/              # Глобальные хоткеи (CGEventTap), настройки, сервис
│       ├── Speech/            # Модели Parakeet/Whisper, верификация кэша (<0.2 мс)
│       ├── Text/              # Очистка текста, автозамены, Ollama/AI клиент
│       └── UI/                # Менюбар, плавающий HUD 120 Гц, статистика
│
├── windows/                   # Нативный движок для Windows
│   ├── UltraDictate.Windows/  # Проект .NET 8 / C#
│   │   ├── Core/              # WASAPI аудио, DirectML ONNX, SendInput вставка
│   │   └── UI/                # Трей-меню Windows и стеклянный HUD
│   ├── build.bat              # Скрипт сборки автономного .exe
│   └── install.ps1            # Скрипт установки и создания ярлыков
│
├── scripts/                   # Утилиты сборки, проверки и тестирования
│   ├── build-app.sh           # Сборка и подпись UltraDictate.app
│   └── check.sh               # Комплексная проверка целостности и версий
│
└── .github/workflows/         # CI/CD релизный пайплайн
    ├── build.yml              # Проверка сборки на macOS и Windows
    └── release.yml            # Автоматическая сборка мультиплатформенных релизов
```

---

## 📜 Лицензия и Авторство

- Автор: **m0rvey** ([GitHub](https://github.com/m0rvey))
- Основано на наработках SuperDictate и Parakey (MIT License).
- Распространяется под лицензией [MIT](LICENSE).
