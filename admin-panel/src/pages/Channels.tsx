import React, { useEffect, useMemo, useState } from 'react';
import Layout from '../components/Layout';
import ProtectedRoute from '../components/ProtectedRoute';
import ConfirmDialog from '../components/ConfirmDialog';
import {
  ChannelDto,
  createChannel,
  deactivateChannel,
  getChannels,
  pinChannel,
  unpinChannel,
  updateChannel,
} from '../api';

/**
 * Страница управления каналами.
 */
export default function ChannelsPage() {
  return (
    <ProtectedRoute>
      <Layout>
        <ChannelsContent />
      </Layout>
    </ProtectedRoute>
  );
}

function ChannelsContent() {
  const [list, setList] = useState<ChannelDto[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [msg, setMsg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [confirm, setConfirm] = useState<{
    title: string;
    body: string;
    confirmLabel?: string;
    tone?: 'primary' | 'danger';
    onConfirm: () => Promise<void>;
  } | null>(null);

  const selected = useMemo(
    () => list.find(x => x.id === selectedId) ?? null,
    [list, selectedId]
  );

  useEffect(() => {
    void load();
  }, []);

  async function load() {
    setError(null);
    try {
      const ch = await getChannels();
      setList(ch);
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка загрузки');
    }
  }

  async function onCreate() {
    setMsg(null);
    setError(null);

    const title = prompt('Название канала') ?? '';
    if (!title.trim()) return;

    const chatIdStr = prompt('TelegramChatId (например -100123...)') ?? '';
    const telegramChatId = Number(chatIdStr);
    if (!telegramChatId) {
      alert('Некорректный TelegramChatId');
      return;
    }

    const telegramUsername = (prompt('TelegramUsername (например sewing_industries) или пусто', '') ?? '').trim();

    const moderationModeStr = (prompt('Режим модерации: 0=Auto, 1=Moderation', '0') ?? '0').trim();
    const moderationMode = Number(moderationModeStr) || 0;

    const enableSpamFilter = (prompt('Спам-фильтр включен? (y/n)', 'y') ?? 'y').toLowerCase() === 'y';
    const spamFilterFreeOnly = (prompt('Спам-фильтр только для бесплатных? (y/n)', 'y') ?? 'y').toLowerCase() === 'y';

    const requireSubscription = (prompt('Требовать подписку? (y/n)', 'y') ?? 'y').toLowerCase() === 'y';
    const subscriptionChannelUsername = (prompt('Канал для проверки подписки (username без @)', 'sewing_industries') ?? 'sewing_industries').trim();

    const footerLinkText = (prompt('FooterLinkText', 'Швейные производства • Объявления') ?? 'Швейные производства • Объявления').trim();
    const footerLinkUrl = (prompt('FooterLinkUrl', 'https://t.me/sewing_industries') ?? 'https://t.me/sewing_industries').trim();

    try {
      await createChannel({
        title,
        telegramChatId,
        telegramUsername: telegramUsername || null,
        isActive: true,
        moderationMode,
        enableSpamFilter,
        spamFilterFreeOnly,
        requireSubscription,
        subscriptionChannelUsername: subscriptionChannelUsername || null,
        footerLinkText,
        footerLinkUrl,
        pinnedMessageId: null,
      });
      setMsg('Канал создан');
      await load();
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка создания');
    }
  }

  async function onSave() {
    if (!selected) return;

    const title = prompt('Название', selected.title) ?? selected.title;
    const chatIdStr = prompt('TelegramChatId', String(selected.telegramChatId)) ?? String(selected.telegramChatId);
    const telegramChatId = Number(chatIdStr) || selected.telegramChatId;
    const telegramUsername = (prompt('TelegramUsername (без @) или пусто', selected.telegramUsername ?? '') ?? (selected.telegramUsername ?? '')).trim();
    const isActive = (prompt('Активен? (y/n)', selected.isActive ? 'y' : 'n') ?? (selected.isActive ? 'y' : 'n')).toLowerCase() === 'y';
    const moderationModeStr = (prompt('Режим модерации: 0=Auto, 1=Moderation', String(selected.moderationMode)) ?? String(selected.moderationMode)).trim();
    const moderationMode = Number(moderationModeStr) || 0;

    const enableSpamFilter = (prompt('Спам-фильтр включен? (y/n)', selected.enableSpamFilter ? 'y' : 'n') ?? (selected.enableSpamFilter ? 'y' : 'n')).toLowerCase() === 'y';
    const spamFilterFreeOnly = (prompt('Спам-фильтр только для бесплатных? (y/n)', selected.spamFilterFreeOnly ? 'y' : 'n') ?? (selected.spamFilterFreeOnly ? 'y' : 'n')).toLowerCase() === 'y';

    const requireSubscription = (prompt('Требовать подписку? (y/n)', selected.requireSubscription ? 'y' : 'n') ?? (selected.requireSubscription ? 'y' : 'n')).toLowerCase() === 'y';
    const subscriptionChannelUsername = (prompt('Канал для проверки подписки (username без @)', selected.subscriptionChannelUsername ?? '') ?? (selected.subscriptionChannelUsername ?? '')).trim();

    const footerLinkText = (prompt('FooterLinkText', selected.footerLinkText) ?? selected.footerLinkText).trim();
    const footerLinkUrl = (prompt('FooterLinkUrl', selected.footerLinkUrl) ?? selected.footerLinkUrl).trim();

    setMsg(null);
    setError(null);

    try {
      await updateChannel(selected.id, {
        title,
        telegramChatId,
        telegramUsername: telegramUsername || null,
        isActive,
        moderationMode,
        enableSpamFilter,
        spamFilterFreeOnly,
        requireSubscription,
        subscriptionChannelUsername: subscriptionChannelUsername || null,
        footerLinkText,
        footerLinkUrl,
        pinnedMessageId: selected.pinnedMessageId ?? null,
      });
      setMsg('Канал обновлён');
      await load();
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка сохранения');
    }
  }

  async function onDeactivate() {
    if (!selected) return;
    setConfirm({
      title: 'Деактивировать канал',
      body: 'Канал перестанет принимать публикации и исчезнет из активных списков.',
      confirmLabel: 'Деактивировать',
      tone: 'danger',
      onConfirm: async () => {
        setMsg(null);
        setError(null);
        try {
          await deactivateChannel(selected.id);
          setSelectedId(null);
          setMsg('Канал деактивирован');
          await load();
        } catch (e: any) {
          setError(e?.message ?? 'Ошибка');
        }
      },
    });
  }

  async function onPin() {
    if (!selected) return;
    setMsg(null);
    setError(null);
    try {
      const res = await pinChannel(selected.id);
      setMsg(res.message);
      await load();
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка');
    }
  }

  async function onUnpin() {
    if (!selected) return;
    setMsg(null);
    setError(null);
    try {
      const res = await unpinChannel(selected.id);
      setMsg(res.message);
      await load();
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка');
    }
  }

  return (
    <>
      <h1>Каналы</h1>

      {error && <div className="card error">{error}</div>}
      {msg && <div className="card success">{msg}</div>}

      <div className="card flex">
        <button className="primary" onClick={onCreate}>+ Добавить канал</button>
        <button onClick={load}>Обновить</button>
      </div>

      <div className="card">
        <table className="table">
          <thead>
            <tr>
              <th>Название</th>
              <th>ChatId</th>
              <th>Username</th>
              <th>Режим</th>
              <th>Активен</th>
              <th>Закреп</th>
            </tr>
          </thead>
          <tbody>
            {list.map(ch => (
              <tr
                key={ch.id}
                style={{ cursor: 'pointer', background: ch.id === selectedId ? '#0b1220' : 'transparent' }}
                onClick={() => setSelectedId(ch.id)}
              >
                <td>{ch.title}</td>
                <td><code>{ch.telegramChatId}</code></td>
                <td><code>{ch.telegramUsername ?? ''}</code></td>
                <td>{ch.moderationMode === 1 ? 'Moderation' : 'Auto'}</td>
                <td>{ch.isActive ? '✅' : '—'}</td>
                <td>{ch.pinnedMessageId ? `#${ch.pinnedMessageId}` : '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {selected && (
        <div className="card">
          <h3>Выбрано: {selected.title}</h3>
          <div className="flex">
            <button className="primary" onClick={onSave}>Сохранить (через prompts)</button>
            <button onClick={onPin}>📌 Закрепить «ОПУБЛИКОВАТЬ»</button>
            <button onClick={onUnpin}>📍 Открепить</button>
            <button className="danger" onClick={onDeactivate}>Деактивировать</button>
          </div>
          <p className="muted" style={{ marginTop: 10 }}>
            Для закрепа/публикации бот должен быть админом канала.
          </p>
        </div>
      )}

      {!selected && (
        <div className="card muted">
          Выберите канал в таблице, чтобы закрепить кнопку или изменить настройки.
        </div>
      )}

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
    </>
  );
}
