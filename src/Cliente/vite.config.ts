import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'

// https://vitejs.dev/config/ — servidor fixo na 5173 (o Tauri aponta devUrl para cá).
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  server: {
    port: 5173,
    strictPort: true,
  },
  // Tauri controla o build do frontend; não limpar a tela para não engolir logs.
  clearScreen: false,
})
