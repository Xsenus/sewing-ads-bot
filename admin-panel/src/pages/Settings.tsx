import React, { useEffect, useState } from 'react';
import Layout from '../components/Layout';
import ProtectedRoute from '../components/ProtectedRoute';
import { AppSetting, getSettings, setSetting } from '../api';

/**
 * Страница глобальных настроек.
 */
export default function SettingsPage() {
  return (
    <ProtectedRoute>
      <Layout>
        <SettingsContent />
      </Layout>
    </ProtectedRoute>
  );
}

function SettingsContent() {
  const [list, setList] = useState<AppSetting[]>([]);
  const [quick, setQuick] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [msg, setMsg] = useState<string | null>(null);

  useEffect(() => {
    void load();
  }, []);

  async function load() {
    setError(null);
    setMsg(null);
    try {
      const s = await getSettings();
      setList(s);

      // Заполняем форму быстрых настроек, чтобы админ мог управлять важными параметрами
      // (токены, лимиты, цены) без необходимости помнить точные ключи.
      const dict: Record<string, string> = {};
      for (const item of s) dict[item.key] = item.value;

      setQuick(prev => ({
        ...prev,
        'Telegram.BotToken': dict['Telegram.BotToken'] ?? '',
        'Telegram.PaymentProviderToken': dict['Telegram.PaymentProviderToken'] ?? '',
        'App.TelegraphTariffsUrl': dict['App.TelegraphTariffsUrl'] ?? '',
        'App.GlobalRequiredSubscriptionChannel': dict['App.GlobalRequiredSubscriptionChannel'] ?? '',
        'App.RequiredSubscriptionChannels': dict['App.RequiredSubscriptionChannels'] ?? '',
        'App.EnableReferralProgram': dict['App.EnableReferralProgram'] ?? '',
        'App.ReferralRewardPercent': dict['App.ReferralRewardPercent'] ?? '',
        'Ads.EnableFreeAds': dict['Ads.EnableFreeAds'] ?? '',
        'Ads.FreeAllowMedia': dict['Ads.FreeAllowMedia'] ?? '',
        'Ads.ForbidLinksInFree': dict['Ads.ForbidLinksInFree'] ?? '',
        'Publication.ModerationMode': dict['Publication.ModerationMode'] ?? '',
        'Post.IncludeLocationTags': dict['Post.IncludeLocationTags'] ?? '',
        'Post.IncludeCategoryTag': dict['Post.IncludeCategoryTag'] ?? '',
        'Post.IncludeFooterLink': dict['Post.IncludeFooterLink'] ?? '',
        'Location.InputMode': dict['Location.InputMode'] ?? '',
        'Location.Countries': dict['Location.Countries'] ?? '',
        'Location.CitiesMap': dict['Location.CitiesMap'] ?? '',
        'Limits.FreeAdsPerCalendarDay': dict['Limits.FreeAdsPerCalendarDay'] ?? '',
        'Limits.FreeAdsPerPeriod': dict['Limits.FreeAdsPerPeriod'] ?? '',
        'Limits.FreeAdsPeriod': dict['Limits.FreeAdsPeriod'] ?? '',
        'Limits.TitleMax': dict['Limits.TitleMax'] ?? '',
        'Limits.TextMax': dict['Limits.TextMax'] ?? '',
        'Limits.ContactsMax': dict['Limits.ContactsMax'] ?? '',
        'PaidAdPriceMinor': dict['PaidAdPriceMinor'] ?? '',
        'BumpPriceMinor': dict['BumpPriceMinor'] ?? '',
      }));
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка загрузки');
    }
  }

  async function onEdit(key: string, current: string) {
    const value = prompt(`Значение для ${key}`, current) ?? current;
    setError(null);
    setMsg(null);
    try {
      await setSetting(key, value);
      setMsg('Сохранено');
      await load();
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка');
    }
  }

  return (
    <>
      <h1>Настройки</h1>

      <div className="card muted">
        Здесь редактируются настройки из таблицы <code>AppSettings</code>.
        <br />
        Важные ключи, которые использует проект:
        <ul style={{ marginTop: 8 }}>
          <li><code>Telegram.BotToken</code> — токен бота (применяется автоматически)</li>
          <li><code>Telegram.PaymentProviderToken</code> — токен провайдера платежей</li>
          <li><code>PaidAdPriceMinor</code>, <code>BumpPriceMinor</code> — цены платных услуг</li>
          <li><code>Limits.FreeAdsPerCalendarDay</code> — лимит бесплатных объявлений в календарный день</li>
          <li><code>Limits.FreeAdsPerPeriod</code>, <code>Limits.FreeAdsPeriod</code> — расширенная настройка лимита</li>
          <li><code>Limits.TitleMax</code>, <code>Limits.TextMax</code>, <code>Limits.ContactsMax</code> — лимиты длины</li>
          <li><code>Ads.EnableFreeAds</code>, <code>Ads.FreeAllowMedia</code>, <code>Ads.ForbidLinksInFree</code> — ограничения бесплатных объявлений</li>
          <li><code>Publication.ModerationMode</code> — Auto или Moderation</li>
          <li><code>Post.IncludeLocationTags</code>, <code>Post.IncludeCategoryTag</code>, <code>Post.IncludeFooterLink</code> — шаблон поста</li>
          <li><code>Location.InputMode</code>, <code>Location.Countries</code>, <code>Location.CitiesMap</code> — выбор страны/города</li>
          <li><code>App.TelegraphTariffsUrl</code> — ссылка на telegra.ph с тарифами</li>
          <li><code>App.GlobalRequiredSubscriptionChannel</code> — общий канал для обязательной подписки</li>
          <li><code>App.RequiredSubscriptionChannels</code> — список обязательных каналов (через запятую)</li>
          <li><code>App.EnableReferralProgram</code>, <code>App.ReferralRewardPercent</code> — реферальная программа</li>
        </ul>
      </div>

      <div className="card">
        <h3 style={{ marginTop: 0 }}>Быстрые настройки</h3>
        <p className="muted" style={{ marginTop: 0 }}>
          Эти поля сохраняют значения в <code>AppSettings</code>. Настройки хранятся в базе данных и
          используются ботом как источник истины.
        </p>

        <div className="grid" style={{ gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <div>
            <label>Telegram.BotToken</label>
            <input
              type="password"
              value={quick['Telegram.BotToken'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Telegram.BotToken': e.target.value }))}
              placeholder="123456:AA..."
            />
          </div>
          <div>
            <label>Telegram.PaymentProviderToken</label>
            <input
              type="password"
              value={quick['Telegram.PaymentProviderToken'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Telegram.PaymentProviderToken': e.target.value }))}
              placeholder="381764678:TEST:..."
            />
          </div>

          <div>
            <label>App.TelegraphTariffsUrl</label>
            <input
              value={quick['App.TelegraphTariffsUrl'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'App.TelegraphTariffsUrl': e.target.value }))}
              placeholder="https://telegra.ph/..."
            />
          </div>
          <div>
            <label>App.GlobalRequiredSubscriptionChannel</label>
            <input
              value={quick['App.GlobalRequiredSubscriptionChannel'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'App.GlobalRequiredSubscriptionChannel': e.target.value }))}
              placeholder="sewing_industries"
            />
          </div>
          <div>
            <label>App.RequiredSubscriptionChannels</label>
            <input
              value={quick['App.RequiredSubscriptionChannels'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'App.RequiredSubscriptionChannels': e.target.value }))}
              placeholder="sewing_industries,another_channel"
            />
          </div>

          <div>
            <label>Ads.EnableFreeAds</label>
            <input
              value={quick['Ads.EnableFreeAds'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Ads.EnableFreeAds': e.target.value }))}
              placeholder="true/false"
            />
          </div>
          <div>
            <label>Ads.FreeAllowMedia</label>
            <input
              value={quick['Ads.FreeAllowMedia'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Ads.FreeAllowMedia': e.target.value }))}
              placeholder="true/false"
            />
          </div>
          <div>
            <label>Ads.ForbidLinksInFree</label>
            <input
              value={quick['Ads.ForbidLinksInFree'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Ads.ForbidLinksInFree': e.target.value }))}
              placeholder="true/false"
            />
          </div>
          <div>
            <label>Publication.ModerationMode</label>
            <input
              value={quick['Publication.ModerationMode'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Publication.ModerationMode': e.target.value }))}
              placeholder="Auto / Moderation"
            />
          </div>

          <div>
            <label>Post.IncludeLocationTags</label>
            <input
              value={quick['Post.IncludeLocationTags'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Post.IncludeLocationTags': e.target.value }))}
              placeholder="true/false"
            />
          </div>
          <div>
            <label>Post.IncludeCategoryTag</label>
            <input
              value={quick['Post.IncludeCategoryTag'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Post.IncludeCategoryTag': e.target.value }))}
              placeholder="true/false"
            />
          </div>
          <div>
            <label>Post.IncludeFooterLink</label>
            <input
              value={quick['Post.IncludeFooterLink'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Post.IncludeFooterLink': e.target.value }))}
              placeholder="true/false"
            />
          </div>
          <div>
            <label>Location.InputMode</label>
            <input
              value={quick['Location.InputMode'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Location.InputMode': e.target.value }))}
              placeholder="Manual / Keyboard"
            />
          </div>
          <div>
            <label>Location.Countries</label>
            <textarea
              value={quick['Location.Countries'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Location.Countries': e.target.value }))}
              placeholder='["Россия","Казахстан"] или Россия, Казахстан'
              rows={2}
            />
          </div>
          <div>
            <label>Location.CitiesMap</label>
            <textarea
              value={quick['Location.CitiesMap'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Location.CitiesMap': e.target.value }))}
              placeholder='{"Россия":["Москва","СПб"],"Казахстан":["Алматы"]}'
              rows={3}
            />
          </div>

          <div>
            <label>Limits.FreeAdsPerCalendarDay</label>
            <input
              value={quick['Limits.FreeAdsPerCalendarDay'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Limits.FreeAdsPerCalendarDay': e.target.value }))}
              placeholder="1"
            />
          </div>
          <div>
            <label>Limits.FreeAdsPerPeriod</label>
            <input
              value={quick['Limits.FreeAdsPerPeriod'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Limits.FreeAdsPerPeriod': e.target.value }))}
              placeholder="1"
            />
          </div>
          <div>
            <label>Limits.FreeAdsPeriod</label>
            <input
              value={quick['Limits.FreeAdsPeriod'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Limits.FreeAdsPeriod': e.target.value }))}
              placeholder="Day / Week / Month"
            />
          </div>
          <div>
            <label>PaidAdPriceMinor</label>
            <input
              value={quick['PaidAdPriceMinor'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'PaidAdPriceMinor': e.target.value }))}
              placeholder="20000 (=200.00 RUB)"
            />
          </div>

          <div>
            <label>BumpPriceMinor</label>
            <input
              value={quick['BumpPriceMinor'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'BumpPriceMinor': e.target.value }))}
              placeholder="5000 (=50.00 RUB)"
            />
          </div>
          <div>
            <label>App.EnableReferralProgram</label>
            <input
              value={quick['App.EnableReferralProgram'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'App.EnableReferralProgram': e.target.value }))}
              placeholder="true/false"
            />
          </div>

          <div>
            <label>App.ReferralRewardPercent</label>
            <input
              value={quick['App.ReferralRewardPercent'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'App.ReferralRewardPercent': e.target.value }))}
              placeholder="10"
            />
          </div>
          <div>
            <label>Limits.TitleMax</label>
            <input
              value={quick['Limits.TitleMax'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Limits.TitleMax': e.target.value }))}
              placeholder="150"
            />
          </div>

          <div>
            <label>Limits.TextMax</label>
            <input
              value={quick['Limits.TextMax'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Limits.TextMax': e.target.value }))}
              placeholder="1000"
            />
          </div>
          <div>
            <label>Limits.ContactsMax</label>
            <input
              value={quick['Limits.ContactsMax'] ?? ''}
              onChange={e => setQuick(q => ({ ...q, 'Limits.ContactsMax': e.target.value }))}
              placeholder="200"
            />
          </div>
        </div>

        <div style={{ marginTop: 12 }}>
          <button
            className="primary"
            onClick={async () => {
              setError(null);
              setMsg(null);
              try {
                // Сохраняем только те ключи, которые присутствуют в quick.
                for (const [k, v] of Object.entries(quick)) {
                  await setSetting(k, v);
                }
                setMsg('Сохранено');
                await load();
              } catch (e: any) {
                setError(e?.message ?? 'Ошибка');
              }
            }}
          >
            Сохранить быстрые настройки
          </button>
        </div>
      </div>

      {error && <div className="card error">{error}</div>}
      {msg && <div className="card success">{msg}</div>}

      <div className="card flex">
        <button onClick={load}>Обновить</button>
        <button
          className="primary"
          onClick={() => {
            const k = prompt('Ключ (например App.PaidAdPriceMinor)');
            if (!k) return;
            onEdit(k, '');
          }}
        >
          + Добавить/обновить ключ
        </button>
      </div>

      <div className="card">
        <table className="table">
          <thead>
            <tr>
              <th>Key</th>
              <th>Value</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {list.map(s => (
              <tr key={s.id}>
                <td><code>{s.key}</code></td>
                <td style={{ whiteSpace: 'pre-wrap' }}>{s.value}</td>
                <td>
                  <button onClick={() => onEdit(s.key, s.value)}>Изменить</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
