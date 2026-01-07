<template>
  <q-page class="q-pa-md">
    <div class="q-mx-auto" style="max-width: 420px;">
      <div class="text-h5 q-mb-md">Register</div>

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
          autocomplete="new-password"
          :rules="[v => (v?.length ?? 0) >= 8 || 'Min 8 characters']"
          outlined
        />

        <q-input
          v-model="confirmPassword"
          label="Confirm password"
          type="password"
          autocomplete="new-password"
          :rules="[v => v === password || 'Passwords do not match']"
          outlined
        />

        <q-btn
          type="submit"
          label="Register"
          color="primary"
          :loading="loading"
          class="full-width"
        />

        <q-btn
          flat
          label="Back to login"
          :to="{ name: 'login' }"
          class="full-width"
        />

        <div v-if="error" class="text-negative">{{ error }}</div>
      </q-form>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { register } from 'src/services/authApi'

const router = useRouter()

const username = ref('')
const password = ref('')
const confirmPassword = ref('')
const loading = ref(false)
const error = ref<string | null>(null)
import { getHttpErrorMessage } from 'src/utils/httpError'

async function onSubmit() {
  loading.value = true
  error.value = null
  try {
    await register({ username: username.value, password: password.value })
    await router.replace({ name: 'login' })
  } catch (e: unknown) {
    error.value = getHttpErrorMessage(e)
  } finally {
    loading.value = false
  }
}
</script>
