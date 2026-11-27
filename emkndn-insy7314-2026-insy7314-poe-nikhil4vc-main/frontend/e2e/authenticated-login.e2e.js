import process from "node:process";
import { test, expect } from "@playwright/test";

async function loginAs(page, email, password) {
  if (!email || !password) {
    throw new Error(
      "Missing E2E login credentials. Set the required environment variables."
    );
  }

  await page.goto("/login");

  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);

  await page.getByRole("button", {
    name: "Login"
  }).click();
}

test.describe("HustleHub+ Authenticated Login E2E", () => {
  test("client can log in and access the gigs dashboard", async ({ page }) => {
    await loginAs(
      page,
      process.env.E2E_CLIENT_EMAIL,
      process.env.E2E_CLIENT_PASSWORD
    );

    await expect(page).toHaveURL(/\/gigs(?:[/?#]|$)/, {
      timeout: 15000
    });
  });

  test("freelancer can log in and access the freelancer dashboard", async ({ page }) => {
    await loginAs(
      page,
      process.env.E2E_FREELANCER_EMAIL,
      process.env.E2E_FREELANCER_PASSWORD
    );

    await expect(page).toHaveURL(/\/freelancer(?:[/?#]|$)/, {
      timeout: 15000
    });
  });
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