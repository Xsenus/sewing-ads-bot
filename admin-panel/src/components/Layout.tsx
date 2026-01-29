import React, { useEffect, useMemo, useState } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { clearToken } from '../auth';

/**
 * Основной layout админки: сайдбар + контент.
 */
export default function Layout({ children }: { children: React.ReactNode }) {
  const nav = useNavigate();
  const [theme, setTheme] = useState<'dark' | 'light'>(() =>
    (localStorage.getItem('admin-theme') as 'dark' | 'light') ?? 'dark',
  );
  const [collapsed, setCollapsed] = useState<boolean>(() => localStorage.getItem('admin-sidebar') === 'collapsed');

  useEffect(() => {
    document.body.dataset.theme = theme;
    localStorage.setItem('admin-theme', theme);
  }, [theme]);

  useEffect(() => {
    localStorage.setItem('admin-sidebar', collapsed ? 'collapsed' : 'expanded');
  }, [collapsed]);

  const themeLabel = useMemo(() => (theme === 'dark' ? 'Светлая тема' : 'Тёмная тема'), [theme]);

  return (
    <div className={`container ${collapsed ? 'collapsed' : ''}`}>
      <aside className="sidebar">
        <div className="sidebar-header">
          <div>
            <h2>SewingAdsBot</h2>
            <span className="muted">Панель управления</span>
          </div>
          <button className="ghost icon-button" onClick={() => setCollapsed((prev) => !prev)}>
            {collapsed ? '➡️' : '⬅️'}
          </button>
        </div>
        <div className="nav">
          <NavLink to="/" end>
            <span className="nav-icon">📊</span>
            <span className="nav-text">Сводка</span>
          </NavLink>
          <NavLink to="/bots">
            <span className="nav-icon">🤖</span>
            <span className="nav-text">Боты</span>
          </NavLink>
          <NavLink to="/categories">
            <span className="nav-icon">🧵</span>
            <span className="nav-text">Категории</span>
          </NavLink>
          <NavLink to="/channels">
            <span className="nav-icon">📣</span>
            <span className="nav-text">Каналы</span>
          </NavLink>
          <NavLink to="/moderation">
            <span className="nav-icon">🛡️</span>
            <span className="nav-text">Модерация</span>
          </NavLink>
          <NavLink to="/telegram-admins">
            <span className="nav-icon">👩‍💼</span>
            <span className="nav-text">Telegram‑модераторы</span>
          </NavLink>
          <NavLink to="/admin-accounts">
            <span className="nav-icon">🔐</span>
            <span className="nav-text">Админы админки</span>
          </NavLink>
          <NavLink to="/settings">
            <span className="nav-icon">⚙️</span>
            <span className="nav-text">Настройки</span>
          </NavLink>
        </div>

        <hr />

        <div className="flex column">
          <button className="ghost" onClick={() => setTheme((prev) => (prev === 'dark' ? 'light' : 'dark'))}>
            {themeLabel}
          </button>
          <button
            className="danger"
            onClick={() => {
              clearToken();
              nav('/login');
            }}
          >
            Выйти
          </button>
        </div>

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
