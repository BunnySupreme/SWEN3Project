<template>
  <q-layout view="lHh Lpr lFf" class="bg-primary">
    <q-header elevated>
      <q-toolbar>
        <q-toolbar-title>Documents</q-toolbar-title>

        <q-space />

        <q-btn
          flat
          dense
          icon="logout"
          label="Logout"
          @click="onLogout"
        />
      </q-toolbar>
    </q-header>

    <q-page-container>
      <router-view />
    </q-page-container>
  </q-layout>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { logout } from '../services/authApi'

const router = useRouter()

async function onLogout() {
  try {
    await logout() // POST /api/auth/logout, clears token
  } finally {
    await router.replace({ name: 'login' })
  }
}
</script>
