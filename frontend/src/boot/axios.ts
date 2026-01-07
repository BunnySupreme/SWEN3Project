import axios from 'axios'
import { boot } from 'quasar/wrappers'
import { getToken } from '../services/authToken'
import type { AxiosRequestHeaders } from 'axios'

export const api = axios.create({
  baseURL: '/api',
})

api.interceptors.request.use((config) => {
  const token = getToken()

  if (token) {
    config.headers = (config.headers ?? {}) as AxiosRequestHeaders
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

export default boot(() => {})
