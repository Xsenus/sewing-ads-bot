/**
 * Утилиты авторизации (JWT токен хранится в localStorage).
 */

const TOKEN_KEY = 'sewing_ads_admin_jwt';

/**
 * Получить JWT токен (если есть).
 */
export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

/**
 * Сохранить JWT токен.
 */
export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

/**
 * Очистить токен.
 */
export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY);
}

/**
 * Проверить, авторизован ли пользователь.
 */
export function isAuthed(): boolean {
  return !!getToken();
}
