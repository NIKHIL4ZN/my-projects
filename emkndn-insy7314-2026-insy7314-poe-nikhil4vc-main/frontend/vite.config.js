import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig({
  plugins: [react()],

  test: {
    environment: "jsdom",
    setupFiles: "./src/test/setup.js"
  }
});

// Code attributes

/*
Title: Writing Unit Tests in Node.js Using Jest
Author(s): David Ekete
Date: 2023
Version: Not applicable
Type: Online tutorial
Availability: https://semaphore.io/blog/unit-tests-nodejs-jest
*/