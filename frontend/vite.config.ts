import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    proxy: {
      '/bff': {
        target: 'http://localhost:5000',
        changeOrigin: false,
        secure: false,
      },
      '/signin-oidc': {
        target: 'http://localhost:5000',
        changeOrigin: false,
        secure: false,
      },
      '/api/user': {
        target: 'http://localhost:5000',
        changeOrigin: false,
        secure: false,
      },
    },
  },
})
