<template>
  <q-page class="q-pa-md">
    <div class="text-h5">Paperless Dashboard</div>
    <q-btn class="q-mt-md" label="Test API" @click="testApi" />
    <div class="q-mt-md">Base: {{ base }}</div>
    <pre class="q-mt-md">{{ result }}</pre>
  </q-page>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { api } from 'boot/axios';

const base = import.meta.env.VITE_API_BASE;
const result = ref('Click "Test API"');

import axios from 'axios';

async function testApi() {
  try {
    const { data } = await api.get('/documents');
    result.value = JSON.stringify(data, null, 2);
  } catch (e: unknown) {
    if (axios.isAxiosError(e)) {
      // Axios error: show server message or generic message
      const body = e.response?.data ?? e.message;
      result.value = typeof body === 'string' ? body : JSON.stringify(body, null, 2);
    } else if (e instanceof Error) {
      result.value = e.message;
    } else {
      result.value = String(e);
    }
  }
}

</script>
