
import { test, expect } from "@playwright/test";

test.describe("HustleHub+ Login E2E", () => {
  test("displays the login page correctly", async ({ page }) => {
    await page.goto("/login");

    await expect(
      page.getByRole("heading", {
        name: "Welcome back"
      })
    ).toBeVisible();

    await expect(
      page.getByLabel("Email")
    ).toBeVisible();

    await expect(
      page.getByLabel("Password")
    ).toBeVisible();

    await expect(
      page.getByRole("button", {
        name: "Login"
      })
    ).toBeVisible();
  });

  test("allows users to enter login credentials", async ({ page }) => {
    await page.goto("/login");

    await page.getByLabel("Email").fill(
      "client@example.com"
    );

    await page.getByLabel("Password").fill(
      "SecurePass123!"
    );

    await expect(
      page.getByLabel("Email")
    ).toHaveValue("client@example.com");

    await expect(
      page.getByLabel("Password")
    ).toHaveValue("SecurePass123!");
  });

  test("provides a link to registration", async ({ page }) => {
    await page.goto("/login");

    await page.getByRole("link", {
      name: "Register"
    }).click();

    await expect(page).toHaveURL(/\/register$/);
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
