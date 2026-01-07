import axios, { AxiosHeaders, type InternalAxiosRequestConfig } from 'axios'
import { boot } from 'quasar/wrappers'
import { getToken } from '../services/authToken'

export const api = axios.create({ baseURL: '/api' })

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getToken()
  if (!token) return config

  const headers = AxiosHeaders.from(config.headers)
  headers.set('Authorization', `Bearer ${token}`)
  config.headers = headers

  return config
})

export default boot(() => {})
