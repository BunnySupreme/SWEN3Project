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
        <div>Created: {{ doc.createdAt }}</div>
        <div v-if="doc.sizeBytes">Size: {{ doc.sizeBytes }} bytes</div>
      </q-card-section>
    </q-card>
    <q-skeleton v-else type="rect" height="120px" />
  </q-page>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { getDocument, type DocumentDto } from 'src/api/client';

const route = useRoute();
const doc = ref<DocumentDto | null>(null);

onMounted(async () => {
  const id = String(route.params.id);
  doc.value = await getDocument(id);
});
</script>
