import axios, { AxiosError } from 'axios'
import { boot } from 'quasar/wrappers'
import { clearToken } from 'src/services/authToken'

export const api = axios.create({
  baseURL: '/api',
})

api.interceptors.response.use(
  (r) => r,
  (err: unknown) => {
    // clear token on 401 if this is an axios error with a response
    if (axios.isAxiosError(err) && err.response?.status === 401) {
      clearToken()
    }

    if (err instanceof Error) {
      return Promise.reject(err)
    }

    return Promise.reject(new Error('Request failed'))
  }
)

export default boot(() => {})
