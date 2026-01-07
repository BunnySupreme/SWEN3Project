<template>
  <q-layout view="lHh Lpr lFf" class="bg-primary">
    <q-header elevated>
      <q-toolbar>
        <q-toolbar-title>Documents</q-toolbar-title>

        <q-space />

        <q-input standout v-model="text" label="Search" />
        <q-btn
          flat
          dense
          icon="search"
          label="Search"
          @click="onSearch" />

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
import { ref } from 'vue'
import { logout } from '../services/authApi'

const router = useRouter()
const text = ref('')

function onSearch() {
  try {
    void router.push({ name: 'searchResults', query: { text: text.value } })
  } catch (error) {
    console.error('Search failed:', error)
  }
}

async function onLogout() {
  try {
    await logout() // POST /api/auth/logout, clears token
  } finally {
    await router.replace({ name: 'login' })
  }
}
</script>
