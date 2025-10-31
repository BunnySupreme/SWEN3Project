<template>
  <q-page class="q-pa-md">
    <div class="text-h5 q-mb-md">Upload Document</div>

    <q-uploader
      label="Drop file or click to select"
      :factory="factory"
      :auto-upload="true"
      accept="*/*"
      flat bordered
      @uploaded="onDone"
      @failed="onFail"
    />

    <q-banner v-if="msg" class="q-mt-md" rounded>{{ msg }}</q-banner>
  </q-page>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { QUploaderFactoryObject } from 'quasar';

const msg = ref('');

/**
 * Quasar will call this for each selected file.
 * We return instructions (url, method, field name...) so Quasar does the XHR upload.
 * Note: use a relative URL so it works in dev (proxy) and prod (Nginx).
 */
// eslint-disable-next-line @typescript-eslint/no-unused-vars
function factory(_files: readonly File[]): QUploaderFactoryObject {
  return {
    url: '/api/documents/upload',   // Nginx/dev-proxy will forward to your backend
    method: 'POST',
    fieldName: 'file',       // <-- must match your .NET action param (IFormFile file)
    withCredentials: false,
    // headers: [{ name: 'Authorization', value: 'Bearer ...' }], // if needed later
    // formFields: [{ name: 'someMeta', value: '123' }],          // extra fields if needed
  };
}

function onDone() {
  msg.value = 'Upload successful';
}
function onFail() {
  msg.value = 'Upload failed';
}
</script>
