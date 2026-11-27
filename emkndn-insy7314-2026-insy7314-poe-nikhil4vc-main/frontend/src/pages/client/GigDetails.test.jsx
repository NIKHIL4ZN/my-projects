
import "@testing-library/jest-dom/vitest";

import {
  fireEvent,
  render,
  screen,
  waitFor
} from "@testing-library/react";

import {
  MemoryRouter,
  Route,
  Routes
} from "react-router-dom";

import {
  beforeEach,
  describe,
  expect,
  test,
  vi
} from "vitest";

import GigDetails from "./GigDetails";
import api from "../../api/api";

const mockLogout = vi.fn();

vi.mock("../../api/api", () => ({
  default: {
    get: vi.fn(),
    post: vi.fn()
  }
}));

vi.mock("../../context/useAuth", () => ({
  useAuth: () => ({
    user: {
      name: "Test Client",
      role: "client"
    },
    logout: mockLogout
  })
}));

const mockGig = {
  _id: "gig-1",
  title: "Website Development",
  description: "Responsive business website",
  category: "Web Development",
  price: 1500,
  freelancer: {
    _id: "freelancer-1",
    name: "Test Freelancer"
  }
};

function mockBooking(status) {
  return {
    _id: "booking-1",
    gig: {
      _id: "gig-1"
    },
    status
  };
}

function mockApiResponses(bookings = []) {
  api.get.mockImplementation((url) => {
    if (url === "/gigs/gig-1") {
      return Promise.resolve({
        data: {
          gig: mockGig
        }
      });
    }

    if (url === "/bookings/mine") {
      return Promise.resolve({
        data: {
          bookings
        }
      });
    }

    return Promise.reject(
      new Error("Unexpected API request")
    );
  });
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/gigs/gig-1"]}>
      <Routes>
        <Route
          path="/gigs/:id"
          element={<GigDetails />}
        />
      </Routes>
    </MemoryRouter>
  );
}

describe("GigDetails", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  test("loads gig details and client bookings", async () => {
    mockApiResponses();

    renderPage();

    expect(
      await screen.findByText("Website Development")
    ).toBeInTheDocument();

    expect(api.get).toHaveBeenCalledWith(
      "/gigs/gig-1"
    );

    expect(api.get).toHaveBeenCalledWith(
      "/bookings/mine"
    );
  });

  test("allows clients to request an available gig", async () => {
    mockApiResponses();

    renderPage();

    expect(
      await screen.findByRole("button", {
        name: "Request Booking for R1500"
      })
    ).toBeEnabled();
  });

  test("disables booking while a request is pending", async () => {
    mockApiResponses([
      mockBooking("pending")
    ]);

    renderPage();

    expect(
      await screen.findByRole("button", {
        name: "Request Pending"
      })
    ).toBeDisabled();

    expect(
      screen.getByText(
        "Your request is awaiting freelancer approval."
      )
    ).toBeInTheDocument();

    expect(api.post).not.toHaveBeenCalled();
  });

  test("shows Already Booked after freelancer acceptance", async () => {
    mockApiResponses([
      mockBooking("booked")
    ]);

    renderPage();

    expect(
      await screen.findByRole("button", {
        name: "Already Booked"
      })
    ).toBeDisabled();

    expect(
      screen.getByText(
        "The freelancer has accepted your booking."
      )
    ).toBeInTheDocument();
  });

  test("allows another request after rejection", async () => {
    mockApiResponses([
      mockBooking("rejected")
    ]);

    renderPage();

    expect(
      await screen.findByRole("button", {
        name: "Request Booking for R1500"
      })
    ).toBeEnabled();
  });

  test("submits a booking request and displays confirmation", async () => {
    mockApiResponses();

    api.post.mockResolvedValue({
      data: {
        message: "Booking request submitted.",
        booking: {
          ...mockBooking("pending")
        }
      }
    });

    renderPage();

    fireEvent.click(
      await screen.findByRole("button", {
        name: "Request Booking for R1500"
      })
    );

    await waitFor(() => {
      expect(api.post).toHaveBeenCalledWith(
        "/bookings",
        {
          gigId: "gig-1"
        }
      );
    });

    expect(
      await screen.findByRole("status")
    ).toHaveTextContent(
      "Booking request submitted successfully."
    );

    expect(
      screen.getByRole("button", {
        name: "Request Pending"
      })
    ).toBeDisabled();
  });

  test("displays an error when a booking request fails", async () => {
    mockApiResponses();

    api.post.mockRejectedValue({
      response: {
        status: 400,
        data: {
          message: "Unable to submit booking."
        }
      }
    });

    renderPage();

    fireEvent.click(
      await screen.findByRole("button", {
        name: "Request Booking for R1500"
      })
    );

    expect(
      await screen.findByRole("alert")
    ).toHaveTextContent(
      "Unable to submit booking."
    );

    expect(
      screen.getByRole("button", {
        name: "Request Booking for R1500"
      })
    ).toBeEnabled();
  });

  test("handles duplicate pending booking requests", async () => {
    mockApiResponses();

    api.post.mockRejectedValue({
      response: {
        status: 409,
        data: {
          message: "An active booking already exists."
        }
      }
    });

    renderPage();

    fireEvent.click(
      await screen.findByRole("button", {
        name: "Request Booking for R1500"
      })
    );

    await screen.findByRole("alert");

    api.get.mockImplementation((url) => {
      if (url === "/bookings/mine") {
        return Promise.resolve({
          data: {
            bookings: [
              mockBooking("pending")
            ]
          }
        });
      }

      return Promise.resolve({
        data: {
          gig: mockGig
        }
      });
    });

    // The duplicate response should be displayed.
    expect(
      screen.getByRole("alert")
    ).toHaveTextContent(
      "An active booking already exists."
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
