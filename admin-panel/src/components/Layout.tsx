import React from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { clearToken } from '../auth';

/**
 * Основной layout админки: сайдбар + контент.
 */
export default function Layout({ children }: { children: React.ReactNode }) {
  const nav = useNavigate();

  return (
    <div className="container">
      <aside className="sidebar">
        <h2>SewingAdsBot — админка</h2>
        <div className="nav">
          <NavLink to="/" end>Сводка</NavLink>
          <NavLink to="/categories">Категории</NavLink>
          <NavLink to="/channels">Каналы</NavLink>
          <NavLink to="/moderation">Модерация</NavLink>
          <NavLink to="/telegram-admins">Telegram‑модераторы</NavLink>
          <NavLink to="/admin-accounts">Админы админки</NavLink>
          <NavLink to="/settings">Настройки</NavLink>
        </div>

        <hr />

        <button
          className="danger"
          onClick={() => {
            clearToken();
            nav('/login');
          }}
        >
          Выйти
        </button>

        <p className="muted" style={{ marginTop: 12 }}>
          API: <code>{import.meta.env.VITE_API_URL ?? 'http://localhost:5000'}</code>
        </p>
      </aside>

      <main className="main">
        {children}
      </main>
    </div>
  );
}
