import React, { useEffect, useMemo, useState } from 'react';
import Layout from '../components/Layout';
import ProtectedRoute from '../components/ProtectedRoute';
import ConfirmDialog from '../components/ConfirmDialog';
import {
  BotChannel,
  TelegramBot,
  createBot,
  deleteBot,
  disableBot,
  getBotChannels,
  getBots,
  pauseBot,
  refreshBot,
  restartBot,
  resumeBot,
  getApiUrl,
} from '../api';
import { getToken } from '../auth';

const statusLabels: Record<number, string> = {
  1: 'Активен',
  2: 'На паузе',
  3: 'Отключён',
};

const statusColors: Record<number, string> = {
  1: 'success',
  2: 'warning',
  3: 'muted',
};

export default function BotsPage() {
  const [bots, setBots] = useState<TelegramBot[]>([]);
  const [channels, setChannels] = useState<BotChannel[]>([]);
  const [activeBotId, setActiveBotId] = useState<string | null>(null);
  const [token, setToken] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [photoUrl, setPhotoUrl] = useState<string | null>(null);
  const [photoKey, setPhotoKey] = useState(0);
  const [confirm, setConfirm] = useState<{
    title: string;
    body: string;
    onConfirm: () => Promise<void>;
  } | null>(null);

  const activeBot = useMemo(() => bots.find((b) => b.id === activeBotId) ?? null, [bots, activeBotId]);

  async function loadBots() {
    try {
      setLoading(true);
      setError(null);
      const data = await getBots();
      setBots(data);
      if (data.length > 0 && !activeBotId) {
        setActiveBotId(data[0].id);
      }
    } catch (e: any) {
      setError(e?.message ?? 'Не удалось загрузить ботов.');
    } finally {
      setLoading(false);
    }
  }

  async function loadChannels(botId: string) {
    try {
      setChannels([]);
      const data = await getBotChannels(botId);
      setChannels(data);
    } catch (e: any) {
      setError(e?.message ?? 'Не удалось загрузить каналы бота.');
    }
  }

  useEffect(() => {
    void loadBots();
  }, []);

  useEffect(() => {
    if (activeBotId) {
      void loadChannels(activeBotId);
    }
  }, [activeBotId, photoKey]);

  useEffect(() => {
    let currentUrl: string | null = null;

    async function loadPhoto(botId: string) {
      try {
        const jwt = getToken();
        if (!jwt) return;

        const res = await fetch(`${getApiUrl()}/api/admin/bots/${botId}/photo`, {
          headers: {
            Authorization: `Bearer ${jwt}`,
          },
        });

        if (!res.ok) {
          setPhotoUrl(null);
          return;
        }

        const blob = await res.blob();
        currentUrl = URL.createObjectURL(blob);
        setPhotoUrl(currentUrl);
      } catch {
        setPhotoUrl(null);
      }
    }

    if (activeBotId) {
      void loadPhoto(activeBotId);
    } else {
      setPhotoUrl(null);
    }

    return () => {
      if (currentUrl) URL.revokeObjectURL(currentUrl);
    };
  }, [activeBotId]);

  const openConfirm = (title: string, body: string, action: () => Promise<void>) => {
    setConfirm({ title, body, onConfirm: action });
  };

  const handleAdd = async () => {
    try {
      setLoading(true);
      setError(null);
      setMessage(null);
      const newBot = await createBot(token.trim());
      setBots((prev) => [newBot, ...prev]);
      setToken('');
      setActiveBotId(newBot.id);
      setMessage('Бот добавлен и запущен.');
    } catch (e: any) {
      setError(e?.message ?? 'Не удалось добавить бота.');
    } finally {
      setLoading(false);
    }
  };

  const handleAction = async (action: () => Promise<any>, successMessage: string, reload = true) => {
    try {
      setLoading(true);
      setError(null);
      setMessage(null);
      await action();
      if (reload) {
        await loadBots();
        if (activeBotId) await loadChannels(activeBotId);
      }
      setMessage(successMessage);
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка операции.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <ProtectedRoute>
      <Layout>
        <div className="page-header">
          <div>
            <h1>Боты</h1>
            <p className="muted">Добавляйте сколько угодно Telegram‑ботов, управляйте статусами и каналами.</p>
          </div>
        </div>

        <div className="grid grid-2">
          <div className="card">
            <h3>Добавить бота</h3>
            <p className="muted">После сохранения бот сразу запускается и подтягивает профиль, команды и описание.</p>
            <div className="row">
              <label>Bot API токен</label>
              <input
                value={token}
                onChange={(e) => setToken(e.target.value)}
                placeholder="123456:AA..."
              />
            </div>
            <div className="flex">
              <button className="primary" disabled={loading || !token.trim()} onClick={handleAdd}>
                Добавить и запустить
              </button>
              <button className="ghost" onClick={loadBots} disabled={loading}>
                Обновить список
              </button>
            </div>
            {error && <p className="error">{error}</p>}
            {message && <p className="success">{message}</p>}
          </div>

          <div className="card">
            <h3>Список ботов</h3>
            {loading && <p className="muted">Загрузка...</p>}
            <div className="bot-list">
              {bots.map((bot) => (
                <button
                  key={bot.id}
                  className={`bot-list-item ${activeBotId === bot.id ? 'active' : ''}`}
                  onClick={() => setActiveBotId(bot.id)}
                >
                  <div className="bot-list-title">
                    <span className="badge">{statusLabels[bot.status] ?? 'Неизвестно'}</span>
                    <strong>{bot.name ?? 'Без имени'}</strong>
                  </div>
                  <span className="muted">@{bot.username ?? 'unknown'}</span>
                </button>
              ))}
              {bots.length === 0 && <p className="muted">Боты ещё не добавлены.</p>}
            </div>
          </div>
        </div>

        {activeBot && (
          <div className="grid grid-3">
            <div className="card">
              <div className="bot-profile">
                <img
                  src={photoUrl ?? 'https://via.placeholder.com/96x96?text=Bot'}
                  alt={activeBot.name ?? 'Bot'}
                />
                <div>
                  <h3>{activeBot.name ?? 'Без имени'}</h3>
                  <p className="muted">@{activeBot.username ?? 'unknown'}</p>
                  <p className={`badge ${statusColors[activeBot.status] ?? ''}`}>
                    {statusLabels[activeBot.status] ?? 'Неизвестно'}
                  </p>
                </div>
              </div>
              <div className="bot-meta">
                <p><strong>ID:</strong> {activeBot.telegramUserId}</p>
                <p><strong>Описание:</strong> {activeBot.description ?? '—'}</p>
                <p><strong>Короткое описание:</strong> {activeBot.shortDescription ?? '—'}</p>
              </div>
              {activeBot.lastError && (
                <div className="alert warning">
                  <strong>Последняя ошибка:</strong> {activeBot.lastError}
                  {activeBot.lastErrorAtUtc && (
                    <span className="muted"> ({new Date(activeBot.lastErrorAtUtc).toLocaleString()})</span>
                  )}
                </div>
              )}
              <div className="flex">
                <button
                  onClick={() =>
                    openConfirm('Поставить на паузу', 'Приостановить получение обновлений ботом?', () =>
                      handleAction(() => pauseBot(activeBot.id), 'Бот поставлен на паузу.'),
                    )
                  }
                >
                  Пауза
                </button>
                <button
                  onClick={() =>
                    openConfirm('Возобновить', 'Запустить бота снова?', () =>
                      handleAction(() => resumeBot(activeBot.id), 'Бот активирован.'),
                    )
                  }
                >
                  Возобновить
                </button>
                <button
                  onClick={() =>
                    openConfirm('Отключить', 'Отключить бота до ручного включения?', () =>
                      handleAction(() => disableBot(activeBot.id), 'Бот отключён.'),
                    )
                  }
                >
                  Отключить
                </button>
                <button
                  onClick={() =>
                    openConfirm('Перезапустить', 'Перезапустить бота с текущими настройками?', () =>
                      handleAction(() => restartBot(activeBot.id), 'Бот перезапущен.'),
                    )
                  }
                >
                  Перезапуск
                </button>
                <button
                  onClick={() =>
                    openConfirm('Обновить данные', 'Обновить описание, команды и аватар?', async () => {
                      const updated = await refreshBot(activeBot.id);
                      setBots((prev) => prev.map((bot) => (bot.id === updated.id ? updated : bot)));
                      await loadChannels(activeBot.id);
                      setPhotoKey((prev) => prev + 1);
                      setMessage('Данные обновлены.');
                    })
                  }
                >
                  Обновить данные
                </button>
                <button
                  className="danger"
                  onClick={() =>
                    openConfirm('Удалить', 'Удалить бота и остановить его работу?', () =>
                      handleAction(() => deleteBot(activeBot.id), 'Бот удалён.'),
                    )
                  }
                >
                  Удалить
                </button>
              </div>
            </div>

            <div className="card">
              <h3>Команды</h3>
              {activeBot.commands && activeBot.commands.length > 0 ? (
                <ul className="list">
                  {activeBot.commands.map((cmd, idx) => (
                    <li key={`${cmd.command}-${idx}`}>
                      <strong>/{cmd.command}</strong> — {cmd.description ?? 'Без описания'}
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="muted">Команды не заданы.</p>
              )}
            </div>

            <div className="card">
              <h3>Каналы, где бот админ</h3>
              {channels.length === 0 && <p className="muted">Нет доступных каналов.</p>}
              {channels.map((channel) => (
                <div key={channel.id} className="channel-card">
                  <div className="channel-header">
                    <strong>{channel.title}</strong>
                    <span className="badge">{channel.isActive ? 'Активен' : 'Выключен'}</span>
                  </div>
                  <p className="muted">
                    ID: {channel.telegramChatId}
                    {channel.telegramUsername && <> • @{channel.telegramUsername}</>}
                  </p>
                  <ul className="list">
                    <li>Модерация: {channel.moderationMode === 0 ? 'Авто' : 'С модерацией'}</li>
                    <li>Спам‑фильтр: {channel.enableSpamFilter ? 'Включён' : 'Выключен'}</li>
                    <li>Подписка: {channel.requireSubscription ? 'Требуется' : 'Не требуется'}</li>
                    <li>Футер: {channel.footerLinkText} → {channel.footerLinkUrl}</li>
                  </ul>
                </div>
              ))}
            </div>
          </div>
        )}

        {confirm && (
          <ConfirmDialog
            title={confirm.title}
            body={confirm.body}
            onCancel={() => setConfirm(null)}
            onConfirm={async () => {
              const action = confirm.onConfirm;
              setConfirm(null);
              await action();
            }}
          />
        )}
      </Layout>
    </ProtectedRoute>
  );
}
