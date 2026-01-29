import React, { useEffect, useState } from 'react';
import Layout from '../components/Layout';
import ProtectedRoute from '../components/ProtectedRoute';
import {
  ModerationRequest,
  approveModeration,
  getModerationPreview,
  getModerationRequests,
  rejectModeration,
} from '../api';

/**
 * Очередь модерации в веб-админке.
 */
export default function ModerationPage() {
  return (
    <ProtectedRoute>
      <Layout>
        <ModerationContent />
      </Layout>
    </ProtectedRoute>
  );
}

function statusLabel(status: number): string {
  if (status === 0) return 'Pending';
  if (status === 1) return 'Approved';
  if (status === 2) return 'Rejected';
  return String(status);
}

function ModerationContent() {
  const [status, setStatus] = useState<number>(0);
  const [list, setList] = useState<ModerationRequest[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [msg, setMsg] = useState<string | null>(null);

  const [previewFor, setPreviewFor] = useState<string | null>(null);
  const [previewText, setPreviewText] = useState<string>('');

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status]);

  async function load() {
    setLoading(true);
    setError(null);
    setMsg(null);
    try {
      const data = await getModerationRequests(status);
      setList(data);
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка загрузки');
    } finally {
      setLoading(false);
    }
  }

  async function openPreview(id: string) {
    setPreviewFor(id);
    setPreviewText('Загрузка…');
    try {
      const t = await getModerationPreview(id);
      setPreviewText(t);
    } catch (e: any) {
      setPreviewText(e?.message ?? 'Ошибка предпросмотра');
    }
  }

  async function onApprove(id: string) {
    if (!confirm('Одобрить и опубликовать?')) return;
    setError(null);
    setMsg(null);
    try {
      const res = await approveModeration(id);
      setMsg(res.message);
      await load();
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка');
    }
  }

  async function onReject(id: string) {
    const reason = prompt('Причина отклонения (необязательно)') ?? undefined;
    if (!confirm('Отклонить заявку?')) return;
    setError(null);
    setMsg(null);
    try {
      const res = await rejectModeration(id, reason);
      setMsg(res.message);
      await load();
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка');
    }
  }

  return (
    <>
      <h1>Модерация</h1>

      <div className="card flex">
        <label>
          Статус:{' '}
          <select value={status} onChange={(e) => setStatus(Number(e.target.value))}>
            <option value={0}>Pending</option>
            <option value={1}>Approved</option>
            <option value={2}>Rejected</option>
          </select>
        </label>
        <button onClick={load} disabled={loading}>Обновить</button>
      </div>

      {error && <div className="card error">{error}</div>}
      {msg && <div className="card success">{msg}</div>}

      <div className="card">
        <table className="table">
          <thead>
            <tr>
              <th>Создано</th>
              <th>RequestId</th>
              <th>AdId</th>
              <th>ChannelId</th>
              <th>Статус</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {list.map(r => (
              <tr key={r.id}>
                <td>{new Date(r.createdAtUtc).toLocaleString()}</td>
                <td><code>{r.id}</code></td>
                <td><code>{r.adId}</code></td>
                <td><code>{r.channelId}</code></td>
                <td><span className="badge">{statusLabel(r.status)}</span></td>
                <td>
                  <div className="flex">
                    <button onClick={() => openPreview(r.id)}>Предпросмотр</button>
                    {r.status === 0 && (
                      <>
                        <button className="primary" onClick={() => onApprove(r.id)}>✅ Одобрить</button>
                        <button className="danger" onClick={() => onReject(r.id)}>❌ Отклонить</button>
                      </>
                    )}
                  </div>
                </td>
              </tr>
            ))}
            {list.length === 0 && (
              <tr><td colSpan={6} className="muted">Нет заявок</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {previewFor && (
        <div className="card">
          <h3>Предпросмотр: <code>{previewFor}</code></h3>
          <p className="muted">
            Это HTML-текст, который бот отправляет модераторам в Telegram (мы показываем как plain text).
          </p>
          <pre className="card" style={{ whiteSpace: 'pre-wrap' }}>{previewText}</pre>
          <button className="ghost" onClick={() => setPreviewFor(null)}>Закрыть</button>
        </div>
      )}
    </>
  );
}
