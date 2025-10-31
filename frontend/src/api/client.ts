import { api } from 'boot/axios';

export interface DocumentDto {
  id: string;
  title: string;
  createdAt: string;
  sizeBytes?: number;
}

export async function listDocuments() {
  const { data } = await api.get<DocumentDto[]>('/api/documents');
  return data;
}

export async function getDocument(id: string) {
  const { data } = await api.get<DocumentDto>(`/api/documents/${id}`);
  return data;
}

export async function uploadDocument(file: File) {
  const form = new FormData();
  form.append('file', file);
  const { data } = await api.post('/api/documents', form, {
    headers: { 'Content-Type': 'multipart/form-data' }
  });
  return data;
}
