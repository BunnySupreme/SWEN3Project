import { api } from 'boot/axios'
import { setToken, clearToken } from 'src/services/authToken'

export type RegisterRequest = {
  username: string
  password: string
}

export type LoginRequest = {
  username: string
  password: string
}

export type AuthResponse = {
  token: string
}

export async function register(req: RegisterRequest) {
  await api.post('/auth/register', req) // baseURL '/api' => /api/auth/register
}

export async function login(req: LoginRequest) {
  const { data } = await api.post<AuthResponse>('/auth/login', req)
  setToken(data.token)
  return data
}

export async function logout() {
  await api.post('/auth/logout')
  clearToken()
}
