import axios from 'axios';

export const api = axios.create({
  baseURL: '/api'  // Relative path, nginx will proxy to the actual backend
});

console.log('API base URL:', '/api');
