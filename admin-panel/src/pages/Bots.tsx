import React, { useEffect, useMemo, useState } from 'react';
import Layout from '../components/Layout';
import ProtectedRoute from '../components/ProtectedRoute';
import ConfirmDialog from '../components/ConfirmDialog';
import {
  BotChannel,
  BotChat,
  TelegramBot,
  createBot,
  deleteBot,
  disableBot,
  getBotChannels,
  getBotChats,
  getBots,
  pauseBot,
  refreshBot,
  restartBot,
  resumeBot,
  getApiUrl,
  updateBotProfile,
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
  const [chats, setChats] = useState<BotChat[]>([]);
  const [activeBotId, setActiveBotId] = useState<string | null>(null);
  const [token, setToken] = useState('');
  const [cloneFromBotId, setCloneFromBotId] = useState<string>('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [photoUrl, setPhotoUrl] = useState<string | null>(null);
  const [photoKey, setPhotoKey] = useState(0);
  const [profileName, setProfileName] = useState('');
  const [profileDescription, setProfileDescription] = useState('');
  const [profileShortDescription, setProfileShortDescription] = useState('');
  const [trackMessagesEnabled, setTrackMessagesEnabled] = useState(true);
  const [commands, setCommands] = useState<Array<{ command: string; description: string }>>([]);
  const [photoFile, setPhotoFile] = useState<File | null>(null);
  const [confirm, setConfirm] = useState<{
    title: string;
    body: string;
    onConfirm: () => Promise<void>;
    confirmLabel?: string;
    tone?: 'primary' | 'danger';
  } | null>(null);

  const activeBot = useMemo(() => bots.find((b) => b.id === activeBotId) ?? null, [bots, activeBotId]);
  const chatBuckets = useMemo(() => {
    const channels: BotChat[] = [];
    const chatsList: BotChat[] = [];
    chats.forEach((chat) => {
      const type = chat.chatType.toLowerCase();
      if (type === 'channel') {
        channels.push(chat);
      } else {
        chatsList.push(chat);
      }
    });
    return { channels, chats: chatsList };
  }, [chats]);
  const defaultAvatar =
    'data:image/svg+xml;utf8,' +
    encodeURIComponent(
      `<svg xmlns="http://www.w3.org/2000/svg" width="96" height="96" viewBox="0 0 96 96">
        <defs>
          <linearGradient id="g" x1="0" x2="1" y1="0" y2="1">
            <stop offset="0%" stop-color="#60a5fa"/>
            <stop offset="100%" stop-color="#22d3ee"/>
          </linearGradient>
        </defs>
        <rect width="96" height="96" rx="20" fill="url(#g)"/>
        <circle cx="34" cy="40" r="8" fill="#0f172a"/>
        <circle cx="62" cy="40" r="8" fill="#0f172a"/>
        <rect x="28" y="58" width="40" height="10" rx="5" fill="#0f172a"/>
      </svg>`,
    );

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

  async function loadChats(botId: string) {
    try {
      setChats([]);
      const data = await getBotChats(botId);
      setChats(data);
    } catch (e: any) {
      setError(e?.message ?? 'Не удалось загрузить чаты бота.');
    }
  }

  useEffect(() => {
    void loadBots();
  }, []);

  useEffect(() => {
    if (activeBotId) {
      void loadChannels(activeBotId);
      void loadChats(activeBotId);
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

  useEffect(() => {
    if (!activeBot) return;
    setProfileName(activeBot.name ?? '');
    setProfileDescription(activeBot.description ?? '');
    setProfileShortDescription(activeBot.shortDescription ?? '');
    setTrackMessagesEnabled(activeBot.trackMessagesEnabled ?? true);
    setCommands(
      activeBot.commands?.map((cmd) => ({
        command: cmd.command ?? '',
        description: cmd.description ?? '',
      })) ?? [],
    );
    setPhotoFile(null);
  }, [activeBot]);

  const openConfirm = (
    title: string,
    body: string,
    action: () => Promise<void>,
    options?: { confirmLabel?: string; tone?: 'primary' | 'danger' },
  ) => {
    setConfirm({ title, body, onConfirm: action, ...options });
  };

  const handleAdd = async () => {
    try {
      setLoading(true);
      setError(null);
      setMessage(null);
      const newBot = await createBot(token.trim(), cloneFromBotId || undefined);
      setBots((prev) => [newBot, ...prev]);
      setToken('');
      setCloneFromBotId('');
      setActiveBotId(newBot.id);
      setMessage('Бот добавлен и запущен.');
    } catch (e: any) {
      setError(e?.message ?? 'Не удалось добавить бота.');
    } finally {
      setLoading(false);
    }
  };

  const handleProfileSave = async () => {
    if (!activeBot) return;
    try {
      setLoading(true);
      setError(null);
      setMessage(null);
      const commandsJson = JSON.stringify(
        commands
          .filter((cmd) => cmd.command.trim())
          .map((cmd) => ({ command: cmd.command.trim(), description: cmd.description.trim() })),
      );

      const updated = await updateBotProfile(activeBot.id, {
        name: profileName,
        description: profileDescription,
        shortDescription: profileShortDescription,
        commandsJson,
        trackMessagesEnabled,
        photo: photoFile,
      });
      setBots((prev) => prev.map((bot) => (bot.id === updated.id ? updated : bot)));
      setPhotoKey((prev) => prev + 1);
      setPhotoFile(null);
      setMessage('Профиль бота обновлён.');
    } catch (e: any) {
      setError(e?.message ?? 'Не удалось обновить профиль.');
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
        if (activeBotId) {
          await loadChannels(activeBotId);
          await loadChats(activeBotId);
        }
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

        <div className="bots-layout">
          <div className="bots-aside">
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
              <div className="row">
                <label>Скопировать настройки</label>
                <select value={cloneFromBotId} onChange={(e) => setCloneFromBotId(e.target.value)}>
                  <option value="">Не копировать</option>
                  {bots.map((bot) => (
                    <option key={bot.id} value={bot.id}>
                      {bot.name ?? 'Без имени'} (@{bot.username ?? 'unknown'})
                    </option>
                  ))}
                </select>
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

          <div className="bots-main">
            {activeBot ? (
              <>
                <div className="card">
                  <div className="bot-profile">
                    <img
                      src={photoUrl ?? defaultAvatar}
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
                  <div className="bot-actions">
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
                          await loadChats(activeBot.id);
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
                        openConfirm(
                          'Удалить',
                          'Удалить бота и остановить его работу?',
                          () => handleAction(() => deleteBot(activeBot.id), 'Бот удалён.'),
                          { confirmLabel: 'Удалить', tone: 'danger' },
                        )
                      }
                    >
                      Удалить
                    </button>
                  </div>
                </div>

                <div className="bot-details-grid">
                  <div className="card">
                    <h3>Профиль и команды</h3>
                    <div className="row">
                      <label>Имя бота</label>
                      <input value={profileName} onChange={(e) => setProfileName(e.target.value)} />
                    </div>
                    <div className="row">
                      <label>Описание</label>
                      <textarea value={profileDescription} onChange={(e) => setProfileDescription(e.target.value)} />
                    </div>
                    <div className="row">
                      <label>Короткое описание</label>
                      <textarea value={profileShortDescription} onChange={(e) => setProfileShortDescription(e.target.value)} />
                    </div>
                    <div className="row">
                      <label>Фото</label>
                      <input type="file" accept="image/*" onChange={(e) => setPhotoFile(e.target.files?.[0] ?? null)} />
                    </div>
                    <div className="row">
                      <label>Отслеживание сообщений</label>
                      <label className="toggle">
                        <input
                          type="checkbox"
                          checked={trackMessagesEnabled}
                          onChange={(e) => setTrackMessagesEnabled(e.target.checked)}
                        />
                        <span>{trackMessagesEnabled ? 'Включено' : 'Выключено'}</span>
                      </label>
                    </div>
                    <div className="card nested">
                      <h4>Команды/кнопки</h4>
                      {commands.length === 0 && <p className="muted">Команды не заданы.</p>}
                      {commands.map((cmd, idx) => (
                        <div key={`${cmd.command}-${idx}`} className="command-row">
                          <input
                            placeholder="/command"
                            value={cmd.command}
                            onChange={(e) => {
                              const next = [...commands];
                              next[idx] = { ...next[idx], command: e.target.value };
                              setCommands(next);
                            }}
                          />
                          <input
                            placeholder="Описание"
                            value={cmd.description}
                            onChange={(e) => {
                              const next = [...commands];
                              next[idx] = { ...next[idx], description: e.target.value };
                              setCommands(next);
                            }}
                          />
                          <button
                            className="ghost"
                            onClick={() => setCommands((prev) => prev.filter((_, i) => i !== idx))}
                          >
                            Удалить
                          </button>
                        </div>
                      ))}
                      <button className="ghost" onClick={() => setCommands((prev) => [...prev, { command: '', description: '' }])}>
                        Добавить команду
                      </button>
                    </div>
                    <div className="flex">
                      <button className="primary" onClick={handleProfileSave} disabled={loading}>
                        Сохранить профиль
                      </button>
                    </div>
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

                  <div className="card">
                    <h3>Чаты и каналы с активностью</h3>
                    <p className="muted">Показываем чаты/каналы, где бот получает сообщения или посты.</p>
                    {chats.length === 0 && <p className="muted">Пока нет активности.</p>}

                    {chatBuckets.channels.length > 0 && (
                      <div className="chat-group">
                        <h4>Каналы</h4>
                        {chatBuckets.channels.map((chat) => (
                          <div key={`${chat.chatId}-${chat.chatType}`} className="chat-card">
                            <div className="chat-header">
                              <strong>{chat.chatTitle ?? 'Без названия'}</strong>
                              <span className="badge">Канал</span>
                            </div>
                            <p className="muted">
                              ID: {chat.chatId} • Последнее сообщение: {new Date(chat.lastMessageAtUtc).toLocaleString()}
                            </p>
                          </div>
                        ))}
                      </div>
                    )}

                    {chatBuckets.chats.length > 0 && (
                      <div className="chat-group">
                        <h4>Чаты</h4>
                        {chatBuckets.chats.map((chat) => {
                          const type = chat.chatType.toLowerCase();
                          const typeLabel = type === 'supergroup'
                            ? 'Супергруппа'
                            : type === 'group'
                              ? 'Группа'
                              : 'Личный чат';
                          return (
                            <div key={`${chat.chatId}-${chat.chatType}`} className="chat-card">
                              <div className="chat-header">
                                <strong>{chat.chatTitle ?? 'Без названия'}</strong>
                                <span className="badge">{typeLabel}</span>
                              </div>
                              <p className="muted">
                                ID: {chat.chatId} • Последнее сообщение: {new Date(chat.lastMessageAtUtc).toLocaleString()}
                              </p>
                            </div>
                          );
                        })}
                      </div>
                    )}
                  </div>
                </div>
              </>
            ) : (
              <div className="card muted">
                Выберите бота слева, чтобы увидеть настройки и активность.
              </div>
            )}
          </div>
        </div>

        {confirm && (
          <ConfirmDialog
            title={confirm.title}
            body={confirm.body}
            confirmLabel={confirm.confirmLabel}
            tone={confirm.tone}
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
