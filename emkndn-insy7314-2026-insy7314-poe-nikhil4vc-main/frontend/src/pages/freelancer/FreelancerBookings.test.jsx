
import "@testing-library/jest-dom/vitest";

import {
  fireEvent,
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

import FreelancerBookings from "./FreelancerBookings";
import api from "../../api/api";

const mockLogout = vi.fn();

vi.mock("../../api/api", () => ({
  default: {
    get: vi.fn(),
    patch: vi.fn()
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

const pendingBooking = {
  _id: "booking-1",
  gig: {
    _id: "gig-1",
    title: "Website Development",
    category: "Web Development"
  },
  client: {
    _id: "client-1",
    name: "Test Client",
    email: "client@example.com"
  },
  price: 1500,
  status: "pending",
  createdAt: "2026-08-22T13:54:08.017Z"
};

function renderPage() {
  return render(
    <MemoryRouter>
      <FreelancerBookings />
    </MemoryRouter>
  );
}

describe("FreelancerBookings", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  test("requests freelancer bookings from the backend", async () => {
    api.get.mockResolvedValue({
      data: {
        bookings: []
      }
    });

    renderPage();

    await waitFor(() => {
      expect(api.get).toHaveBeenCalledWith(
        "/bookings/freelancer"
      );
    });
  });

  test("displays bookings returned by the API", async () => {
    api.get.mockResolvedValue({
      data: {
        bookings: [
          {
            ...pendingBooking,
            status: "booked"
          }
        ]
      }
    });

    renderPage();

    expect(
      await screen.findByText("Website Development")
    ).toBeInTheDocument();

    expect(
      screen.getByText((content) =>
        content.includes("Test Client")
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText((content) =>
        content.includes("1500")
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText("booked", {
        selector: ".status-badge"
      })
    ).toBeInTheDocument();
  });

  test("shows pending requests with Accept and Reject buttons", async () => {
    api.get.mockResolvedValue({
      data: {
        bookings: [pendingBooking]
      }
    });

    renderPage();

    expect(
      await screen.findByText("pending", {
        selector: ".status-badge"
      })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("button", {
        name: "Accept"
      })
    ).toBeInTheDocument();

    expect(
      screen.getByRole("button", {
        name: "Reject"
      })
    ).toBeInTheDocument();
  });

  test("accepts a pending booking request", async () => {
    api.get.mockResolvedValue({
      data: {
        bookings: [pendingBooking]
      }
    });

    api.patch.mockResolvedValue({
      data: {
        message: "Booking request accepted successfully.",
        booking: {
          ...pendingBooking,
          status: "booked"
        }
      }
    });

    renderPage();

    fireEvent.click(
      await screen.findByRole("button", {
        name: "Accept"
      })
    );

    await waitFor(() => {
      expect(api.patch).toHaveBeenCalledWith(
        "/bookings/booking-1/accept"
      );
    });

    expect(
      await screen.findByText("booked", {
        selector: ".status-badge"
      })
    ).toBeInTheDocument();

    expect(
      screen.queryByRole("button", {
        name: "Accept"
      })
    ).not.toBeInTheDocument();
  });

  test("rejects a pending booking request", async () => {
    api.get.mockResolvedValue({
      data: {
        bookings: [pendingBooking]
      }
    });

    api.patch.mockResolvedValue({
      data: {
        message: "Booking request rejected.",
        booking: {
          ...pendingBooking,
          status: "rejected"
        }
      }
    });

    renderPage();

    fireEvent.click(
      await screen.findByRole("button", {
        name: "Reject"
      })
    );

    await waitFor(() => {
      expect(api.patch).toHaveBeenCalledWith(
        "/bookings/booking-1/reject"
      );
    });

    expect(
      await screen.findByText("rejected", {
        selector: ".status-badge"
      })
    ).toBeInTheDocument();

    expect(
      screen.queryByRole("button", {
        name: "Reject"
      })
    ).not.toBeInTheDocument();
  });

  test("does not show approval buttons for confirmed bookings", async () => {
    api.get.mockResolvedValue({
      data: {
        bookings: [
          {
            ...pendingBooking,
            status: "booked"
          }
        ]
      }
    });

    renderPage();

    expect(
      await screen.findByText("booked", {
        selector: ".status-badge"
      })
    ).toBeInTheDocument();

    expect(
      screen.queryByRole("button", {
        name: "Accept"
      })
    ).not.toBeInTheDocument();

    expect(
      screen.queryByRole("button", {
        name: "Reject"
      })
    ).not.toBeInTheDocument();
  });

  test("shows an error if approval fails", async () => {
    api.get.mockResolvedValue({
      data: {
        bookings: [pendingBooking]
      }
    });

    api.patch.mockRejectedValue({
      response: {
        data: {
          message: "This booking request has already been reviewed."
        }
      }
    });

    renderPage();

    fireEvent.click(
      await screen.findByRole("button", {
        name: "Accept"
      })
    );

    expect(
      await screen.findByRole("alert")
    ).toHaveTextContent(
      "This booking request has already been reviewed."
    );

    expect(
      screen.getByText("pending", {
        selector: ".status-badge"
      })
    ).toBeInTheDocument();
  });

  test("shows a message when there are no freelancer bookings", async () => {
    api.get.mockResolvedValue({
      data: {
        bookings: []
      }
    });

    renderPage();

    expect(
      await screen.findByText("No booking requests yet")
    ).toBeInTheDocument();
  });

  test("shows an error when bookings cannot be loaded", async () => {
    api.get.mockRejectedValue({
      response: {
        data: {
          message: "Unable to retrieve freelancer bookings."
        }
      }
    });

    renderPage();

    expect(
      await screen.findByRole("alert")
    ).toHaveTextContent(
      "Unable to retrieve freelancer bookings."
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
