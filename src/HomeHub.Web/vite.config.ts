import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: {
    // Output to ASP.NET Core wwwroot for the monolith build
    outDir: '../HomeHub.Api/wwwroot',
    emptyOutDir: true,
  },
  server: {
    proxy: {
      // Proxy API calls to the .NET backend during local dev
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
})
