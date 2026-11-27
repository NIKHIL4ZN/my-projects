const express = require("express");
const rateLimit = require("express-rate-limit");

const {
    createBooking,
    acceptBooking,
    rejectBooking,
    getMyClientBookings,
    getFreelancerBookings,
    getFreelancerIncome
} = require("../controllers/bookingController");

const {
    createBookingValidation
} = require("../validators/bookingValidators");

const {
    handleValidationErrors
} = require("../middleware/validationMiddleware");

const {
    authenticateToken
} = require("../middleware/authMiddleware");

const {
    requireRole
} = require("../middleware/roleMiddleware");

const bookingLimiter = rateLimit({
    windowMs: 15 * 60 * 1000,
    max: 20,
    standardHeaders: true,
    legacyHeaders: false,
    message: {
        success: false,
        message: "Too many booking requests. Please try again later."
    }
});

const router = express.Router();

// Client submits a booking request
router.post(
    "/",
    bookingLimiter,
    authenticateToken,
    requireRole("client"),
    createBookingValidation,
    handleValidationErrors,
    createBooking
);

// Client views own bookings
router.get(
    "/mine",
    authenticateToken,
    requireRole("client"),
    getMyClientBookings
);

// Freelancer views booking requests
router.get(
    "/freelancer",
    authenticateToken,
    requireRole("freelancer"),
    getFreelancerBookings
);

// Freelancer views income
router.get(
    "/income",
    authenticateToken,
    requireRole("freelancer"),
    getFreelancerIncome
);

// Freelancer accepts a pending booking request
router.patch(
    "/:bookingId/accept",
    authenticateToken,
    requireRole("freelancer"),
    acceptBooking
);

// Freelancer rejects a pending booking request
router.patch(
    "/:bookingId/reject",
    authenticateToken,
    requireRole("freelancer"),
    rejectBooking
);

module.exports = router;