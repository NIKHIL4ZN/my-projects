import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{js,jsx}'],
    extends: [
      js.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
      parserOptions: { ecmaFeatures: { jsx: true } },
    },
  },
])

// Code attributes

/*
Title: How I Set Up CI/CD for a Node.js App with ESLint, Jest, and Docker Hub
Author(s): Nafisah Abidemi Abdulkadir
Date: 2025
Version: Not applicable
Type: Medium article
Availability: https://medium.com/opportunities-in-the-world-of-tech/how-i-set-up-ci-cd-for-a-node-js-app-with-eslint-jest-and-docker-hub-7d3cacf7add8
*/