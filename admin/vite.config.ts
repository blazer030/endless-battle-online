import react from "@vitejs/plugin-react"
import { defineConfig } from "vitest/config"

const apiTarget = "http://localhost:5062"

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5174,
    proxy: {
      "/admin": apiTarget,
      "/openapi": apiTarget,
    },
  },
  test: {
    environment: "jsdom",
    setupFiles: ["src/test/setup.ts"],
    // Refine 的套件交給 Vite 處理，和測試共用同一份 ESM 的 react-router（否則各自拿到 Router context）
    server: { deps: { inline: [/@refinedev\//] } },
    testTimeout: 15_000,
  },
})
