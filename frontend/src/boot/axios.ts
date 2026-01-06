import { boot } from 'quasar/wrappers'
import axios from 'axios'
import { getToken, clearToken } from 'src/services/authToken'

export const api = axios.create({
  baseURL: '/api' // nginx proxy
})

// Attach Authorization header to every request if token exists
api.interceptors.request.use((config) => {
  const token = getToken()
  if (token) {
    config.headers = config.headers ?? {}
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

//if backend returns 401, clear token (prevents infinite broken state)
api.interceptors.response.use(
  (r) => r,
  (err) => {
    if (err?.response?.status === 401) {
      clearToken()
    }
    return Promise.reject(err)
  }
)

export default boot(() => {})
