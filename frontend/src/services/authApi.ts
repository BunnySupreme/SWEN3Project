import { api } from '../boot/axios'
import { setToken, clearToken } from './authToken'

export type RegisterRequest = {
  username: string
  password: string
}

export type AuthResponse = {
  token: string
}

export async function register(req: RegisterRequest) {
  await api.post('/auth/register', req)
}

export async function login(req: { username: string; password: string }) {
  const { data } = await api.post<AuthResponse>('/auth/login', req)

  if (!data || typeof data.token !== 'string' || data.token.trim() === '') {
    throw new Error('Login succeeded but no token was returned (check AuthResponse field name).')
  }

  setToken(data.token)
  return data
}
export async function logout() {
  await api.post('/auth/logout')
  clearToken()
}
