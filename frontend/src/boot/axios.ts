import axios from 'axios';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE
});

console.log('API base is: ',
  import.meta.env.VITE_API_BASE);
