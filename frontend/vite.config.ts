import react from "@vitejs/plugin-react"
import { defineConfig } from "vitest/config"

const apiTarget = "http://localhost:5062"

export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    proxy: {
      "/api": apiTarget,
      "/openapi": apiTarget,
    },
  },
  test: {
    environment: "jsdom",
    setupFiles: ["src/test/setup.ts"],
    // Vitest 預設把 CSS 換成空字串；theme.test.ts 要讀 theme.css 原文
    css: { include: [/theme\.css/] },
  },
})
