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
    // Em dev o front chama /acesso (mesma origem) e o Vite repassa para o host .NET.
    // Evita CORS no backend. Alvo configurável por AGENTE_LOCAL_URL (padrão 5080).
    proxy: {
      '/acesso': {
        target: process.env.AGENTE_LOCAL_URL ?? 'http://localhost:5080',
        changeOrigin: true,
      },
      '/cad': {
        target: process.env.AGENTE_LOCAL_URL ?? 'http://localhost:5080',
        changeOrigin: true,
      },
      '/fin': {
        target: process.env.AGENTE_LOCAL_URL ?? 'http://localhost:5080',
        changeOrigin: true,
      },
    },
  },
  // Tauri controla o build do frontend; não limpar a tela para não engolir logs.
  clearScreen: false,
})
