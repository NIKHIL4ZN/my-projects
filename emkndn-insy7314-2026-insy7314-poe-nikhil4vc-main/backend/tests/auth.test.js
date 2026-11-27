const request = require("supertest");
const bcrypt = require("bcryptjs");
const jwt = require("jsonwebtoken");

const app = require("../src/app");

const {
    jwtSecret
} = require("../src/config/env");

describe("Authentication API", () => {
    const testEmail = `test-${Date.now()}@example.com`;
    const testPassword = "SecurePass123!";
    let authToken;

    test("POST /api/auth/register should create a new account", async () => {
        const response = await request(app)
            .post("/api/auth/register")
            .send({
                name: "Test User",
                email: testEmail,
                password: testPassword
            });

        expect(response.statusCode).toBe(201);

        expect(response.body.success).toBe(true);
        expect(response.body.message).toBe(
            "Account created successfully."
        );

        expect(response.body.user).toHaveProperty("id");
        expect(response.body.user.email).toBe(testEmail);
        expect(response.body.user.role).toBe("client");

        expect(response.body.user).not.toHaveProperty("password");
        expect(response.body.user).not.toHaveProperty("passwordHash");
    });

    test("POST /api/auth/register should allow freelancer registration", async () => {
        const freelancerEmail =
            `freelancer.${Date.now()}@example.com`;

        const response = await request(app)
            .post("/api/auth/register")
            .send({
                name: "Freelancer Test",
                email: freelancerEmail,
                password: "SecurePass123!",
                role: "freelancer"
            });

        expect(response.statusCode).toBe(201);
        expect(response.body.success).toBe(true);
        expect(response.body.user.role).toBe(
            "freelancer"
        );
    });

    test("POST /api/auth/register should reject admin self-registration", async () => {
        const response = await request(app)
            .post("/api/auth/register")
            .send({
                name: "Admin Attempt",
                email: `adminattempt.${Date.now()}@example.com`,
                password: "SecurePass123!",
                role: "admin"
            });

        expect(response.statusCode).toBe(400);
        expect(response.body.success).toBe(false);

        expect(
            response.body.errors.some(
                (error) =>
                    error.field === "role"
            )
        ).toBe(true);
    });

    test("POST /api/auth/register should reject duplicate email", async () => {
        const response = await request(app)
            .post("/api/auth/register")
            .send({
                name: "Another User",
                email: testEmail,
                password: testPassword
            });

        expect(response.statusCode).toBe(409);

        expect(response.body.success).toBe(false);
        expect(response.body.message).toBe(
            "An account with this email already exists."
        );
    });

    test("POST /api/auth/register should reject a weak password", async () => {
        const response = await request(app)
            .post("/api/auth/register")
            .send({
                name: "Weak Password User",
                email: `weak-${Date.now()}@example.com`,
                password: "123"
            });

        expect(response.statusCode).toBe(400);

        expect(response.body.success).toBe(false);
    });

    test("POST /api/auth/login should authenticate a valid user", async () => {
        const response = await request(app)
            .post("/api/auth/login")
            .send({
                email: testEmail,
                password: testPassword
            });

        expect(response.statusCode).toBe(200);

        expect(response.body.success).toBe(true);
        expect(response.body.message).toBe("Login successful.");

        expect(response.body).toHaveProperty("token");
        expect(response.body.user.email).toBe(testEmail);

        authToken = response.body.token;
    });

    test("POST /api/auth/login should reject an incorrect password", async () => {
        const response = await request(app)
            .post("/api/auth/login")
            .send({
                email: testEmail,
                password: "WrongPassword123!"
            });

        expect(response.statusCode).toBe(401);

        expect(response.body.success).toBe(false);
        expect(response.body.message).toBe(
            "Invalid email or password."
        );
    });

        test("POST /api/auth/login should track failed login attempts", async () => {
        const lockoutEmail =
            `lockout-track-${Date.now()}@example.com`;

        await request(app)
            .post("/api/auth/register")
            .send({
                name: "Lockout Track User",
                email: lockoutEmail,
                password: testPassword
            });

        const response = await request(app)
            .post("/api/auth/login")
            .send({
                email: lockoutEmail,
                password: "WrongPassword123!"
            });

        expect(response.statusCode).toBe(401);
        expect(response.body.success).toBe(false);

        const {
            findUserByEmail
        } = require("../src/models/userModel");

        const user = await findUserByEmail(lockoutEmail);

        expect(user.failedLoginAttempts).toBe(1);
        expect(user.lockUntil).toBeNull();
    });

    test("POST /api/auth/login should lock an account after five failed attempts", async () => {
        const lockoutEmail =
            `lockout-five-${Date.now()}@example.com`;

        await request(app)
            .post("/api/auth/register")
            .send({
                name: "Lockout Test User",
                email: lockoutEmail,
                password: testPassword
            });

        for (let attempt = 1; attempt <= 4; attempt++) {
            const response = await request(app)
                .post("/api/auth/login")
                .send({
                    email: lockoutEmail,
                    password: "WrongPassword123!"
                });

            expect(response.statusCode).toBe(401);
        }

        const fifthAttempt = await request(app)
            .post("/api/auth/login")
            .send({
                email: lockoutEmail,
                password: "WrongPassword123!"
            });

        expect(fifthAttempt.statusCode).toBe(423);
        expect(fifthAttempt.body.success).toBe(false);
        expect(fifthAttempt.body.message).toBe(
            "Too many failed login attempts. Please wait 15 minutes before trying again."
        );

        const {
            findUserByEmail
        } = require("../src/models/userModel");

        const user = await findUserByEmail(lockoutEmail);

        expect(user.failedLoginAttempts).toBe(5);
        expect(user.lockUntil).toBeDefined();
        expect(user.lockUntil.getTime()).toBeGreaterThan(
            Date.now()
        );
    }, 15000);

    test("POST /api/auth/login should reject the correct password while the account is locked", async () => {
        const lockoutEmail =
            `lockout-correct-${Date.now()}@example.com`;

        await request(app)
            .post("/api/auth/register")
            .send({
                name: "Locked User",
                email: lockoutEmail,
                password: testPassword
            });

        for (let attempt = 1; attempt <= 5; attempt++) {
            await request(app)
                .post("/api/auth/login")
                .send({
                    email: lockoutEmail,
                    password: "WrongPassword123!"
                });
        }

        const response = await request(app)
            .post("/api/auth/login")
            .send({
                email: lockoutEmail,
                password: testPassword
            });

        expect(response.statusCode).toBe(423);
        expect(response.body.success).toBe(false);
        expect(response.body.message).toBe(
            "Too many failed login attempts. Please wait 15 minutes before trying again."
        );
    }, 15000);

    test("POST /api/auth/login should reset failed attempts after a successful login", async () => {
        const resetEmail =
            `lockout-reset-${Date.now()}@example.com`;

        await request(app)
            .post("/api/auth/register")
            .send({
                name: "Lockout Reset User",
                email: resetEmail,
                password: testPassword
            });

        await request(app)
            .post("/api/auth/login")
            .send({
                email: resetEmail,
                password: "WrongPassword123!"
            });

        await request(app)
            .post("/api/auth/login")
            .send({
                email: resetEmail,
                password: "WrongPassword123!"
            });

        const successfulLogin = await request(app)
            .post("/api/auth/login")
            .send({
                email: resetEmail,
                password: testPassword
            });

        expect(successfulLogin.statusCode).toBe(200);
        expect(successfulLogin.body.success).toBe(true);

        const {
            findUserByEmail
        } = require("../src/models/userModel");

        const user = await findUserByEmail(resetEmail);

        expect(user.failedLoginAttempts).toBe(0);
        expect(user.lockUntil).toBeNull();
    });

    test("GET /api/auth/me should reject requests without a token", async () => {
        const response = await request(app)
            .get("/api/auth/me");

        expect(response.statusCode).toBe(401);

        expect(response.body.success).toBe(false);
        expect(response.body.message).toBe(
            "Authentication token is required."
        );
    });

    test("GET /api/auth/me should reject an invalid token", async () => {
        const response = await request(app)
            .get("/api/auth/me")
            .set("Authorization", "Bearer invalid.token.here");

        expect(response.statusCode).toBe(401);

        expect(response.body.success).toBe(false);
        expect(response.body.message).toBe(
            "Invalid authentication token."
        );
    });

    test("GET /api/auth/me should accept a valid token", async () => {
        const response = await request(app)
            .get("/api/auth/me")
            .set("Authorization", `Bearer ${authToken}`);

        expect(response.statusCode).toBe(200);

        expect(response.body.success).toBe(true);
        expect(response.body.user.email).toBe(testEmail);
        expect(response.body.user.role).toBe("client");
    });

    test("JWT should contain the user's ID, email and role", async () => {
        const decoded = jwt.verify(authToken, jwtSecret);

        expect(decoded).toHaveProperty("sub");
        expect(decoded).toHaveProperty("email", testEmail);
        expect(decoded).toHaveProperty("role", "client");
    });

    test("Stored password should be bcrypt hashed", async () => {
        const {
            findUserByEmail
        } = require("../src/models/userModel");

        const user = await findUserByEmail(testEmail);

        expect(user).toBeDefined();
        expect(user.passwordHash).toBeDefined();
        expect(user.passwordHash).not.toBe(testPassword);

        const passwordMatches = await bcrypt.compare(
            testPassword,
            user.passwordHash
        );

        expect(passwordMatches).toBe(true);
    });

    test("POST /api/auth/register should reject missing required fields", async () => {
        const response = await request(app)
            .post("/api/auth/register")
            .send({
                email: `missing-${Date.now()}@example.com`
            });

        expect(response.statusCode).toBe(400);

        expect(response.body.success).toBe(false);
        expect(response.body.message).toBe("Validation failed.");

        expect(response.body.errors).toEqual(
            expect.arrayContaining([
                expect.objectContaining({
                    field: "name"
                }),
                expect.objectContaining({
                    field: "password"
                })
            ])
        );
    });

    test("POST /api/auth/login should reject an invalid email address", async () => {
        const response = await request(app)
            .post("/api/auth/login")
            .send({
                email: "not-an-email",
                password: testPassword
            });

        expect(response.statusCode).toBe(400);

        expect(response.body.success).toBe(false);
        expect(response.body.message).toBe("Validation failed.");

        expect(response.body.errors).toEqual(
            expect.arrayContaining([
                expect.objectContaining({
                    field: "email"
                })
            ])
        );
    });

    test("GET /api/auth/me should reject a malformed authorization header", async () => {
        const response = await request(app)
            .get("/api/auth/me")
            .set("Authorization", "InvalidFormat");

        expect(response.statusCode).toBe(401);

        expect(response.body.success).toBe(false);
        expect(response.body.message).toBe(
            "Authentication token is required."
        );
    });

    test("GET /api/auth/me should reject an expired JWT", async () => {
        const expiredToken = jwt.sign(
            {
                sub: "expired-user-id",
                email: "expired@example.com",
                role: "client"
            },
            jwtSecret,
            {
                expiresIn: -1
            }
        );

        const response = await request(app)
            .get("/api/auth/me")
            .set("Authorization", `Bearer ${expiredToken}`);

        expect(response.statusCode).toBe(401);

        expect(response.body.success).toBe(false);
        expect(response.body.message).toBe(
            "Authentication token has expired."
        );
    });

    test("GET /api/auth/me should reject an empty bearer token", async () => {
        const response = await request(app)
            .get("/api/auth/me")
            .set("Authorization", "Bearer ");

        expect(response.statusCode).toBe(401);

        expect(response.body.success).toBe(false);
    });

    test(
    "POST /api/auth/logout should revoke the authenticated token",
    async () => {
        const email =
            `logout-${Date.now()}@example.com`;

        const password = "SecurePass123!";

        await request(app)
            .post("/api/auth/register")
            .send({
                name: "Logout Test",
                email,
                password,
                role: "client"
            });

        const loginResponse = await request(app)
            .post("/api/auth/login")
            .send({
                email,
                password
            });

        expect(loginResponse.statusCode).toBe(200);

        const token = loginResponse.body.token;

        const logoutResponse = await request(app)
            .post("/api/auth/logout")
            .set(
                "Authorization",
                `Bearer ${token}`
            );

        expect(logoutResponse.statusCode).toBe(200);
        expect(logoutResponse.body.success).toBe(true);
        expect(logoutResponse.body.message).toBe(
            "Logout successful."
        );

        const meResponse = await request(app)
            .get("/api/auth/me")
            .set(
                "Authorization",
                `Bearer ${token}`
            );

        expect(meResponse.statusCode).toBe(401);
        expect(meResponse.body.success).toBe(false);
        expect(meResponse.body.message).toBe(
            "Authentication token has been revoked."
        );
    }
);

test(
    "POST /api/auth/logout should require authentication",
    async () => {
        const response = await request(app)
            .post("/api/auth/logout");

        expect(response.statusCode).toBe(401);
        expect(response.body.success).toBe(false);
        expect(response.body.message).toBe(
            "Authentication token is required."
        );
    }
);

test(
    "a revoked token should not be usable to logout again",
    async () => {
        const email =
            `double-logout-${Date.now()}@example.com`;

        const password = "SecurePass123!";

        await request(app)
            .post("/api/auth/register")
            .send({
                name: "Double Logout Test",
                email,
                password,
                role: "client"
            });

        const loginResponse = await request(app)
            .post("/api/auth/login")
            .send({
                email,
                password
            });

        const token = loginResponse.body.token;

        const firstLogout = await request(app)
            .post("/api/auth/logout")
            .set(
                "Authorization",
                `Bearer ${token}`
            );

        expect(firstLogout.statusCode).toBe(200);

        const secondLogout = await request(app)
            .post("/api/auth/logout")
            .set(
                "Authorization",
                `Bearer ${token}`
            );

        expect(secondLogout.statusCode).toBe(401);
        expect(secondLogout.body.message).toBe(
            "Authentication token has been revoked."
        );
    }
);

test("POST /api/auth/register should reject a non-string email", async () => {
    const response = await request(app)
        .post("/api/auth/register")
        .send({
            name: "Injection Test",
            email: {
                $ne: null
            },
            password: testPassword
        });

    expect(response.statusCode).toBe(400);
    expect(response.body.success).toBe(false);
    expect(response.body.message).toBe("Validation failed.");

    expect(response.body.errors).toEqual(
        expect.arrayContaining([
            expect.objectContaining({
                field: "email"
            })
        ])
    );
});

test("POST /api/auth/login should reject a non-string email", async () => {
    const response = await request(app)
        .post("/api/auth/login")
        .send({
            email: {
                $ne: null
            },
            password: testPassword
        });

    expect(response.statusCode).toBe(400);
    expect(response.body.success).toBe(false);
    expect(response.body.message).toBe("Validation failed.");

    expect(response.body.errors).toEqual(
        expect.arrayContaining([
            expect.objectContaining({
                field: "email"
            })
        ])
    );
});
});

// Code attributes

/*
Title: Node.js Express Testing with Jest and SuperTest
Author(s): Ermias Asmare
Date: 2023
Version: Not applicable
Type: Medium article
Availability: https://medium.com/@it.ermias.asmare/node-js-express-with-jest-and-supertest-e58aaf4c4514
*/