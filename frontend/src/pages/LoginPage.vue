<template>
  <q-page class="q-pa-md">
    <div class="q-mx-auto" style="max-width: 420px;">
      <div class="text-h5 q-mb-md">Login</div>

      <q-form @submit.prevent="onSubmit" class="q-gutter-md">
        <q-input
          v-model.trim="username"
          label="Username"
          autocomplete="username"
          :rules="[v => !!v || 'Username is required']"
          outlined
        />

        <q-input
          v-model="password"
          label="Password"
          type="password"
          autocomplete="current-password"
          :rules="[v => !!v || 'Password is required']"
          outlined
        />

        <q-btn
          type="submit"
          label="Login"
          color="primary"
          :loading="loading"
          class="full-width"
        />

        <q-btn
          flat
          label="Create account"
          :to="{ name: 'register' }"
          class="full-width"
        />

        <div v-if="error" class="text-negative">{{ error }}</div>
      </q-form>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { login } from 'src/services/authApi' // adjust import path

const router = useRouter()
const route = useRoute()

const username = ref('')
const password = ref('')
const loading = ref(false)
const error = ref<string | null>(null)

async function onSubmit() {
  loading.value = true
  error.value = null
  try {
    await login({ username: username.value, password: password.value })

    const next = (route.query.next as string) || '/'
    await router.replace(next)
  } catch (e: any) {
    error.value = e?.response?.data?.error ?? 'Login failed'
  } finally {
    loading.value = false
  }
}
</script>
