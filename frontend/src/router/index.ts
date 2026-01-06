import { defineRouter } from '#q-app/wrappers'
import {
  createMemoryHistory,
  createRouter,
  createWebHashHistory,
  createWebHistory,
} from 'vue-router'
import routes from './routes'
import { getToken } from 'src/services/authToken'

export default defineRouter(function () {
  const createHistory = process.env.SERVER
    ? createMemoryHistory
    : process.env.VUE_ROUTER_MODE === 'history'
      ? createWebHistory
      : createWebHashHistory

  const Router = createRouter({
    scrollBehavior: () => ({ left: 0, top: 0 }),
    routes,
    history: createHistory(process.env.VUE_ROUTER_BASE),
  })

  Router.beforeEach((to) => {
    // allow public routes
    if (to.meta.public) return true

    const token = getToken()
    const isAuth = !!token

    if (to.meta.public) {
      if (isAuth && (to.name === 'login' || to.name === 'register')) {
        return { name: 'home' }
      }
      return true
    }

    if (!isAuth) return { name: 'login', query: { next: to.fullPath } }
    return true
  })

  return Router
})
