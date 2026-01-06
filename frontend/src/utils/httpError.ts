import axios from 'axios'

export function getHttpErrorMessage(e: unknown): string {
  if (axios.isAxiosError(e)) {
    const data = e.response?.data as unknown

    // backend returns { error: "..." }
    if (data && typeof data === 'object' && 'error' in data) {
      const msg = (data as { error?: unknown }).error
      if (typeof msg === 'string' && msg.trim().length > 0) return msg
    }

    // fallback to axios message
    if (typeof e.message === 'string' && e.message.trim().length > 0) return e.message
    return 'Request failed'
  }

  if (e instanceof Error) return e.message
  return 'Request failed'
}
