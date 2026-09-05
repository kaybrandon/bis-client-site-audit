import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'http://localhost:5088', changeOrigin: true },
      '/uploads': { target: 'http://localhost:5088', changeOrigin: true },
    },
  },
  preview: {
    port: 4173,
    proxy: {
      '/api': { target: 'http://localhost:5088', changeOrigin: true },
      '/uploads': { target: 'http://localhost:5088', changeOrigin: true },
    },
  },
})
