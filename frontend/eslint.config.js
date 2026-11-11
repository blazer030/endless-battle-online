import js from "@eslint/js"
import globals from "globals"
import reactHooks from "eslint-plugin-react-hooks"
import reactRefresh from "eslint-plugin-react-refresh"
import tseslint from "typescript-eslint"
import eslintConfigPrettier from "eslint-config-prettier/flat"
import { defineConfig, globalIgnores } from "eslint/config"

export default defineConfig([
  globalIgnores(["dist", "src/api/schema.ts"]),
  {
    files: ["**/*.{ts,tsx}"],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
  },
  // 畫面與遊戲狀態只用 Vo；API 產生的型別只在 src/api 的 Converter 裡出現
  {
    files: ["src/game/**/*.{ts,tsx}", "src/ui/**/*.{ts,tsx}", "src/render/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              regex: "(^|/)api/schema$",
              message: "畫面與遊戲狀態改用 src/api 的 Converter 轉出的 Vo。",
            },
          ],
        },
      ],
    },
  },
  eslintConfigPrettier,
])
