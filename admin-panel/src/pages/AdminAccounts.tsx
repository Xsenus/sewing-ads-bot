import React, { useEffect, useState } from 'react';
import Layout from '../components/Layout';
import ProtectedRoute from '../components/ProtectedRoute';
import { AdminAccount, createAdminAccount, getAdminAccounts, resetAdminPassword, setAdminAccountActive } from '../api';

/**
 * Управление пользователями веб‑админки (логин/пароль).
 */
export default function AdminAccountsPage() {
  return (
    <ProtectedRoute>
      <Layout>
        <AdminAccountsContent />
      </Layout>
    </ProtectedRoute>
  );
}

function AdminAccountsContent() {
  const [list, setList] = useState<AdminAccount[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [msg, setMsg] = useState<string | null>(null);

  useEffect(() => {
    void load();
  }, []);

  async function load() {
    setError(null);
    setMsg(null);
    try {
      const l = await getAdminAccounts();
      setList(l);
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка загрузки');
    }
  }

  async function onCreate() {
    const username = prompt('Логин (username) нового админа');
    if (!username) return;

    const password = prompt('Пароль (минимум 6 символов)');
    if (!password) return;

    setError(null);
    setMsg(null);

    try {
      await createAdminAccount({ username, password, isActive: true });
      setMsg('Создано');
      await load();
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка');
    }
  }

  async function onResetPassword(id: string, username: string) {
    const password = prompt(`Новый пароль для ${username} (минимум 6 символов)`);
    if (!password) return;

    setError(null);
    setMsg(null);

    try {
      await resetAdminPassword(id, password);
      setMsg('Пароль обновлён');
      await load();
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка');
    }
  }

  async function onToggle(id: string, username: string, isActive: boolean) {
    if (!confirm(`${isActive ? 'Отключить' : 'Включить'} ${username}?`)) return;

    setError(null);
    setMsg(null);

    try {
      await setAdminAccountActive(id, !isActive);
      setMsg('Обновлено');
      await load();
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка');
    }
  }

  return (
    <>
      <h1>Админы админки</h1>

      <div className="card muted">
        Эти пользователи могут входить в веб‑панель (JWT‑авторизация). Telegram‑модераторы управляются отдельно.
      </div>

      {error && <div className="card error">{error}</div>}
      {msg && <div className="card success">{msg}</div>}

      <div className="card flex">
        <button className="primary" onClick={onCreate}>+ Создать админа</button>
        <button onClick={load}>Обновить</button>
      </div>

      <div className="card">
        <table className="table">
          <thead>
            <tr>
              <th>Логин</th>
              <th>Активен</th>
              <th>Создан</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {list.map(x => (
              <tr key={x.id}>
                <td><code>{x.username}</code></td>
                <td>{x.isActive ? '✅' : '—'}</td>
                <td>{new Date(x.createdAtUtc).toLocaleString()}</td>
                <td>
                  <button onClick={() => onResetPassword(x.id, x.username)}>Сбросить пароль</button>
                  <span style={{ margin: '0 6px' }} />
                  <button className={x.isActive ? 'danger' : 'primary'} onClick={() => onToggle(x.id, x.username, x.isActive)}>
                    {x.isActive ? 'Отключить' : 'Включить'}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
