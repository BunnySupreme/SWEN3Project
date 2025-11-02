<template>
  <q-page class="q-pa-md">
    <div class="row items-center justify-between q-mb-md">
      <div class="text-h5">Documents</div>
      <q-btn color="primary" label="Upload" :to="{ name: 'upload' }" />
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
import { listDocuments, type DocumentReadDto } from 'src/api/client';

const router = useRouter();
const rows = ref<DocumentReadDto[]>([]);
const loading = ref(false);

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
  }
];

function goDetail(_evt: unknown, row: DocumentReadDto) {
  void router.push({ name: 'docDetail', params: { id: row.id } });
}

onMounted(async () => {
  loading.value = true;
  try { rows.value = await listDocuments(); }
  finally { loading.value = false; }
});
</script>
