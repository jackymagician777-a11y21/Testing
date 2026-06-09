# Steam Auto Launcher

Автоматизированная программа для циклического запуска Steam-аккаунтов и игры Bongo Cat с автоматическим вводом Steam Guard кодов.

## Возможности

- ✅ **Циклический запуск аккаунтов** — автоматически переключается между аккаунтами
- ✅ **Steam Guard автоматизация** — генерирует и вводит 2FA коды из `.maFile`
- ✅ **Гибкий запуск игры** — поддержка AppID и прямого пути до executable
- ✅ **Полная автоматизация** — от логина в Steam до выхода из игры
- ✅ **Настраиваемые задержки** — для каждого шага процесса
- ✅ **WPF интерфейс** — красивое окно с логами
- ✅ **Логирование** — файловые логи и консоль

## Требования

- **Windows 10/11** (64-bit)
- **.NET 8.0 Runtime** ([скачать](https://dotnet.microsoft.com/en-us/download/dotnet/8.0))
- **Steam** установлен
- **.maFile** файлы для каждого аккаунта

## Установка

### 1. Клонирование репозитория
```bash
git clone https://github.com/jackymagician777-a11y21/Testing.git
cd Testing
```

### 2. Подготовка

Убедитесь, что у вас установлен .NET 8.0:
```bash
dotnet --version
```

### 3. Создание папки с .maFile'ами

Создайте папку `mafiles` в директории программы и положите туда ваши `.maFile` файлы:
```
mafiles/
├── fillin.maFile
├── account2.maFile
└── ...
```

**Имя файла должно совпадать с логином!** Например: `fillin.maFile` для аккаунта с логином `fillin`.

## Конфигурация

### config.json

Отредактируйте `config.json` с вашими данными:

```json
{
  "accounts": [
    {
      "label": "Основной",
      "login": "fillin",
      "password": "YOUR_PASSWORD",
      "maFilePath": "./mafiles/fillin.maFile"
    },
    {
      "label": "Торговый",
      "login": "account2",
      "password": "PASSWORD_2",
      "maFilePath": "./mafiles/account2.maFile"
    }
  ],
  "game": {
    "appId": 3419430,
    "gamePath": "C:\\Games\\BongoCat\\BongoCat.exe",
    "launchMethod": "appid"
  },
  "delays": {
    "waitSteamStartMs": 5000,
    "waitSteamLoginMs": 15000,
    "gamePlayTimeMs": 20000,
    "waitGameStartMs": 3000,
    "waitGameExitMs": 2000,
    "waitSteamExitMs": 5000,
    "betweenAccountsMs": 3000
  },
  "settings": {
    "steamExePath": "C:\\Program Files (x86)\\Steam\\steam.exe",
    "uIAutomationTimeoutMs": 10000,
    "maxRetries": 3
  }
}
```

### Параметры конфига

#### `accounts` — список аккаунтов
- `label` — отображаемое имя в интерфейсе
- `login` — логин Steam
- `password` — пароль Steam (хранится открытым текстом!)
- `maFilePath` — путь к `.maFile`

#### `game` — настройки запуска игры
- `appId` — App ID игры (3419430 для Bongo Cat)
- `gamePath` — прямой путь до исполняемого файла
- `launchMethod` — `"appid"` или `"path"`

#### `delays` (мс)
- `waitSteamStartMs` — ожидание запуска Steam
- `waitSteamLoginMs` — ожидание логина в Steam
- `gamePlayTimeMs` — время игры (20000 = 20 сек)
- `waitGameStartMs` — ожидание запуска игры
- `waitGameExitMs` — ожидание выхода из игры
- `waitSteamExitMs` — ожидание выхода из Steam
- `betweenAccountsMs` — пауза между аккаунтами

#### `settings`
- `steamExePath` — путь к `steam.exe`
- `uIAutomationTimeoutMs` — таймаут UI Automation
- `maxRetries` — максимум повторов при ошибках

## Использование

### Запуск программы

```bash
dotnet build -c Release
dotnet run
```

Или просто запустите скомпилированный `.exe` файл:
```bash
./bin/Release/net8.0-windows/SteamAutoLauncher.exe
```

### В интерфейсе

1. Нажмите **Start** — программа начнёт цикл
2. В левой части видны **логи** всех действий
3. Справа — информация о конфигурации
4. Нажмите **Stop** для остановки

## Логирование

Все логи сохраняются в папку `logs/`:
- `log_2024-12-09.txt` — логи за каждый день

## Безопасность

⚠️ **ВАЖНО:**
- Пароли хранятся в `config.json` **открытым текстом**
- Не коммитьте `config.json` в git
- Не оставляйте программу на общедоступном ПК
- Добавлен `.gitignore` — `config.json` не попадёт в репозиторий

## Проблемы и решения

### "Configuration file 'config.json' not found"
Создайте `config.json` в директории программы

### Steam Guard код не вводится автоматически
UI Automation может не работать если:
- Steam обновился
- Окно логина скрыто
- У программы нет прав администратора

В этом случае введите код вручную в окне Steam.

### Игра не запускается
- Проверьте правильность `appId` или `gamePath`
- Убедитесь, что Steam запущен
- Проверьте путь к `steam.exe`

### Программа зависает
Увеличьте значения в `delays`:
- `waitSteamStartMs` → 7000-10000
- `waitSteamLoginMs` → 20000-30000

## Структура проекта

```
SteamAutoLauncher/
├── Config/
│   ├── AppConfig.cs           # Классы конфигурации
│   └── ConfigManager.cs       # Управление конфигом
├── Core/
│   ├── SteamGuard/
│   │   ├── MaFileParser.cs    # Парсинг .maFile
│   │   └── SteamGuardGenerator.cs  # TOTP генерация
│   ├── SteamClient/
│   │   ├── SteamProcessManager.cs  # Управление Steam
│   │   └── UIAutomationHelper.cs   # Ввод 2FA
│   ├── GameLauncher/
│   │   └── GameLauncher.cs    # Запуск игры
│   ├── Logging/
│   │   └── Logger.cs          # Логирование
│   └── AccountCycler.cs       # Главный цикл
├── UI/
│   ├── MainWindow.xaml        # Интерфейс
│   └── MainWindow.xaml.cs     # Логика UI
├── App.xaml / App.xaml.cs     # WPF приложение
├── Program.cs                 # Entry point
├── SteamAutoLauncher.csproj   # Проект
├── config.json                # Конфигурация
└── README.md                  # Этот файл
```

## Развитие проекта

Возможные улучшения:
- [ ] Шифрование паролей в конфиге
- [ ] Сохранение статистики циклов
- [ ] Планировщик по времени
- [ ] Поддержка прокси
- [ ] Экспорт логов в CSV

## Лицензия

MIT License

## Поддержка

Если возникли проблемы — откройте issue на GitHub.

---

**Важно:** Это приложение предназначено только для ваших собственных аккаунтов. Не используйте его для взлома или несанкционированного доступа к чужим аккаунтам.