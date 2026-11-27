import process from "node:process";
import { test, expect } from "@playwright/test";

const API_BASE_URL = "https://localhost:5000/api";
const API_LOGIN_PATH = "/api/auth/login";

// Log in through the real HustleHub+ frontend.
async function loginAs(page, email, password, expectedPath) {
  if (!email || !password) {
    throw new Error(
      "Missing E2E account credentials."
    );
  }

  await page.goto("/login");

  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);

  const [loginResponse] = await Promise.all([
    page.waitForResponse((response) =>
      response.url().includes(API_LOGIN_PATH) &&
      response.request().method() === "POST"
    ),
    page.getByRole("button", {
      name: "Login"
    }).click()
  ]);

  const loginStatus = loginResponse.status();

  expect(
    loginResponse.ok(),
    `Test account login failed with HTTP ${loginStatus}`
  ).toBeTruthy();

  const loginData = await loginResponse.json();

  await expect(page).toHaveURL(
    new RegExp(`${expectedPath}(?:[/?#]|$)`)
  );

  return loginData.user;
}

test.describe("HustleHub+ Booking Workflow E2E", () => {
  test("client requests a booking and freelancer accepts it", async ({
    browser
  }) => {
    test.setTimeout(60000);

    const clientContext = await browser.newContext({
      ignoreHTTPSErrors: true
    });

    const freelancerContext = await browser.newContext({
      ignoreHTTPSErrors: true
    });

    try {
      const clientPage = await clientContext.newPage();
      const freelancerPage =
        await freelancerContext.newPage();

      // Step 1: Freelancer logs in.
      const freelancer = await loginAs(
        freelancerPage,
        process.env.E2E_FREELANCER_EMAIL,
        process.env.E2E_FREELANCER_PASSWORD,
        "/freelancer"
      );

      expect(freelancer.role).toBe("freelancer");

      // Step 2: Retrieve the freelancer's
      // authentication token.
      const freelancerToken =
        await freelancerPage.evaluate(() =>
          sessionStorage.getItem("token")
        );

      expect(
        freelancerToken,
        "Freelancer authentication token is required"
      ).toBeTruthy();

      // Step 3: Automatically create a fresh
      // gig for this test run.
      const gigData = {
        title: `Playwright Booking Test ${Date.now()}`,
        description:
          "Automated test gig for verifying the complete client booking and freelancer acceptance workflow.",
        category: "Development",
        price: 250
      };

      const createGigResponse =
        await freelancerContext.request.post(
          `${API_BASE_URL}/gigs`,
          {
            headers: {
              Authorization: `Bearer ${freelancerToken}`
            },
            data: gigData
          }
        );

      expect(
        createGigResponse.status(),
        "The freelancer should be able to create a test gig"
      ).toBe(201);

      const createGigData =
        await createGigResponse.json();

      expect(createGigData.success).toBe(true);
      expect(createGigData.gig?._id).toBeTruthy();

      const gigId = createGigData.gig._id;
      const gigTitle = createGigData.gig.title;

      // Step 4: Client logs in.
      const client = await loginAs(
        clientPage,
        process.env.E2E_CLIENT_EMAIL,
        process.env.E2E_CLIENT_PASSWORD,
        "/gigs"
      );

      expect(client.role).toBe("client");

      // Step 5: Client opens the newly
      // created test gig.
      await clientPage.goto(`/gigs/${gigId}`);

      const gigHeading = clientPage.locator(
        "article.gig-details-card h1"
      );

      await expect(gigHeading).toBeVisible();
      await expect(gigHeading).toHaveText(gigTitle);

      // Step 6: Verify the new gig
      // can receive a booking request.
      const requestButton = clientPage.getByRole(
        "button",
        {
          name: /^Request Booking for R/
        }
      );

      await expect(requestButton).toBeEnabled();

      // Step 7: Client submits the
      // booking request.
      await requestButton.click();

      await expect(
        clientPage.getByRole("status")
      ).toContainText(
        "Booking request submitted successfully"
      );

      // Step 8: Booking must initially
      // have pending status.
      await expect(
        clientPage.getByRole("button", {
          name: "Request Pending"
        })
      ).toBeDisabled();

      // Step 9: Freelancer opens
      // incoming booking requests.
      await freelancerPage.goto(
        "/freelancer/bookings"
      );

      await expect(
        freelancerPage.getByRole("heading", {
          name: "Client Booking Requests"
        })
      ).toBeVisible();

      // Find the booking belonging to
      // this gig and this client.
      // Do not filter by status because
      // the status changes after acceptance.
      const bookingCard = freelancerPage
        .locator("article.booking-card")
        .filter({
          has: freelancerPage.getByRole(
            "heading",
            {
              name: gigTitle,
              exact: true
            }
          )
        })
        .filter({
          hasText: `Client: ${client.name}`
        });

      await expect(bookingCard).toHaveCount(1);

      await expect(
        bookingCard.locator(".status-badge")
      ).toHaveText("pending");

      // Step 10: Freelancer accepts
      // the booking request.
      await bookingCard.getByRole("button", {
        name: "Accept"
      }).click();

      // Step 11: Verify that the
      // booking is confirmed.
      await expect(
        bookingCard.locator(".status-badge")
      ).toHaveText("booked");

      // Step 12: Client reloads the gig
      // and sees the confirmed booking.
      await clientPage.reload();

      await expect(
        clientPage.getByRole("button", {
          name: "Already Booked"
        })
      ).toBeDisabled();

      await expect(
        clientPage.getByText(
          "The freelancer has accepted your booking."
        )
      ).toBeVisible();

    } finally {
      await clientContext.close();
      await freelancerContext.close();
    }
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
