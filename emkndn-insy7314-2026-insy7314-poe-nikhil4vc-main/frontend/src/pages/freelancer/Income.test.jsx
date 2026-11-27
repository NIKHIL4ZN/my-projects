
import "@testing-library/jest-dom/vitest";
formatCurrency
import {
  render,
  screen,
  waitFor
} from "@testing-library/react";

import { MemoryRouter } from "react-router-dom";

import {
  beforeEach,
  describe,
  expect,
  test,
  vi
} from "vitest";

import Income from "./Income";
import api from "../../api/api";

const mockLogout = vi.fn();

vi.mock("../../api/api", () => ({
  default: {
    get: vi.fn()
  }
}));

vi.mock("../../context/useAuth", () => ({
  useAuth: () => ({
    user: {
      name: "Test Freelancer",
      role: "freelancer"
    },
    logout: mockLogout
  })
}));

const emptyIncomeResponse = {
  totalIncome: 0,
  transactionCount: 0,
  financialSummary: {
    grossIncome: 0,
    estimatedTax: 0,
    estimatedNetIncome: 0,
    effectiveTaxRate: 0
  },
  transactions: []
};

const incomeResponse = {
  totalIncome: 2500,
  transactionCount: 2,
  financialSummary: {
    grossIncome: 2500,
    estimatedTax: 375,
    estimatedNetIncome: 2125,
    effectiveTaxRate: 15
  },
  transactions: [
    {
      _id: "transaction-1",
      amount: 1500,
      type: "booking_payment",
      status: "completed",
      createdAt: "2026-08-22T13:54:08.017Z"
    },
    {
      _id: "transaction-2",
      amount: 1000,
      type: "booking_payment",
      status: "completed",
      createdAt: "2026-08-22T14:14:58.403Z"
    }
  ]
};

function formatCurrency(value) {
  return new Intl.NumberFormat("en-ZA", {
    style: "currency",
    currency: "ZAR"
  })
    .format(value)
    .replace(/\s/g, " ");
}

function renderPage() {
  return render(
    <MemoryRouter>
      <Income />
    </MemoryRouter>
  );
}

describe("Income", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  test("requests freelancer income from the backend", async () => {
    api.get.mockResolvedValue({
      data: emptyIncomeResponse
    });

    renderPage();

    await waitFor(() => {
      expect(api.get).toHaveBeenCalledWith(
        "/bookings/income"
      );
    });
  });

  test("displays the financial dashboard", async () => {
    api.get.mockResolvedValue({
      data: incomeResponse
    });

    renderPage();

    expect(
      await screen.findByRole("heading", {
        name: "Financial Dashboard"
      })
    ).toBeInTheDocument();

    expect(
      await screen.findByText("Gross Income")
    ).toBeInTheDocument();

    expect(
      screen.getAllByText("Estimated Tax").length
    ).toBeGreaterThanOrEqual(1);

    expect(
      screen.getAllByText(
        "Estimated Net Income"
      ).length
    ).toBeGreaterThanOrEqual(1);

    expect(
      screen.getByText("Flat Tax Rate")
    ).toBeInTheDocument();
  });

  test("displays financial values using the flat 15% tax rate", async () => {
    api.get.mockResolvedValue({
      data: incomeResponse
    });

    renderPage();

    expect(
      await screen.findByText(
        formatCurrency(2500)
      )
    ).toBeInTheDocument();

    expect(
      screen.getAllByText(
        formatCurrency(375)
      ).length
    ).toBeGreaterThanOrEqual(2);

    expect(
      screen.getAllByText(
        formatCurrency(2125)
      ).length
    ).toBeGreaterThanOrEqual(2);

    expect(
      screen.getAllByText("15%")
    ).toHaveLength(2);
  });

  test("displays only the flat 15% tax policy", async () => {
    api.get.mockResolvedValue({
      data: incomeResponse
    });

    renderPage();

    expect(
      await screen.findByRole("heading", {
        name: "Tax Estimate"
      })
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        "Flat Estimated Tax Rate"
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        /A fixed 15% tax estimate is applied/
      )
    ).toBeInTheDocument();

    expect(
      screen.queryByText("First R10,000")
    ).not.toBeInTheDocument();

    expect(
      screen.queryByText("R10,001 – R30,000")
    ).not.toBeInTheDocument();

    expect(
      screen.queryByText("Above R30,000")
    ).not.toBeInTheDocument();

    expect(
      screen.queryByText("10%")
    ).not.toBeInTheDocument();

    expect(
      screen.queryByText("20%")
    ).not.toBeInTheDocument();
  });

  test("displays the income breakdown visualisation", async () => {
    api.get.mockResolvedValue({
      data: incomeResponse
    });

    renderPage();

    expect(
      await screen.findByRole("heading", {
        name: "Income Breakdown"
      })
    ).toBeInTheDocument();

    const breakdownBar =
      document.querySelector(".breakdown-bar");

    const netBar =
      document.querySelector(".breakdown-net");

    const taxBar =
      document.querySelector(".breakdown-tax");

    expect(
      breakdownBar
    ).toBeInTheDocument();

    expect(
      netBar
    ).toBeInTheDocument();

    expect(
      taxBar
    ).toBeInTheDocument();

    expect(
      netBar
    ).toHaveStyle({
      width: "85%"
    });

    expect(
      taxBar
    ).toHaveStyle({
      width: "15%"
    });
  });

  test("displays completed transaction details", async () => {
    api.get.mockResolvedValue({
      data: incomeResponse
    });

    renderPage();

    expect(
      await screen.findByRole("heading", {
        name: "Transactions"
      })
    ).toBeInTheDocument();

    expect(
      screen.getByText("2 completed")
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        formatCurrency(1500)
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText(
        formatCurrency(1000)
      )
    ).toBeInTheDocument();

    expect(
      screen.getAllByText("Booking Payment")
    ).toHaveLength(2);

    expect(
      screen.getAllByText("completed")
    ).toHaveLength(2);
  });

  test("shows zero income and no transactions", async () => {
    api.get.mockResolvedValue({
      data: emptyIncomeResponse
    });

    renderPage();

    expect(
      await screen.findByText(
        "No transactions yet"
      )
    ).toBeInTheDocument();

    expect(
      screen.getAllByText(
        formatCurrency(0)
      ).length
    ).toBeGreaterThanOrEqual(3);

    expect(
      screen.getAllByText("15%")
    ).toHaveLength(2);

    expect(
      document.querySelector(".breakdown-net")
    ).toHaveStyle({
      width: "0%"
    });

    expect(
      document.querySelector(".breakdown-tax")
    ).toHaveStyle({
      width: "0%"
    });
  });

  test("shows an error when income cannot be loaded", async () => {
    api.get.mockRejectedValue({
      response: {
        data: {
          message: "Unable to retrieve income."
        }
      }
    });

    renderPage();

    expect(
      await screen.findByRole("alert")
    ).toHaveTextContent(
      "Unable to retrieve income."
    );
  });
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
