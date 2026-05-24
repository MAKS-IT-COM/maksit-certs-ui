import { realpathSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'
import tailwindcss from '@tailwindcss/vite'

const root = realpathSync(fileURLToPath(new URL('.', import.meta.url)))

// https://vite.dev/config/
export default defineConfig({
  root,
  resolve: {
    dedupe: [
      'zod',
      'react',
      'react-dom',
      '@maks-it.com/webui-contracts',
      '@maks-it.com/webui-core',
      '@maks-it.com/webui-components',
    ],
  },
  plugins: [
    react(),
    tailwindcss(),
  ],
  server: {
    host: '0.0.0.0',
    allowedHosts: ['client'],
    watch: {
      usePolling: true,
    },
  },
})
