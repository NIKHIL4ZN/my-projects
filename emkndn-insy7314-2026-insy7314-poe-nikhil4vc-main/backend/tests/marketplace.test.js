
const request = require("supertest");
const bcrypt = require("bcryptjs");

const app = require("../src/app");
const User = require("../src/models/User");
const Gig = require("../src/models/Gig");
const Booking = require("../src/models/Booking");
const Transaction = require("../src/models/Transaction");

describe("Marketplace API", () => {
    let clientToken;
    let freelancerToken;
    let otherFreelancerToken;

    let clientUser;
    let freelancerUser;
    let otherFreelancerUser;

    let gigId;
    let bookingId;

    const password = "SecurePass123!";

    beforeAll(async () => {
        const passwordHash = await bcrypt.hash(password, 12);

        clientUser = await User.create({
            name: "Marketplace Client",
            email: `client-${Date.now()}@example.com`,
            passwordHash,
            role: "client"
        });

        freelancerUser = await User.create({
            name: "Marketplace Freelancer",
            email: `freelancer-${Date.now()}@example.com`,
            passwordHash,
            role: "freelancer"
        });

        otherFreelancerUser = await User.create({
            name: "Other Freelancer",
            email: `other-${Date.now()}@example.com`,
            passwordHash,
            role: "freelancer"
        });

        const clientLogin = await request(app)
            .post("/api/auth/login")
            .send({
                email: clientUser.email,
                password
            });

        const freelancerLogin = await request(app)
            .post("/api/auth/login")
            .send({
                email: freelancerUser.email,
                password
            });

        const otherFreelancerLogin = await request(app)
            .post("/api/auth/login")
            .send({
                email: otherFreelancerUser.email,
                password
            });

        clientToken = clientLogin.body.token;
        freelancerToken = freelancerLogin.body.token;
        otherFreelancerToken = otherFreelancerLogin.body.token;
    });

    afterAll(async () => {
        await Transaction.deleteMany({
            $or: [
                { client: clientUser._id },
                { freelancer: freelancerUser._id },
                { freelancer: otherFreelancerUser._id }
            ]
        });

        await Booking.deleteMany({
            $or: [
                { client: clientUser._id },
                { freelancer: freelancerUser._id },
                { freelancer: otherFreelancerUser._id }
            ]
        });

        await Gig.deleteMany({
            freelancer: {
                $in: [
                    freelancerUser._id,
                    otherFreelancerUser._id
                ]
            }
        });

        await User.deleteMany({
            _id: {
                $in: [
                    clientUser._id,
                    freelancerUser._id,
                    otherFreelancerUser._id
                ]
            }
        });
    });

    test("Freelancer should create a gig", async () => {
        const response = await request(app)
            .post("/api/gigs")
            .set("Authorization", `Bearer ${freelancerToken}`)
            .send({
                title: "Automated Test Gig",
                description: "This gig is created by the automated marketplace tests.",
                category: "Testing",
                price: 1200
            });

        expect(response.statusCode).toBe(201);
        expect(response.body.success).toBe(true);
        expect(response.body.gig.title).toBe("Automated Test Gig");

        gigId = response.body.gig._id;
    });

    test("Client should not create a gig", async () => {
        const response = await request(app)
            .post("/api/gigs")
            .set("Authorization", `Bearer ${clientToken}`)
            .send({
                title: "Invalid Client Gig",
                description: "A client should not be allowed to create this gig.",
                category: "Testing",
                price: 500
            });

        expect(response.statusCode).toBe(403);
        expect(response.body.success).toBe(false);
    });

    test("Freelancer should view their own gigs", async () => {
        const response = await request(app)
            .get("/api/gigs/mine")
            .set("Authorization", `Bearer ${freelancerToken}`);

        expect(response.statusCode).toBe(200);
        expect(response.body.success).toBe(true);

        const gig = response.body.gigs.find(
            (item) => item._id === gigId
        );

        expect(gig).toBeDefined();
    });

    test("Authenticated client should browse active gigs", async () => {
        const response = await request(app)
            .get("/api/gigs")
            .set("Authorization", `Bearer ${clientToken}`);

        expect(response.statusCode).toBe(200);
        expect(response.body.success).toBe(true);

        const gig = response.body.gigs.find(
            (item) => item._id === gigId
        );

        expect(gig).toBeDefined();
    });

    test("Freelancer should update their own gig", async () => {
        const response = await request(app)
            .put(`/api/gigs/${gigId}`)
            .set("Authorization", `Bearer ${freelancerToken}`)
            .send({
                price: 1500
            });

        expect(response.statusCode).toBe(200);
        expect(response.body.success).toBe(true);
        expect(response.body.gig.price).toBe(1500);
    });

    test("Another freelancer should not update someone else's gig", async () => {
        const response = await request(app)
            .put(`/api/gigs/${gigId}`)
            .set("Authorization", `Bearer ${otherFreelancerToken}`)
            .send({
                price: 9999
            });

        expect(response.statusCode).toBe(403);
        expect(response.body.success).toBe(false);
    });

    // A client requests a booking.
    // The booking starts as pending and no transaction exists yet.
    test("Client should request a pending booking", async () => {
        const response = await request(app)
            .post("/api/bookings")
            .set("Authorization", `Bearer ${clientToken}`)
            .send({
                gigId
            });

        expect(response.statusCode).toBe(201);
        expect(response.body.success).toBe(true);
        expect(response.body.booking.price).toBe(1500);
        expect(response.body.booking.status).toBe("pending");

        expect(response.body.transaction).toBeUndefined();

        bookingId = response.body.booking._id;

        const existingTransaction = await Transaction.findOne({
            booking: bookingId
        });

        expect(existingTransaction).toBeNull();
    });

    test("Freelancer should not book a gig", async () => {
        const response = await request(app)
            .post("/api/bookings")
            .set("Authorization", `Bearer ${freelancerToken}`)
            .send({
                gigId
            });

        expect(response.statusCode).toBe(403);
        expect(response.body.success).toBe(false);
    });

    test("Client should view their own bookings", async () => {
        const response = await request(app)
            .get("/api/bookings/mine")
            .set("Authorization", `Bearer ${clientToken}`);

        expect(response.statusCode).toBe(200);
        expect(response.body.success).toBe(true);
        expect(response.body.count).toBeGreaterThanOrEqual(1);

        const booking = response.body.bookings.find(
            (item) => item._id === bookingId
        );

        expect(booking).toBeDefined();
        expect(booking.status).toBe("pending");
    });

    test("Freelancer should view bookings for their gigs", async () => {
        const response = await request(app)
            .get("/api/bookings/freelancer")
            .set("Authorization", `Bearer ${freelancerToken}`);

        expect(response.statusCode).toBe(200);
        expect(response.body.success).toBe(true);
        expect(response.body.count).toBeGreaterThanOrEqual(1);

        const booking = response.body.bookings.find(
            (item) => item._id === bookingId
        );

        expect(booking).toBeDefined();
        expect(booking.status).toBe("pending");
    });

    // Only the assigned freelancer can accept a pending booking.
    test("Another freelancer should not accept the booking", async () => {
        const response = await request(app)
            .patch(`/api/bookings/${bookingId}/accept`)
            .set(
                "Authorization",
                `Bearer ${otherFreelancerToken}`
            );

        expect(response.statusCode).toBe(404);
        expect(response.body.success).toBe(false);

        const booking = await Booking.findById(bookingId);

        expect(booking.status).toBe("pending");
    });

    // Accepting a booking changes its status and creates a transaction.
    test("Freelancer should accept the booking and create a transaction", async () => {
        const response = await request(app)
            .patch(`/api/bookings/${bookingId}/accept`)
            .set("Authorization", `Bearer ${freelancerToken}`);

        expect(response.statusCode).toBe(200);
        expect(response.body.success).toBe(true);
        expect(response.body.message).toBe(
            "Booking request accepted successfully."
        );

        expect(response.body.booking.status).toBe("booked");

        expect(response.body.transaction).toBeDefined();
        expect(response.body.transaction.amount).toBe(1500);

        const savedTransaction = await Transaction.findOne({
            booking: bookingId
        });

        expect(savedTransaction).not.toBeNull();
        expect(savedTransaction.amount).toBe(1500);
    });

    // A reviewed booking cannot be accepted again.
    test("Freelancer should not accept an already accepted booking", async () => {
        const response = await request(app)
            .patch(`/api/bookings/${bookingId}/accept`)
            .set("Authorization", `Bearer ${freelancerToken}`);

        expect(response.statusCode).toBe(409);
        expect(response.body.success).toBe(false);
        expect(response.body.message).toBe(
            "This booking request has already been reviewed."
        );

        const transactionCount = await Transaction.countDocuments({
            booking: bookingId
        });

        expect(transactionCount).toBe(1);
    });

    // Income is generated only after a booking is accepted.
    test("Freelancer income should include accepted booking transaction", async () => {
        const response = await request(app)
            .get("/api/bookings/income")
            .set("Authorization", `Bearer ${freelancerToken}`);

        expect(response.statusCode).toBe(200);
        expect(response.body.success).toBe(true);

        expect(response.body.totalIncome).toBeGreaterThanOrEqual(1500);
        expect(response.body.transactionCount).toBeGreaterThanOrEqual(1);

        expect(response.body.financialSummary).toBeDefined();

        expect(
            response.body.financialSummary.grossIncome
        ).toBe(response.body.totalIncome);

        expect(
            response.body.financialSummary.estimatedTax
        ).toBeCloseTo(response.body.totalIncome * 0.15, 2);

        expect(
            response.body.financialSummary.estimatedNetIncome
        ).toBeCloseTo(response.body.totalIncome * 0.85, 2);

        expect(
            response.body.financialSummary.effectiveTaxRate
        ).toBe(15);
    });

    test("Client should not access freelancer income", async () => {
        const response = await request(app)
            .get("/api/bookings/income")
            .set("Authorization", `Bearer ${clientToken}`);

        expect(response.statusCode).toBe(403);
        expect(response.body.success).toBe(false);
    });

    test("Gig creation should reject non-string text input", async () => {
        const response = await request(app)
            .post("/api/gigs")
            .set("Authorization", `Bearer ${freelancerToken}`)
            .send({
                title: {
                    $ne: null
                },
                description:
                    "This description is long enough for validation.",
                category: "Testing",
                price: 500
            });

        expect(response.statusCode).toBe(400);
        expect(response.body.success).toBe(false);
        expect(response.body.message).toBe("Validation failed.");
    });

    test("Gig creation should escape potentially unsafe HTML", async () => {
        const response = await request(app)
            .post("/api/gigs")
            .set("Authorization", `Bearer ${freelancerToken}`)
            .send({
                title: "<script>alert('xss')</script>",
                description:
                    "A valid description containing enough characters.",
                category: "Testing",
                price: 500
            });

        expect(response.statusCode).toBe(201);
        expect(response.body.success).toBe(true);

        expect(response.body.gig.title).not.toContain("<script>");
        expect(response.body.gig.title).toContain("&lt;script&gt;");
    });

    test("Booking should reject an invalid MongoDB gig ID", async () => {
        const response = await request(app)
            .post("/api/bookings")
            .set("Authorization", `Bearer ${clientToken}`)
            .send({
                gigId: "not-a-valid-mongodb-id"
            });

        expect(response.statusCode).toBe(400);
        expect(response.body.success).toBe(false);
        expect(response.body.message).toBe("Validation failed.");
    });

    test("Booking should reject object-shaped gig ID input", async () => {
        const response = await request(app)
            .post("/api/bookings")
            .set("Authorization", `Bearer ${clientToken}`)
            .send({
                gigId: {
                    $ne: null
                }
            });

        expect(response.statusCode).toBe(400);
        expect(response.body.success).toBe(false);
        expect(response.body.message).toBe("Validation failed.");
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
