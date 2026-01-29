import React, { useEffect, useMemo, useState } from 'react';
import Layout from '../components/Layout';
import ProtectedRoute from '../components/ProtectedRoute';
import {
  CategoryDto,
  ChannelDto,
  createCategory,
  deleteCategory,
  getCategories,
  getCategoryChannels,
  getChannels,
  setCategoryChannels,
  updateCategory,
} from '../api';

/**
 * Страница управления категориями.
 */
export default function CategoriesPage() {
  return (
    <ProtectedRoute>
      <Layout>
        <CategoriesContent />
      </Layout>
    </ProtectedRoute>
  );
}

function CategoriesContent() {
  const [cats, setCats] = useState<CategoryDto[]>([]);
  const [channels, setChannelsState] = useState<ChannelDto[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [msg, setMsg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const selected = useMemo(
    () => cats.find(c => c.id === selectedId) ?? null,
    [cats, selectedId]
  );

  useEffect(() => {
    void load();
  }, []);

  async function load() {
    setError(null);
    try {
      const [c, ch] = await Promise.all([getCategories(), getChannels()]);
      setCats(c);
      setChannelsState(ch);
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка загрузки');
    }
  }

  async function onCreate() {
    setMsg(null);
    setError(null);

    const name = prompt('Название категории');
    if (!name) return;

    const sortOrderStr = prompt('SortOrder (число)', '0') ?? '0';
    const sortOrder = Number(sortOrderStr) || 0;

    const parentId = prompt('ParentId (GUID) или пусто', '') ?? '';

    try {
      await createCategory({
        name,
        parentId: parentId.trim() ? parentId.trim() : null,
        sortOrder,
        isActive: true,
      });
      setMsg('Категория создана');
      await load();
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка создания');
    }
  }

  async function onSave() {
    if (!selected) return;
    setMsg(null);
    setError(null);

    const name = prompt('Название', selected.name) ?? selected.name;
    const slug = prompt('Slug (хэштег)', selected.slug) ?? selected.slug;
    const parentId = prompt('ParentId (GUID) или пусто', selected.parentId ?? '') ?? (selected.parentId ?? '');
    const sortOrderStr = prompt('SortOrder (число)', String(selected.sortOrder)) ?? String(selected.sortOrder);
    const sortOrder = Number(sortOrderStr) || 0;
    const isActive = (prompt('Активна? (y/n)', selected.isActive ? 'y' : 'n') ?? (selected.isActive ? 'y' : 'n')).toLowerCase() === 'y';

    try {
      await updateCategory(selected.id, {
        name,
        slug,
        parentId: parentId.trim() ? parentId.trim() : null,
        sortOrder,
        isActive,
      });
      setMsg('Категория обновлена');
      await load();
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка сохранения');
    }
  }

  async function onDelete() {
    if (!selected) return;
    if (!confirm('Деактивировать категорию?')) return;

    setMsg(null);
    setError(null);

    try {
      await deleteCategory(selected.id);
      setSelectedId(null);
      setMsg('Категория деактивирована');
      await load();
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка удаления');
    }
  }

  return (
    <>
      <h1>Категории</h1>

      {error && <div className="card error">{error}</div>}
      {msg && <div className="card success">{msg}</div>}

      <div className="card flex">
        <button className="primary" onClick={onCreate}>+ Добавить категорию</button>
        <button onClick={load}>Обновить</button>
      </div>

      <div className="card">
        <table className="table">
          <thead>
            <tr>
              <th>Название</th>
              <th>Slug</th>
              <th>ParentId</th>
              <th>Sort</th>
              <th>Активна</th>
            </tr>
          </thead>
          <tbody>
            {cats.map(c => (
              <tr
                key={c.id}
                style={{ cursor: 'pointer', background: c.id === selectedId ? '#0b1220' : 'transparent' }}
                onClick={() => setSelectedId(c.id)}
              >
                <td>{c.name}</td>
                <td><code>{c.slug}</code></td>
                <td><code>{c.parentId ?? ''}</code></td>
                <td>{c.sortOrder}</td>
                <td>{c.isActive ? '✅' : '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {selected && (
        <div className="card">
          <h3>Выбрано: {selected.name}</h3>
          <div className="flex">
            <button className="primary" onClick={onSave}>Сохранить (через prompts)</button>
            <button className="danger" onClick={onDelete}>Деактивировать</button>
          </div>
          <hr />
          <CategoryChannels categoryId={selected.id} channels={channels} />
        </div>
      )}

      {!selected && (
        <div className="card muted">
          Выберите категорию в таблице, чтобы назначить ей каналы публикации.
        </div>
      )}
    </>
  );
}

function CategoryChannels(props: { categoryId: string; channels: ChannelDto[] }) {
  const { categoryId, channels } = props;
  const [loading, setLoading] = useState(false);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [msg, setMsg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [categoryId]);

  async function load() {
    setMsg(null);
    setError(null);
    setLoading(true);
    try {
      const ids = await getCategoryChannels(categoryId);
      setSelected(new Set(ids));
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка загрузки маппингов');
    } finally {
      setLoading(false);
    }
  }

  async function onSave() {
    setMsg(null);
    setError(null);
    setLoading(true);
    try {
      await setCategoryChannels(categoryId, Array.from(selected));
      setMsg('Каналы назначены. Если у категории нет прямых каналов — бот делает fallback к родителю.');
    } catch (e: any) {
      setError(e?.message ?? 'Ошибка сохранения');
    } finally {
      setLoading(false);
    }
  }

  const activeChannels = useMemo(
    () => [...channels].sort((a, b) => a.title.localeCompare(b.title)),
    [channels]
  );

  return (
    <>
      <h4>Каналы для категории (вариант C: публикация в несколько каналов)</h4>
      <p className="muted">
        Если поставить галочки — объявление будет публиковаться/модерироваться отдельно в каждом выбранном канале.
      </p>

      {error && <div className="error">{error}</div>}
      {msg && <div className="success">{msg}</div>}

      <div className="card" style={{ background: '#0b0e12' }}>
        {activeChannels.map(ch => (
          <label key={ch.id} style={{ display: 'block', marginBottom: 8 }}>
            <input
              type="checkbox"
              checked={selected.has(ch.id)}
              onChange={(e) => {
                const next = new Set(selected);
                if (e.target.checked) next.add(ch.id);
                else next.delete(ch.id);
                setSelected(next);
              }}
            />{' '}
            {ch.title} <span className="muted">({ch.telegramUsername ?? ch.telegramChatId})</span>
          </label>
        ))}
      </div>

      <div className="flex">
        <button onClick={load} disabled={loading}>Обновить</button>
        <button className="primary" onClick={onSave} disabled={loading}>Сохранить</button>
      </div>
    </>
  );
}
