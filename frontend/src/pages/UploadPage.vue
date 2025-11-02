<template>
  <q-page class="q-pa-md">
    <div class="text-h5 q-mb-md">Upload Document</div>

    <q-form ref="form" @submit.prevent="onSubmit">
      <q-input
        v-model="title"
        label="Title"
        :rules="[req, max255]"
        lazy-rules
        filled
        class="q-mb-md"
      />

      <q-input
        v-model="tagsText"
        label="Tags (comma separated)"
        :rules="[tagsRule]"
        lazy-rules
        filled
        class="q-mb-md"
      />

      <q-uploader
        ref="uploader"
        :auto-upload="false"
        accept=".pdf"
      :max-file-size="10 * 1024 * 1024"
      :multiple="false"
      flat bordered class="q-mb-md"
      @rejected="onRejected"
      />


      <div class="row q-gutter-sm">
        <q-btn type="submit" color="primary" label="Upload" :disable="submitting"/>
        <q-btn flat label="Reset" @click="() => reset(true)" :disable="submitting"/>
      </div>
    </q-form>

    <q-banner
      v-if="msg"
      class="q-mt-md"
      rounded
      :class="isError ? 'bg-negative text-white' : 'bg-positive text-white'"
    >
      {{ msg }}
    </q-banner>
  </q-page>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { Notify } from 'quasar';
import { uploadDocument } from 'src/api/client';

const form = ref();
const uploader = ref();
const title = ref('');
const tagsText = ref('');
const msg = ref('');
const isError = ref(false);
const submitting = ref(false);


const req = (v: string) => (!!v && v.trim().length > 0) || 'Title is required';
const max255 = (v: string) => (v?.length ?? 0) <= 255 || 'Max 255 characters';
const tagsRule = (v: string) => {
  if (!v) return true;
  const tags = v.split(',').map(t => t.trim()).filter(Boolean);
  if (tags.length > 10) return 'Max 10 tags';
  if (tags.some(t => t.length > 30)) return 'Each tag ≤ 30 chars';
  return true;
};

type UploaderRejectedEntry = {
  failedPropValidation: string;
  file: File;
};

async function onSubmit() {
  const valid = await form.value.validate();
  if (!valid) return;

  const qItem = uploader.value.files?.[0];
  const file: File | undefined = (qItem && (qItem.__file ?? qItem));
  if (!file) { msg.value = 'Please select a PDF file'; isError.value = true; return; }

  submitting.value = true;
  try {
    await uploadDocument(file, title.value, tagsText.value);
    msg.value = 'Upload successful'; isError.value = false; reset(false);
  } catch {
    msg.value = 'Upload failed'; isError.value = true;
  } finally {
    submitting.value = false;
  }
}

function reset(clearMsg = true) {
  title.value = '';
  tagsText.value = '';
  uploader.value.reset();
  if (clearMsg) msg.value = '';
}

function onRejected() {
    Notify.create({ type: 'negative', message: `file was rejected. Only PDF up to 10 MB.` });
    msg.value = `file was rejected. Only PDF files up to 10 MB are allowed.`;
    isError.value = true;
}

</script>
