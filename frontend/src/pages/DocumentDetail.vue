<template>
  <q-page class="q-pa-md">
    <q-breadcrumbs class="q-mb-md">
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
      </q-card-section>
    </q-card>

    <q-skeleton v-else type="rect" height="160px" />
  </q-page>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { getDocument, type DocumentReadDto } from 'src/api/client';

const route = useRoute();
const doc = ref<DocumentReadDto | null>(null);

onMounted(async () => {
  const id = String(route.params.id);
  doc.value = await getDocument(id);
});
</script>
