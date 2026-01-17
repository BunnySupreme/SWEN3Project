<template>
  <q-page class="q-pa-md">
    <q-breadcrumbs class="q-mb-md bg-white q-pa-sm rounded-borders shadow-1">
      <q-breadcrumbs-el label="Documents" :to="{ name: 'home' }" />
      <q-breadcrumbs-el :label="doc?.title || 'Loading…'" />
    </q-breadcrumbs>

    <q-card v-if="doc">
      <q-card-section>
        <div class="text-h6">{{ doc.title }}</div>
        <div class="text-subtitle2">ID: {{ doc.id }}</div>
      </q-card-section>

      <q-separator />

      <q-card-section>
        <div class="q-mb-sm"><b>Uploaded:</b> {{ new Date(doc.uploadedAt).toLocaleString() }}</div>

        <div class="q-mt-sm">
          <b>Tags:</b>
          <div class="q-gutter-xs q-mt-xs">
            <q-chip
              v-for="t in doc.tags" :key="t"
              size="sm" color="primary" text-color="white"
              outline>{{ t }}</q-chip>
            <span v-if="!doc.tags || doc.tags.length === 0">—</span>
          </div>
        </div>

        <div class="q-mt-md">
          <b>Summary:</b>
          <div class="q-mt-xs">{{ doc.summary || '—' }}</div>
        </div>

        <div class="q-mt-md">
          <b>Access Count:</b>
          <div class="q-mt-xs">{{ doc.accessCount }}</div>
        </div>
      </q-card-section>

      <q-separator />

      <q-card-section>
        <div class="q-mt-md">
          <q-btn
            @click="handleDownload"
            type="button"
            color="primary"
            label="Download"
            :loading="downloading"
          />
        </div>
      </q-card-section>
    </q-card>

    <q-skeleton v-else type="rect" height="160px" />
  </q-page>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { getDocument, downloadDocument, logDocumentAccess, type DocumentReadDto } from '../api/client';

const route = useRoute();
const doc = ref<DocumentReadDto | null>(null);
const downloading = ref(false);

onMounted(async () => {
  const id = String(route.params.id);
  doc.value = await getDocument(id);
  await logAccess();
});

async function logAccess() {
  if (!doc.value) return;
  try {
    await logDocumentAccess(doc.value.id);
  } catch (error) {
    console.error('Failed to log document access:', error);
  }
}

async function handleDownload() {
  if (!doc.value) return;

  downloading.value = true;
  try {
    const blob = await downloadDocument(doc.value.id);

    // Create a download link and trigger it
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${doc.value.title}.pdf`; // Use the document title as filename
    document.body.appendChild(link);
    link.click();

    // Cleanup
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  } catch (error) {
    console.error('Download failed:', error);
    // Optionally show an error notification using Quasar's Notify plugin
  } finally {
    downloading.value = false;
  }
}
</script>
