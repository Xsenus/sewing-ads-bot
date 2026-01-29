# SewingAdsBot — Admin Panel (React)

## Запуск

1) Установите зависимости:

```bash
npm i
```

2) Создайте `.env` рядом с `package.json`:

```bash
VITE_API_URL=http://localhost:5000
```

3) Запустите:

```bash
npm run dev
```

Админка: `http://localhost:5173`

## Авторизация

Используется JWT. Логин/пароль настраиваются в backend `appsettings.json` (секция `Admin`).
