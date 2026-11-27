
import "@testing-library/jest-dom/vitest";

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

import Gigs from "./Gigs";
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
    name: "Test Freelancer"
  }
};

function mockMarketplace(gigs = [], bookings = []) {
  api.get.mockImplementation((url) => {
    if (url === "/gigs") {
      return Promise.resolve({
        data: {
          gigs
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
    <MemoryRouter>
      <Gigs />
    </MemoryRouter>
  );
}

describe("Gigs", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  test("displays available gigs returned by the API", async () => {
    mockMarketplace([
      mockGig,
      {
        _id: "gig-2",
        title: "Logo Design",
        description: "Professional business logo",
        category: "Graphic Design",
        price: 750,
        freelancer: {
          name: "Second Freelancer"
        }
      }
    ]);

    renderPage();

    expect(
      screen.getByText(/loading gigs/i)
    ).toBeInTheDocument();

    expect(
      await screen.findByText("Website Development")
    ).toBeInTheDocument();

    expect(
      screen.getByText("Logo Design")
    ).toBeInTheDocument();

    expect(
      screen.getByText(/R1500/)
    ).toBeInTheDocument();

    expect(
      screen.getByText(/R750/)
    ).toBeInTheDocument();

    expect(
      screen.getByText((content) =>
        content.includes("Test Freelancer")
      )
    ).toBeInTheDocument();

    expect(
      screen.getByText((content) =>
        content.includes("Second Freelancer")
      )
    ).toBeInTheDocument();
  });

  test("requests gigs and client bookings from the backend", async () => {
    mockMarketplace();

    renderPage();

    await waitFor(() => {
      expect(api.get).toHaveBeenCalledWith(
        "/gigs"
      );

      expect(api.get).toHaveBeenCalledWith(
        "/bookings/mine"
      );
    });
  });

  test("shows a message when there are no gigs", async () => {
    mockMarketplace();

    renderPage();

    expect(
      await screen.findByText(
        "No gigs are currently available."
      )
    ).toBeInTheDocument();
  });

  test("shows Request Pending for pending bookings", async () => {
    mockMarketplace(
      [mockGig],
      [
        {
          _id: "booking-1",
          status: "pending",
          gig: {
            _id: "gig-1"
          }
        }
      ]
    );

    renderPage();

    expect(
      await screen.findByText("Request Pending")
    ).toBeInTheDocument();

    expect(
      screen.queryByRole("link", {
        name: "View Gig"
      })
    ).not.toBeInTheDocument();
  });

  test("shows Already Booked for accepted bookings", async () => {
    mockMarketplace(
      [mockGig],
      [
        {
          _id: "booking-1",
          status: "booked",
          gig: {
            _id: "gig-1"
          }
        }
      ]
    );

    renderPage();

    expect(
      await screen.findByText("Already Booked")
    ).toBeInTheDocument();

    expect(
      screen.queryByRole("link", {
        name: "View Gig"
      })
    ).not.toBeInTheDocument();
  });

  test("allows clients to view gigs after rejection", async () => {
    mockMarketplace(
      [mockGig],
      [
        {
          _id: "booking-1",
          status: "rejected",
          gig: {
            _id: "gig-1"
          }
        }
      ]
    );

    renderPage();

    const viewGigLink =
      await screen.findByRole("link", {
        name: "View Gig"
      });

    expect(viewGigLink).toHaveAttribute(
      "href",
      "/gigs/gig-1"
    );

    expect(
      screen.queryByText("Request Pending")
    ).not.toBeInTheDocument();

    expect(
      screen.queryByText("Already Booked")
    ).not.toBeInTheDocument();
  });

  test("supports multiple gigs with different booking statuses", async () => {
    mockMarketplace(
      [
        mockGig,
        {
          ...mockGig,
          _id: "gig-2",
          title: "Logo Design"
        },
        {
          ...mockGig,
          _id: "gig-3",
          title: "Mobile App Development"
        }
      ],
      [
        {
          _id: "booking-1",
          status: "pending",
          gig: {
            _id: "gig-1"
          }
        },
        {
          _id: "booking-2",
          status: "booked",
          gig: {
            _id: "gig-2"
          }
        }
      ]
    );

    renderPage();

    expect(
      await screen.findByText("Request Pending")
    ).toBeInTheDocument();

    expect(
      screen.getByText("Already Booked")
    ).toBeInTheDocument();

    expect(
      screen.getAllByRole("link", {
        name: "View Gig"
      })
    ).toHaveLength(1);

    expect(
      screen.getByRole("link", {
        name: "View Gig"
      })
    ).toHaveAttribute(
      "href",
      "/gigs/gig-3"
    );
  });

  test("shows an error when gigs cannot be loaded", async () => {
    api.get.mockRejectedValue({
      response: {
        data: {
          message: "Unable to retrieve gigs."
        }
      }
    });

    renderPage();

    expect(
      await screen.findByRole("alert")
    ).toHaveTextContent(
      "Unable to retrieve gigs."
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
