<div align="center">

# 🎙️ UltraDictate

**Сверхбыстрая, полностью локальная и приватная система голосовой диктовки для macOS и Windows.**

[![Release](https://img.shields.io/github/v/release/m0rvey/ultradictate?style=flat-square&color=2ea44f&label=Release)](https://github.com/m0rvey/ultradictate/releases/latest)
[![CI](https://img.shields.io/github/actions/workflow/status/m0rvey/ultradictate/build.yml?branch=main&style=flat-square&label=CI)](https://github.com/m0rvey/ultradictate/actions/workflows/build.yml)
[![macOS](https://img.shields.io/badge/macOS-Apple%20Silicon%20(M1--M5%2C%20A18%20Pro)-000000?style=flat-square&logo=apple&logoColor=white)](https://github.com/m0rvey/ultradictate/releases/latest)
[![Windows](https://img.shields.io/badge/Windows-10%2F11%20(x64)-0078D6?style=flat-square&logo=windows&logoColor=white)](https://github.com/m0rvey/ultradictate/releases/latest)
[![Swift](https://img.shields.io/badge/Swift-6.0-F05138?style=flat-square&logo=swift&logoColor=white)](https://swift.org/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![DirectML](https://img.shields.io/badge/DirectML-Hardware%20Accelerated-0078D4?style=flat-square)](windows/)
[![CoreML](https://img.shields.io/badge/CoreML-Neural%20Engine-FF6F00?style=flat-square&logo=apple&logoColor=white)](https://developer.apple.com/documentation/coreml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

[Возможности](#-ключевые-возможности) • [Модели речи](#-выбор-модели-whisper-small-vs-base) • [Конфиденциальность](#-конфиденциальность-и-безопасность-100-offline) • [Как скачать и установить](#-как-скачать-и-установить) • [Горячие клавиши](#-горячие-клавиши) • [Архитектура](#-архитектура-проекта) • [English Version](docs/README_EN.md)

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

> [!TIP]
> Для русского языка мы настоятельно рекомендуем профиль **Whisper Small**. Благодаря оптимизациям жадного поиска и контекстного прайминга он идеально распознает сложные падежные окончания и не допускает повторов слов.

---

## 📦 Как скачать и установить

### 🪟 Для Windows 10 / 11 (64-бит)

Установка предельно проста — приложение поставляется как **единый автономный файл** (все зависимости и библиотеки .NET 8 уже вшиты внутрь, устанавливать .NET или сторонние пакеты не требуется):

1. Перейдите на страницу [GitHub Releases](https://github.com/m0rvey/ultradictate/releases/latest) и скачайте архив **`UltraDictate-Windows-x64.zip`**.
2. Распакуйте архив в удобное место (например, `C:\Program Files\UltraDictate` или в папку пользователя).
3. Запустите **`UltraDictate.exe`**.
4. При первом запуске откроется приветственное окно:
   - Выберите профиль: **Whisper Small** (рекомендуется) или **Whisper Base**.
   - Нажмите **«Начать использование»** (модель автоматически загрузится в защищенную папку `%APPDATA%\UltraDictate\models\`).
5. В системном трее появится иконка UltraDictate. Зажмите **Правый Ctrl** и говорите. Текст мгновенно появится в месте, где мигает курсор!

*Сборка из исходного кода на Windows:*
```cmd
git clone https://github.com/m0rvey/ultradictate.git
cd ultradictate
windows\build.bat
```

---

### 🍏 Для macOS (Apple Silicon M1–M5, A18 Pro)

1. **Быстрая установка через терминал:**
   ```bash
   curl -fsSL https://raw.githubusercontent.com/m0rvey/ultradictate/main/install.sh | /usr/bin/arch -arm64 /bin/bash
   ```
2. **Либо скачивание готового приложения:**
   - Скачайте **`UltraDictate-macOS-arm64.zip`** из [Releases](https://github.com/m0rvey/ultradictate/releases/latest).
   - Распакуйте и переместите `UltraDictate.app` в папку `/Applications`.
3. Запустите приложение и выдайте разрешения в macOS:
   - **Микрофон** (для захвата аудиопотока)
   - **Универсальный доступ** (для эмуляции клавиатуры и вставки текста)
   - **Мониторинг ввода** (для перехвата глобального хоткея)
4. Удерживайте **Правый ⌘ (Right Command)** и диктуйте.

---

## ✨ Ключевые возможности

- ⚡ **Мгновенный Push-to-Talk:** Зажали клавишу — надиктовали — отпустили — текст уже в документе.
- 🎨 **Эстетичный акриловый HUD:**
  - Плавающая капсула в стиле Apple Dynamic Island со скруглением 26px.
  - Высокоточный эквалайзер 60 FPS с перцептивной чувствительностью и асимметричной физикой затухания.
  - Анимированная бегущая волна в режиме транскрипции «Processing...».
  - Полное отсутствие перехвата фокуса (`WS_EX_NOACTIVATE`) — курсор всегда остается в текстовом поле.
- ⚙️ **Современное окно настроек (3 вкладки):**
  - **Диктовка и ввод:** выбор клавиши (`Right Ctrl`, `Right Alt`, `Caps Lock`, `F8`), режима срабатывания (удержание / переключение), языка и метода вставки.
  - **Модель Whisper:** переключение между профилями Small и Base с отображением размера на диске и статуса.
  - **AI Постобработка:** быстрые пресеты в один клик для локального Ollama, LM Studio или облачных моделей с индикатором связи.
- 🗣️ **Встроенные голосовые команды:**
  - «новая строка» ➔ перенос строки (`\n`)
  - «новый абзац» ➔ двойной перенос (`\n\n`)

---

## ⌨️ Горячие клавиши

| Платформа | Горячая клавиша по умолчанию | Назначение |
| :--- | :--- | :--- |
| **Windows** | `Правый Ctrl (Right Control)` | Удерживайте для записи, отпустите для вставки |
| **macOS** | `Правый ⌘ (Right Command)` | Удерживайте для записи, отпустите для вставки |
| **Все ОС** | `Escape` | Отмена диктовки без вставки текста |

*В окне настроек на Windows также доступны: `Right Alt`, `Caps Lock`, `F8`, а также режим переключения по клику (Press-to-Toggle).*

---

## 🏗️ Архитектура проекта

```
ultradictate/
├── swift/                     # Нативный движок для macOS (Apple Silicon M1-M5, A18 Pro)
│   ├── Package.swift          # Модульная сборка SwiftPM
│   ├── FluidAudio/            # Локальная библиотека CoreML/ANE инференса
│   └── Sources/UltraDictate/
│       ├── Audio/             # Захват и конвертация звука (AVAudioEngine)
│       ├── Core/              # Глобальные хоткеи (CGEventTap), настройки, сервис
│       ├── Speech/            # Модели Parakeet/Whisper, верификация кэша (<0.2 мс)
│       ├── Text/              # Очистка текста, автозамены, Ollama/AI клиент
│       └── UI/                # Менюбар, плавающий HUD 120 Гц, статистика
│
├── windows/                   # Нативный движок для Windows (DirectML / Whisper.net)
│   ├── UltraDictate.Windows/  # Проект .NET 8 / C#
│   │   ├── Core/              # WASAPI аудио, Whisper.net инференс, SendInput вставка
│   │   └── UI/                # Трей-меню, акриловая капсула HUD, мастер настройки моделей
│   ├── build.bat              # Скрипт автономной сборки единого .exe
│   └── install.ps1            # Скрипт быстрой локальной установки
│
├── scripts/                   # Утилиты сборки и вспомогательные скрипты
│   ├── build-app.sh           # Сборка и подпись UltraDictate.app
│   ├── download_small_model.ps1 # Фоновая загрузка модели Whisper Small
│   └── check.sh               # Комплексная проверка целостности и версий
│
└── .github/workflows/         # CI/CD релизный пайплайн
    ├── build.yml              # Автоматическая проверка сборки на macOS и Windows
    └── release.yml            # Автоматическая сборка мультиплатформенных релизов
```

---

## 📜 Лицензия и Авторство

- Автор: **m0rvey** ([GitHub](https://github.com/m0rvey))
- Основано на наработках SuperDictate и Parakey (MIT License).
- Распространяется под лицензией [MIT](LICENSE).
