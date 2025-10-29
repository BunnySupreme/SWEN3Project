// import axios from 'axios';

// export const api = axios.create({
//   baseURL: import.meta.env.VITE_API_BASE
// });

// console.log('API base is: ',
//   import.meta.env.VITE_API_BASE);

import axios from 'axios';

export const api = axios.create({
  baseURL: '/api'  // Relative path, nginx will proxy to the actual backend
});

console.log('API base URL:', '/api');