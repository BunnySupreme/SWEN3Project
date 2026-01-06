import axios from 'axios'
import { boot } from 'quasar/wrappers'
import { getToken } from 'src/services/authToken'

export const api = axios.create({
  baseURL: '/api',
})

api.interceptors.request.use((config) => {
  const token = getToken()

  if (token) {
    config.headers = config.headers ?? {}
    config.headers.Authorization = `Bearer ${token}`
  }

  // TEMP DEBUG:
  console.log('REQ', config.method, config.url, 'auth?', !!token)

  return config
})

export default boot(() => {})
