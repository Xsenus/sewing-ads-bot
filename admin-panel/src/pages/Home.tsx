import React from 'react';
import Layout from '../components/Layout';
import ProtectedRoute from '../components/ProtectedRoute';

/**
 * Главная страница / сводка.
 */
export default function HomePage() {
  return (
    <ProtectedRoute>
      <Layout>
        <h1>Сводка</h1>

        <div className="card">
          <h3>Что можно настроить</h3>
          <ul>
            <li>Категории + назначение нескольких каналов на категорию (вариант C)</li>
            <li>Каналы: режим модерации, спам‑фильтр, проверка подписки, футер‑ссылка, закреп кнопки «ОПУБЛИКОВАТЬ»</li>
            <li>Очередь модерации: approve / reject</li>
            <li>Telegram‑модераторы</li>
            <li>Глобальные настройки (AppSettings): цены, лимиты, тарифы и т.д.</li>
          </ul>
        </div>

        <div className="card">
          <h3>Важно</h3>
          <ul>
            <li>Backend должен быть запущен (по умолчанию <code>http://localhost:5000</code>).</li>
            <li>Бот должен быть админом каналов, иначе публикация/закреп не будут работать.</li>
            <li>JWT логин/пароль задаются в backend <code>appsettings.json</code> в секции <code>Admin</code>.</li>
          </ul>
        </div>
      </Layout>
    </ProtectedRoute>
  );
}
