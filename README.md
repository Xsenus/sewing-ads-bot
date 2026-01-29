# SewingAdsBot (Вариант C)

Бот Telegram для публикации объявлений в швейной индустрии.

Реализовано по **варианту C**: **категория → несколько каналов** через таблицу `CategoryChannels`.

## Возможности

- Главное меню:
  - **Создать объявление**
  - **Найти объявление**
  - **Мой профиль**
  - **Помощь**
  - **Платное объявление** (показывает ссылку на тарифы в telegra.ph)

- Профиль:
  - **Место** (страна/город)
  - **Мои объявления**
  - **Реферальная ссылка**

- Создание объявления:
  - выбор категории (дерево категорий)
  - выбор типа: **бесплатное / платное**
  - ввод: заголовок, текст, контакты
  - **платное**: можно добавить фото/видео
  - предпросмотр + редактирование полей
  - публикация:
    - бесплатное — сразу отправляется в каналы
    - платное — отправляется **инвойс Telegram Payments**, после оплаты публикуется автоматически

- Бесплатные ограничения:
  - **строгий лимит 1 раз в календарные сутки (UTC)**
  - **без фото/видео**
  - **без ссылок** (с анти-обход нормализацией)

- Модерация:
  - Каналы могут быть в режиме `Auto` или `Moderation`
  - Для `Moderation` создаётся заявка и отправляется всем активным `TelegramAdmins` в личку
  - Кнопки ✅/❌ в inline, по одобрению — публикация и уведомление пользователя со ссылкой (если канал публичный)

- Закреп в канале:
  - Админка умеет закрепить/открепить сообщение с кнопкой **ОПУБЛИКОВАТЬ**
  - Кнопка ведёт на deep-link: `https://t.me/<bot>?start=publish`

- Рефералка:
  - `https://t.me/<bot>?start=ref_<code>`
  - Вознаграждение начисляется на `User.Balance` (процент настраивается в `App.ReferralRewardPercent`)

## Технологии

- .NET 8 / ASP.NET Core
- Telegram.Bot
- EF Core + PostgreSQL
- Swagger
- JWT для админки
- Prometheus metrics (/metrics)

## React админка

В проект добавлена веб‑админка на React (Vite) в папке `admin-panel`.

Что умеет:
- управление категориями
- управление каналами (включая закреп/откреп кнопки «ОПУБЛИКОВАТЬ»)
- назначение **нескольких каналов** на категорию (вариант C)
- просмотр очереди модерации + **approve/reject** из веб‑админки
- управление списком Telegram‑модераторов
- редактирование `AppSettings`

Запуск:

```bash
cd admin-panel
npm i
cp .env.example .env
npm run dev
```

## Быстрый старт

1) Поднимите PostgreSQL (локально или в Docker) и задайте connection string в `src/SewingAdsBot.Api/appsettings.json`

2) Укажите токен бота:

```json
"Telegram": {
  "BotToken": "PUT_TELEGRAM_BOT_TOKEN_HERE",
  "PaymentProviderToken": "PUT_TELEGRAM_PAYMENT_PROVIDER_TOKEN_HERE"
}
```

3) Запуск:

```bash
cd src/SewingAdsBot.Api
dotnet restore
dotnet run
```

При старте:
- применяются миграции (`db.Database.MigrateAsync()`)
- выполняется seed:
  - создаётся первый админ (логин/пароль из `Admin`),
  - создаются категории из ТЗ,
  - добавляется пример канала (неактивный).

## Админ API

Swagger: `/swagger`

- `POST /api/admin/auth/login` — получить JWT
- `GET/POST/PUT/DELETE /api/admin/categories` — категории
- `PUT /api/admin/categories/{id}/channels` — назначить каналы для категории (вариант C)
- `GET /api/admin/categories/{id}/channels` — получить назначенные каналы (для админки)
- `GET/POST/PUT/DELETE /api/admin/channels` — каналы
- `POST /api/admin/channels/{id}/pin` — закрепить кнопку «ОПУБЛИКОВАТЬ»
- `POST /api/admin/channels/{id}/unpin` — открепить
- `GET/POST/DELETE /api/admin/telegram-admins` — модераторы
- `GET/PUT /api/admin/settings` — настройки (например цены: `PaidAdPriceMinor`, `BumpPriceMinor`)

Модерация из админки:
- `GET /api/admin/moderation/requests?status=0` — список заявок (Pending)
- `GET /api/admin/moderation/requests/{id}/preview` — предпросмотр
- `POST /api/admin/moderation/requests/{id}/approve` — одобрить и опубликовать
- `POST /api/admin/moderation/requests/{id}/reject` — отклонить

## Важно про Telegram права

- Бот должен быть **администратором канала**, чтобы:
  - публиковать сообщения,
  - закреплять/откреплять сообщения,
  - проверять подписку (`GetChatMember`).
- Для приватных каналов ссылка на сообщение не формируется (нет username).
