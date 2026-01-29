import { getToken } from './auth';

/**
 * Базовый URL backend API.
 * Укажите VITE_API_URL в .env (например http://localhost:5000).
 */
const API_URL: string = (import.meta as any).env?.VITE_API_URL ?? 'http://localhost:5000';

/**
 * Ошибка API с текстом.
 */
export class ApiError extends Error {
  public status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

/**
 * Выполнить HTTP запрос к backend.
 */
async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken();

  const headers = new Headers(options.headers ?? {});
  headers.set('Content-Type', 'application/json');
  if (token) headers.set('Authorization', `Bearer ${token}`);

  const res = await fetch(`${API_URL}${path}`, {
    ...options,
    headers,
  });

  if (!res.ok) {
    let msg = res.statusText;
    try {
      const json = await res.json();
      msg = json?.message ?? JSON.stringify(json);
    } catch {
      // ignore
    }
    throw new ApiError(res.status, msg);
  }

  // 204 NoContent
  if (res.status === 204) {
    return undefined as T;
  }

  const text = await res.text();
  if (!text) return undefined as T;

  const contentType = res.headers.get('Content-Type') ?? '';
  if (contentType.includes('application/json')) {
    return JSON.parse(text) as T;
  }

  return text as T;
}

/**
 * DTO категории (как отдаёт backend).
 */
export type CategoryDto = {
  id: string;
  name: string;
  slug: string;
  parentId: string | null;
  sortOrder: number;
  isActive: boolean;
};

/**
 * DTO канала.
 */
export type ChannelDto = {
  id: string;
  title: string;
  telegramChatId: number;
  telegramUsername?: string | null;
  isActive: boolean;
  moderationMode: number; // 0 Auto, 1 Moderation
  enableSpamFilter: boolean;
  spamFilterFreeOnly: boolean;
  requireSubscription: boolean;
  subscriptionChannelUsername?: string | null;
  footerLinkText: string;
  footerLinkUrl: string;
  pinnedMessageId?: number | null;
};

/**
 * Настройка приложения.
 */
export type AppSetting = {
  id: string;
  key: string;
  value: string;
};

/**
 * Модерационная заявка.
 */
export type ModerationRequest = {
  id: string;
  adId: string;
  channelId: string;
  status: number; // Pending/Approved/Rejected
  rejectReason?: string | null;
  reviewedByTelegramUserId?: number | null;
  createdAtUtc: string;
  reviewedAtUtc?: string | null;
};

/**
 * Telegram модератор.
 */
export type TelegramAdmin = {
  id: string;
  telegramUserId: number;
  isActive: boolean;
  createdAtUtc: string;
};

/**
 * Пользователь веб‑админки.
 */
export type AdminAccount = {
  id: string;
  username: string;
  isActive: boolean;
  createdAtUtc: string;
};

/**
 * Логин и получение JWT.
 */
export async function login(username: string, password: string): Promise<string> {
  const res = await request<{ token: string }>('/api/admin/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username, password }),
  });
  return res.token;
}

// --- Categories ---

export async function getCategories(): Promise<CategoryDto[]> {
  return await request<CategoryDto[]>('/api/admin/categories');
}

export async function createCategory(payload: {
  name: string;
  parentId: string | null;
  sortOrder: number;
  isActive: boolean;
}): Promise<CategoryDto> {
  return await request<CategoryDto>('/api/admin/categories', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

export async function updateCategory(id: string, payload: {
  name: string;
  slug: string;
  parentId: string | null;
  sortOrder: number;
  isActive: boolean;
}): Promise<CategoryDto> {
  return await request<CategoryDto>(`/api/admin/categories/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  });
}

export async function deleteCategory(id: string): Promise<void> {
  await request<void>(`/api/admin/categories/${id}`, { method: 'DELETE' });
}

export async function getCategoryChannels(id: string): Promise<string[]> {
  return await request<string[]>(`/api/admin/categories/${id}/channels`);
}

export async function setCategoryChannels(id: string, channelIds: string[]): Promise<void> {
  await request<void>(`/api/admin/categories/${id}/channels`, {
    method: 'PUT',
    body: JSON.stringify({ channelIds }),
  });
}

// --- Channels ---

export async function getChannels(): Promise<ChannelDto[]> {
  return await request<ChannelDto[]>('/api/admin/channels');
}

export async function createChannel(payload: Omit<ChannelDto, 'id'>): Promise<ChannelDto> {
  return await request<ChannelDto>('/api/admin/channels', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

export async function updateChannel(id: string, payload: Omit<ChannelDto, 'id'>): Promise<ChannelDto> {
  return await request<ChannelDto>(`/api/admin/channels/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  });
}

export async function deactivateChannel(id: string): Promise<void> {
  await request<void>(`/api/admin/channels/${id}`, { method: 'DELETE' });
}

export async function pinChannel(id: string): Promise<{ message: string }> {
  return await request<{ message: string }>(`/api/admin/channels/${id}/pin`, { method: 'POST' });
}

export async function unpinChannel(id: string): Promise<{ message: string }> {
  return await request<{ message: string }>(`/api/admin/channels/${id}/unpin`, { method: 'POST' });
}

// --- Settings ---

export async function getSettings(): Promise<AppSetting[]> {
  return await request<AppSetting[]>('/api/admin/settings');
}

export async function setSetting(key: string, value: string): Promise<void> {
  await request<void>('/api/admin/settings', {
    method: 'PUT',
    body: JSON.stringify({ key, value }),
  });
}

// --- Telegram admins ---

export async function getTelegramAdmins(): Promise<TelegramAdmin[]> {
  return await request<TelegramAdmin[]>('/api/admin/telegram-admins');
}

export async function addTelegramAdmin(telegramUserId: number): Promise<void> {
  await request<void>('/api/admin/telegram-admins', {
    method: 'POST',
    body: JSON.stringify({ telegramUserId }),
  });
}

export async function deactivateTelegramAdmin(telegramUserId: number): Promise<void> {
  await request<void>(`/api/admin/telegram-admins/${telegramUserId}`, { method: 'DELETE' });
}

// --- Moderation ---

export async function getModerationRequests(status?: number): Promise<ModerationRequest[]> {
  const qs = status === undefined ? '' : `?status=${status}`;
  return await request<ModerationRequest[]>(`/api/admin/moderation/requests${qs}`);
}

export async function getModerationPreview(id: string): Promise<string> {
  return await request<string>(`/api/admin/moderation/requests/${id}/preview`);
}

export async function approveModeration(id: string): Promise<{ message: string }> {
  return await request<{ message: string }>(`/api/admin/moderation/requests/${id}/approve`, { method: 'POST' });
}

export async function rejectModeration(id: string, reason?: string): Promise<{ message: string }> {
  return await request<{ message: string }>(`/api/admin/moderation/requests/${id}/reject`, {
    method: 'POST',
    body: JSON.stringify({ reason }),
  });
}

// --- Admin accounts ---

/**
 * Получить пользователей админки.
 */
export async function getAdminAccounts(): Promise<AdminAccount[]> {
  return await request<AdminAccount[]>('/api/admin/admin-accounts');
}

/**
 * Создать пользователя админки.
 */
export async function createAdminAccount(payload: {
  username: string;
  password: string;
  isActive: boolean;
}): Promise<void> {
  await request<void>('/api/admin/admin-accounts', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

/**
 * Сбросить пароль пользователя админки.
 */
export async function resetAdminPassword(id: string, password: string): Promise<void> {
  await request<void>(`/api/admin/admin-accounts/${id}/reset-password`, {
    method: 'POST',
    body: JSON.stringify({ password }),
  });
}

/**
 * Включить/выключить пользователя админки.
 */
export async function setAdminAccountActive(id: string, isActive: boolean): Promise<void> {
  await request<void>(`/api/admin/admin-accounts/${id}/set-active`, {
    method: 'POST',
    body: JSON.stringify({ isActive }),
  });
}

export function getApiUrl(): string {
  return API_URL;
}
