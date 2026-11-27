import process from "node:process";
import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  testMatch: "**/*.e2e.js",

  fullyParallel: true,

  reporter: "html",

  use: {
    baseURL: "http://localhost:5173",
    ignoreHTTPSErrors: true,
    trace: "on-first-retry"
  },

  projects: [
    {
      name: "chromium",
      use: {
        ...devices["Desktop Chrome"]
      }
    }
  ],

  webServer: {
    command:
      "npm run dev -- --host 127.0.0.1 --port 5173 --strictPort",
    url: "http://127.0.0.1:5173",
    reuseExistingServer: !process.env.CI,
    timeout: 30000
  }
});

// Code attributes

/*
Title: Getting Started with Playwright: A Beginner’s Guide
Author(s): Mandalchandan
Date: 2025
Version: Not applicable
Type: Online tutorial
Availability: https://medium.com/@90mandalchandan/getting-started-with-playwright-a-beginners-guide-7c79825b66e8
*/
