<template>
  <q-page class="q-pa-md">
    <q-breadcrumbs class="q-mb-md bg-white q-pa-sm rounded-borders shadow-1">
      <q-breadcrumbs-el label="Documents" :to="{ name: 'home' }" />
      <q-breadcrumbs-el :label="'Search Results'" />
    </q-breadcrumbs>

    <div class="row items-center justify-between q-mb-md">
      <div class="text-h5">Search Results</div>
    </div>

    <q-table
      :rows="rows"
      :columns="columns"
      row-key="id"
      :loading="loading"
      flat bordered
      @row-click="goDetail"
    />
  </q-page>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import type { QTableColumn } from 'quasar';
import { search, type DocumentReadDto } from 'src/api/client';
import { watch } from 'vue';
import { useRoute } from 'vue-router';

const router = useRouter();
const rows = ref<DocumentReadDto[]>([]);
const loading = ref(false);
const route = useRoute();

const columns: QTableColumn<DocumentReadDto>[] = [
  {
    name: 'title',
    label: 'Title',
    field: (row) => row.title,
    align: 'left',
    sortable: true
  },
  {
    name: 'tags',
    label: 'Tags',
    field: (row) => row.tags?.join(', ') ?? '',
    align: 'left',
    sortable: false
  },
  {
    name: 'uploadedAt',
    label: 'Uploaded',
    field: (row) => new Date(row.uploadedAt).toLocaleString(),
    align: 'left',
    sortable: true
  },
  {
    name: 'summary',
    label: 'Summary',
    field: (row) => row.summary,
    align: 'left',
    sortable: false
  }
];

watch(
  () => route.query.text,
  async (newText) => {
    if (newText) {
      await search(newText as string).then((data) => {
        rows.value = data;
      });
    }
  },
  { immediate: true }
)

function goDetail(_evt: unknown, row: DocumentReadDto) {
  void router.push({ name: 'docDetail', params: { id: row.id } });
}

onMounted(async () => {
  loading.value = true;
  const searchTerm = String(router.currentRoute.value.query.text);
  try { rows.value = await search(searchTerm); }
  finally { loading.value = false; }
});
</script>
