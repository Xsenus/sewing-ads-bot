import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { login } from '../api';
import { setToken } from '../auth';

/**
 * Страница входа в админку.
 */
export default function Login() {
  const nav = useNavigate();
  const [username, setUsername] = useState('admin');
  const [password, setPassword] = useState('admin12345');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const token = await login(username, password);
      setToken(token);
      nav('/');
    } catch (err: any) {
      setError(err?.message ?? 'Ошибка входа');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ maxWidth: 520, margin: '60px auto' }}>
      <div className="card">
        <h2>Вход в админку</h2>
        <p className="muted">
          Логин/пароль по умолчанию задаются в <code>backend/src/SewingAdsBot.Api/appsettings.json</code> (секция <code>Admin</code>).
          Если админ уже создан в БД, используются его актуальные значения.
        </p>

        <form onSubmit={onSubmit}>
          <div className="row">
            <div>Логин</div>
            <input value={username} onChange={e => setUsername(e.target.value)} />
          </div>

          <div className="row">
            <div>Пароль</div>
            <input type="password" value={password} onChange={e => setPassword(e.target.value)} />
          </div>

          {error && <div className="error" style={{ marginBottom: 10 }}>{error}</div>}

          <button className="primary" disabled={loading} type="submit">
            {loading ? 'Входим…' : 'Войти'}
          </button>
        </form>
      </div>

      <div className="card">
        <h3>Подключение к API</h3>
        <p className="muted">
          Создайте файл <code>.env</code> рядом с <code>package.json</code> и укажите:
        </p>
        <pre className="card" style={{ marginTop: 10 }}>
VITE_API_URL=http://localhost:5000
        </pre>
      </div>
    </div>
  );
}
