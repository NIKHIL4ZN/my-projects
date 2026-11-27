
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

import Login from "./Login";

const mockLogin = vi.fn();
const mockNavigate = vi.fn();

vi.mock("../context/useAuth", () => ({
  useAuth: () => ({
    login: mockLogin
  })
}));

vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");

  return {
    ...actual,
    useNavigate: () => mockNavigate
  };
});

function renderLogin() {
  return render(
    <MemoryRouter>
      <Login />
    </MemoryRouter>
  );
}

function submitLogin(
  email = "client@example.com",
  password = "SecurePass123!"
) {
  fireEvent.change(
    screen.getByLabelText("Email"),
    {
      target: {
        value: email
      }
    }
  );

  fireEvent.change(
    screen.getByLabelText("Password"),
    {
      target: {
        value: password
      }
    }
  );

  fireEvent.click(
    screen.getByRole("button", {
      name: "Login"
    })
  );
}

describe("Login", () => {
  beforeEach(() => {
    mockLogin.mockReset();
    mockNavigate.mockClear();
  });

  test("renders login form", () => {
    renderLogin();

    expect(
      screen.getByRole("heading", {
        name: "Welcome back"
      })
    ).toBeInTheDocument();

    expect(
      screen.getByLabelText("Email")
    ).toBeInTheDocument();

    expect(
      screen.getByLabelText("Password")
    ).toBeInTheDocument();

    expect(
      screen.getByRole("button", {
        name: "Login"
      })
    ).toBeInTheDocument();
  });

  test("submits email and password", async () => {
    mockLogin.mockResolvedValue({
      user: {
        name: "Test Client",
        role: "client"
      }
    });

    renderLogin();

    submitLogin();

    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalledWith(
        "client@example.com",
        "SecurePass123!"
      );
    });
  });

  test("redirects client to gigs after successful login", async () => {
    mockLogin.mockResolvedValue({
      user: {
        name: "Test Client",
        role: "client"
      }
    });

    renderLogin();

    submitLogin();

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith(
        "/gigs"
      );
    });
  });

  test("redirects freelancer to freelancer dashboard", async () => {
    mockLogin.mockResolvedValue({
      user: {
        name: "Test Freelancer",
        role: "freelancer"
      }
    });

    renderLogin();

    submitLogin(
      "freelancer@example.com",
      "SecurePass123!"
    );

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith(
        "/freelancer"
      );
    });
  });

  test("shows login error returned by API", async () => {
    mockLogin.mockRejectedValue({
      response: {
        status: 401,
        data: {
          message: "Invalid email or password."
        }
      }
    });

    renderLogin();

    submitLogin(
      "client@example.com",
      "WrongPassword123!"
    );

    expect(
      await screen.findByRole("alert")
    ).toHaveTextContent(
      "Invalid email or password."
    );

    expect(
      mockNavigate
    ).not.toHaveBeenCalled();
  });

  test("shows temporary block message when login rate limit is exceeded", async () => {
    mockLogin.mockRejectedValue({
      response: {
        status: 429,
        data: {
          success: false,
          message:
            "Too many authentication attempts. Please try again later."
        }
      }
    });

    renderLogin();

    submitLogin();

    expect(
      await screen.findByRole("alert")
    ).toHaveTextContent(
      "Too many login attempts. Login is temporarily blocked. Please wait and try again later."
    );

    expect(
      mockNavigate
    ).not.toHaveBeenCalled();
  });

  test("shows a fallback error when the server cannot be reached", async () => {
    mockLogin.mockRejectedValue(
      new Error("Network Error")
    );

    renderLogin();

    submitLogin();

    expect(
      await screen.findByRole("alert")
    ).toHaveTextContent(
      "Login failed. Please try again."
    );

    expect(
      mockNavigate
    ).not.toHaveBeenCalled();
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
