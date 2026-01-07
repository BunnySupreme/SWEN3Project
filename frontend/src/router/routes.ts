import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: () => import('../layouts/MainLayout.vue'),
    children: [
      { path: '', name: 'home', component: () => import('../pages/IndexPage.vue') },
      { path: 'documents/:id', name: 'docDetail', component: () => import('../pages/DocumentDetail.vue') },
      { path: 'upload', name: 'upload', component: () => import('../pages/UploadPage.vue') }
    ]
  },

  {
    path: '/',
    component: () => import('../layouts/AuthLayout.vue'),
    children: [
      {
        path: 'login',
        name: 'login',
        component: () => import('../pages/LoginPage.vue'),
        meta: { public: true }
      },
      {
        path: 'register',
        name: 'register',
        component: () => import('../pages/RegisterPage.vue'),
        meta: { public: true }
      }
    ]
  },

  { path: '/:catchAll(.*)*', component: () => import('../pages/ErrorNotFound.vue') }
]

export default routes
