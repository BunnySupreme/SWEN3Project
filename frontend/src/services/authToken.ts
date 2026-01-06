import { LocalStorage } from 'quasar'

const KEY = 'auth_token'

export function getToken(): string | null {
  return LocalStorage.getItem<string>(KEY) ?? null
}

export function setToken(token: string) {
  LocalStorage.set(KEY, token)
}

export function clearToken() {
  LocalStorage.remove(KEY)
}
