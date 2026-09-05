<div align="center">

# 🎙️ UltraDictate

**Сверхбыстрая, полностью локальная и приватная система голосовой диктовки для macOS и Windows.**

[![Release](https://img.shields.io/github/v/release/m0rvey/ultradictate?style=flat-square&color=2ea44f&label=Release)](https://github.com/m0rvey/ultradictate/releases/latest)
[![macOS](https://img.shields.io/badge/macOS-Apple%20Silicon%20(M1--M5%2C%20A18%20Pro)-000000?style=flat-square&logo=apple&logoColor=white)](https://github.com/m0rvey/ultradictate/releases/latest)
[![Windows](https://img.shields.io/badge/Windows-10%2F11%20(x64)-0078D6?style=flat-square&logo=windows&logoColor=white)](https://github.com/m0rvey/ultradictate/releases/latest)
[![Swift](https://img.shields.io/badge/Swift-6.0-F05138?style=flat-square&logo=swift&logoColor=white)](https://swift.org/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![DirectML](https://img.shields.io/badge/DirectML-Hardware%20Accelerated-0078D4?style=flat-square)](../windows/)
[![CoreML](https://img.shields.io/badge/CoreML-Neural%20Engine-FF6F00?style=flat-square&logo=apple&logoColor=white)](https://developer.apple.com/documentation/coreml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](../LICENSE)

[Возможности](#-ключевые-возможности) • [Модели речи](#-выбор-модели-whisper-small-vs-base) • [Конфиденциальность](#-конфиденциальность-и-безопасность-100-offline) • [Как скачать и установить](#-как-скачать-и-установить) • [Горячие клавиши](#-горячие-клавиши) • [Архитектура](#-архитектура-проекта) • [English Version](README_EN.md)

</div>

---

## 📌 Обзор

**UltraDictate** — автономная система голосового ввода текста (Speech-to-Text) нового поколения, созданная для мгновенного набора текста голосом в любых приложениях: браузерах, Telegram, Discord, Word, Notion, Obsidian, средах разработки (VS Code, JetBrains) и системных окнах.

- **macOS:** Нативное Swift 6 / SwiftUI приложение, использующее нейропроцессоры **Apple Neural Engine (ANE)** и CoreML для ультранизкой задержки (< 250 мс) на процессорах серии M1–M5 и A18 Pro.
- **Windows:** Нативное приложение на C# / .NET 8 (Self-Contained Standalone Single-File), использующее движок **Whisper.net** с аппаратным ускорением DirectML и AVX2/AVX512 для мгновенного набора текста на любом ПК.

---

## 🔒 Конфиденциальность и безопасность (100% Offline)

В эпоху облачных сервисов UltraDictate гарантирует абсолютную приватность вашей речи и вводимых данных:

1. **Никакой передачи аудио в сеть:** Захват голоса с микрофона (WASAPI на Windows, CoreAudio на macOS) происходит исключительно в оперативной памяти (In-Memory PCM Stream). Ни аудиозаписи, ни их спектрограммы никогда не отправляются на удаленные серверы.
2. **Локальные модели нейросетей:** Все веса моделей Whisper хранятся локально на вашем компьютере:
   - **Windows:** `%APPDATA%\UltraDictate\models\`
   - **macOS:** `~/Library/Application Support/UltraDictate/`
3. **Нулевая телеметрия (Zero Telemetry):** В приложении нет аналитики, трекеров, сбора персональных данных или логов нажатия клавиш.
4. **Безопасная постобработка (Local LLM):** Опциональный модуль исправления грамматики и пунктуации работает с локально запущенным **Ollama** (`http://localhost:11434/v1`) или **LM Studio** (`http://localhost:1234/v1`). Вам не требуется передавать текст внешним API или оплачивать подписки.

---

## 🧠 Выбор модели Whisper: Small vs Base

При первом запуске UltraDictate мастер настройки предлагает выбрать подходящий профиль модели. Вы также можете переключить профиль в любой момент в окне настроек:

| Параметр | 🌟 Whisper Small (Рекомендуется) | ⚡ Whisper Base (Быстрая) |
| :--- | :--- | :--- |
| **Размер на диске** | ~465 МБ (`ggml-small.bin`) | ~140 МБ (`ggml-base.bin`) |
| **Параметры нейросети** | 244 миллиона | 74 миллиона |
| **Качество русского языка** | **Максимальное** (качество Apple Silicon Mac) | Базовое (для коротких заметок) |
| **Словарный запас** | Профессиональные термины, сленг, пунктуация | Повседневные слова |
| **Скорость декодирования** | ~0.8 – 1.4 сек на предложение | < 0.4 сек на предложение |
| **Потребление RAM** | ~1.0 ГБ | ~400 МБ |
| **Для кого подходит** | Основной инструмент для работы, кода и текстов | Ноутбуки, слабые ПК, быстрый отклик |

---

## 📦 Как скачать и установить

### 🪟 Для Windows 10 / 11 (64-бит)

Установка предельно проста — приложение поставляется как **единый автономный файл** (все зависимости и библиотеки .NET 8 уже вшиты внутрь):

1. Скачайте архив **`UltraDictate-Windows-x64.zip`** из [GitHub Releases](https://github.com/m0rvey/ultradictate/releases/latest).
2. Распакуйте архив в удобное место.
3. Запустите **`UltraDictate.exe`**.
4. При первом запуске откроется приветственное окно: выберите профиль **Whisper Small** (рекомендуется) или **Whisper Base** и нажмите **«Начать использование»**.
5. Зажмите **Правый Ctrl** и диктуйте текст.

### 🍏 Для macOS (Apple Silicon M1–M5, A18 Pro)

1. **Быстрая установка через терминал:**
   ```bash
   curl -fsSL https://raw.githubusercontent.com/m0rvey/ultradictate/main/install.sh | /usr/bin/arch -arm64 /bin/bash
   ```
2. **Либо скачивание готового приложения:**
   - Скачайте **`UltraDictate-macOS-arm64.zip`** из [Releases](https://github.com/m0rvey/ultradictate/releases/latest).
   - Распакуйте и переместите `UltraDictate.app` в папку `/Applications`.
3. Запустите приложение и выдайте разрешения в macOS (Микрофон, Универсальный доступ, Мониторинг ввода).
4. Удерживайте **Правый ⌘ (Right Command)** и диктуйте.

---

## ⌨️ Горячие клавиши

| Платформа | Горячая клавиша по умолчанию | Назначение |
| :--- | :--- | :--- |
| **Windows** | `Правый Ctrl (Right Control)` | Удерживайте для записи, отпустите для вставки |
| **macOS** | `Правый ⌘ (Right Command)` | Удерживайте для записи, отпустите для вставки |
| **Все ОС** | `Escape` | Отмена диктовки без вставки текста |

---

## 📜 Лицензия и Авторство

- Автор: **m0rvey** ([GitHub](https://github.com/m0rvey))
- Распространяется под лицензией [MIT](../LICENSE).
