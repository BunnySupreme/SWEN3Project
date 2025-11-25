import { api } from 'boot/axios';

export interface DocumentReadDto {
  id: string;
  title: string;
  uploadedAt: string;
  summary: string;
  tags: string[];
}

export async function listDocuments() {
  const { data } = await api.get<DocumentReadDto[]>('/documents');
  return data;
}

export async function getDocument(id: string) {
  const { data } = await api.get<DocumentReadDto>(`/documents/${id}`);
  return data;
}

export async function downloadDocument(id: string) {
  const { data } = await api.get<Blob>(`/documents/${id}/download`, {
    responseType: 'blob',
  });
  return data;
}

export async function uploadDocument(file: File, title: string, tagsCsv: string) {
  const form = new FormData();
  form.append('file', file, file.name);
  form.append('title', title);
  form.append('tags',  tagsCsv);
  const { data } = await api.post('/documents/upload', form); // baseURL '/api'
  return data;
}
