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
import { listDocuments } from 'src/api/client';
import { useRouter } from 'vue-router';
import type { QTableColumn } from 'quasar';
import type { DocumentDto } from 'src/api/client';

const router = useRouter();
const rows = ref<DocumentDto[]>([]);
const loading = ref(false);

const columns: QTableColumn<DocumentDto>[] = [
  { name: 'title', label: 'Title', field: 'title', align: 'left', sortable: true },
  { name: 'createdAt', label: 'Created', field: 'createdAt', align: 'left',
    format: v => new Date(v).toLocaleString(), sortable: true },
  { name: 'sizeBytes', label: 'Size (B)', field: 'sizeBytes', align: 'right',
    format: v => v?.toLocaleString() ?? '', sortable: true },
];

function goDetail(_evt: unknown, row: DocumentDto) {
  void router.push({ name: 'docDetail', params: { id: row.id } });
}

onMounted(async () => {
  loading.value = true;
  try { rows.value = await listDocuments(); }
  finally { loading.value = false; }
});
</script>
