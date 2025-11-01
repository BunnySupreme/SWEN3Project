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
import { useRouter } from 'vue-router';
import { useQuasar } from 'quasar';

const router = useRouter();
const $q = useQuasar();

// define msg so the template can use it
const msg = ref<string | null>(null);

function factory(): QUploaderFactoryObject {
  return {
    url: '/api/documents/upload',
    method: 'POST',
    fieldName: 'file',
    withCredentials: false
  }
}

async function onDone () {
  msg.value = 'Upload successful'
  $q.notify({ type: 'positive', message: 'Upload successful' })
  await router.push({ name: 'home' })
}

function onFail () {
  msg.value = 'Upload failed'
  $q.notify({ type: 'negative', message: 'Upload failed' })
}
</script>
