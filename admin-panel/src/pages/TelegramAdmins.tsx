import React, { useEffect, useState } from 'react';
import Layout from '../components/Layout';
import ProtectedRoute from '../components/ProtectedRoute';
import { TelegramAdmin, addTelegramAdmin, deactivateTelegramAdmin, getTelegramAdmins } from '../api';

/**
 * Управление Telegram-модераторами.
 */
export default function TelegramAdminsPage() {
  return (
    <ProtectedRoute>
      <Layout>
        <TelegramAdminsContent />
      </Layout>
    </ProtectedRoute>
  );
}

function TelegramAdminsContent() {
  const [list, setList] = useState<TelegramAdmin[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [msg, setMsg] = useState<string | null>(null);

  useEffect(() => {
    void load();
  }, []);

  async function load() {
    setError(null);
    setMsg(null);
    try {
      const l = await getTelegramAdmins();
      setList(l);
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка загрузки');
    }
  }

  async function onAdd() {
    const s = prompt('TelegramUserId модератора (например 123456789)');
    if (!s) return;
    const id = Number(s);
    if (!id) {
      alert('Некорректный TelegramUserId');
      return;
    }

    setError(null);
    setMsg(null);

    try {
      await addTelegramAdmin(id);
      setMsg('Добавлено');
      await load();
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка');
    }
  }

  async function onDeactivate(telegramUserId: number) {
    if (!confirm(`Деактивировать ${telegramUserId}?`)) return;
    setError(null);
    setMsg(null);
    try {
      await deactivateTelegramAdmin(telegramUserId);
      setMsg('Деактивировано');
      await load();
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка');
    }
  }

  return (
    <>
      <h1>Telegram‑модераторы</h1>

      <div className="card muted">
        Эти TelegramUserId получают заявки в личку с inline кнопками ✅/❌.
      </div>

      {error && <div className="card error">{error}</div>}
      {msg && <div className="card success">{msg}</div>}

      <div className="card flex">
        <button className="primary" onClick={onAdd}>+ Добавить модератора</button>
        <button onClick={load}>Обновить</button>
      </div>

      <div className="card">
        <table className="table">
          <thead>
            <tr>
              <th>TelegramUserId</th>
              <th>Активен</th>
              <th>Создан</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {list.map(x => (
              <tr key={x.id}>
                <td><code>{x.telegramUserId}</code></td>
                <td>{x.isActive ? '✅' : '—'}</td>
                <td>{new Date(x.createdAtUtc).toLocaleString()}</td>
                <td>
                  {x.isActive && (
                    <button className="danger" onClick={() => onDeactivate(x.telegramUserId)}>Деактивировать</button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
