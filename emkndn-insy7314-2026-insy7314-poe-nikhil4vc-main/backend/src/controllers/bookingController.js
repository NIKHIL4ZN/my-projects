const Booking = require("../models/Booking");
const Transaction = require("../models/Transaction");
const Gig = require("../models/Gig");
const mongoose = require("mongoose");
const { calculateEstimatedTax } = require("../utils/taxCalculator");
const logger = require("../utils/logger");

// Client requests to book an active gig
async function createBooking(req, res, next) {
    try {
        const { gigId } = req.body;

        const gig = await Gig.findById(gigId);

        if (!gig) {
            return res.status(404).json({
                success: false,
                message: "Gig not found."
            });
        }

        if (!gig.isActive) {
            return res.status(400).json({
                success: false,
                message:
                    "This gig is not currently available for booking."
            });
        }

        if (gig.freelancer.toString() === req.user.sub) {
            return res.status(400).json({
                success: false,
                message: "You cannot book your own gig."
            });
        }

        const existingBooking = await Booking.findOne({
            gig: gig._id,
            client: req.user.sub,
            status: { $in: ["pending", "booked"] }
        });

        if (existingBooking) {
            return res.status(409).json({
                success: false,
                message:
                    "You already have a pending or confirmed booking for this gig."
            });
        }

        const createdBooking = await Booking.create({
            gig: gig._id,
            client: req.user.sub,
            freelancer: gig.freelancer,
            price: gig.price,
            status: "pending"
        });

        logger.info("Booking request created", {
            bookingId: createdBooking._id.toString(),
            gigId: createdBooking.gig.toString(),
            clientId: createdBooking.client.toString(),
            freelancerId: createdBooking.freelancer.toString(),
            status: createdBooking.status
        });

        return res.status(201).json({
            success: true,
            message:
                "Booking request submitted. Awaiting freelancer approval.",
            booking: createdBooking
        });
    } catch (error) {
        if (error.code === 11000) {
            return res.status(409).json({
                success: false,
                message:
                    "You already have a pending or confirmed booking for this gig."
            });
        }

        if (error.name === "CastError") {
            return res.status(400).json({
                success: false,
                message: "Invalid gig ID."
            });
        }

        next(error);
    }
}

// Freelancer accepts or rejects a pending booking request
async function reviewBooking(req, res, next, decision) {
    const { bookingId } = req.params;

    if (!mongoose.isValidObjectId(bookingId)) {
        return res.status(400).json({
            success: false,
            message: "Invalid booking ID."
        });
    }

    const newStatus =
        decision === "accept" ? "booked" : "rejected";

    let updatedBooking;
    let createdTransaction;

    let session;

    try {
        session = await mongoose.startSession();

        await session.withTransaction(async () => {
            // Only the assigned freelancer can change
            // a booking that is still pending.
            updatedBooking = await Booking.findOneAndUpdate(
                {
                    _id: bookingId,
                    freelancer: req.user.sub,
                    status: "pending"
                },
                {
                    $set: { status: newStatus }
                },
                {
                    new: true,
                    session
                }
            );

            if (!updatedBooking) {
                const existingBooking = await Booking.findOne({
                    _id: bookingId,
                    freelancer: req.user.sub
                }).session(session);

                const error = new Error(
                    existingBooking
                        ? "This booking request has already been reviewed."
                        : "Booking request not found."
                );

                error.statusCode = existingBooking ? 409 : 404;
                throw error;
            }

            // A transaction is created only after acceptance.
            if (decision === "accept") {
                createdTransaction = new Transaction({
                    booking: updatedBooking._id,
                    client: updatedBooking.client,
                    freelancer: updatedBooking.freelancer,
                    amount: updatedBooking.price
                });

                await createdTransaction.save({ session });
            }
        });

        logger.info("Booking request reviewed", {
            bookingId: updatedBooking._id.toString(),
            freelancerId: updatedBooking.freelancer.toString(),
            status: updatedBooking.status
        });

        if (createdTransaction) {
            logger.info("Transaction created", {
                transactionId:
                    createdTransaction._id.toString(),
                bookingId:
                    updatedBooking._id.toString(),
                clientId:
                    createdTransaction.client.toString(),
                freelancerId:
                    createdTransaction.freelancer.toString(),
                amount:
                    createdTransaction.amount,
                status:
                    createdTransaction.status
            });
        }

        return res.status(200).json({
            success: true,
            message:
                decision === "accept"
                    ? "Booking request accepted successfully."
                    : "Booking request rejected.",
            booking: updatedBooking,
            ...(createdTransaction && {
                transaction: createdTransaction
            })
        });
    } catch (error) {
        if (error.statusCode) {
            return res.status(error.statusCode).json({
                success: false,
                message: error.message
            });
        }

        if (error.code === 11000) {
            return res.status(409).json({
                success: false,
                message:
                    "This booking request has already been processed."
            });
        }

        next(error);
    } finally {
        if (session) {
            await session.endSession();
        }
    }
}

// Freelancer accepts a booking request
async function acceptBooking(req, res, next) {
    return reviewBooking(req, res, next, "accept");
}

// Freelancer rejects a booking request
async function rejectBooking(req, res, next) {
    return reviewBooking(req, res, next, "reject");
}

// Client views their own bookings
async function getMyClientBookings(req, res, next) {
    try {
        const bookings = await Booking.find({
            client: req.user.sub
        })
            .populate("gig", "title category")
            .populate("freelancer", "name email")
            .sort({ createdAt: -1 });

        return res.status(200).json({
            success: true,
            count: bookings.length,
            bookings
        });
    } catch (error) {
        next(error);
    }
}

// Freelancer views bookings made against their gigs
async function getFreelancerBookings(req, res, next) {
    try {
        const bookings = await Booking.find({
            freelancer: req.user.sub
        })
            .populate("gig", "title category")
            .populate("client", "name email")
            .sort({ createdAt: -1 });

        return res.status(200).json({
            success: true,
            count: bookings.length,
            bookings
        });
    } catch (error) {
        next(error);
    }
}

// Freelancer views income from completed simulated transactions
async function getFreelancerIncome(req, res, next) {
    try {
        const transactions = await Transaction.find({
            freelancer: req.user.sub,
            status: "completed"
        })
            .populate("booking")
            .sort({ createdAt: -1 });

        const totalIncome = transactions.reduce(
            (total, transaction) =>
                total + transaction.amount,
            0
        );

        const taxSummary =
            calculateEstimatedTax(totalIncome);

        return res.status(200).json({
            success: true,
            totalIncome,
            transactionCount: transactions.length,

            financialSummary: {
                grossIncome:
                    taxSummary.grossIncome,

                estimatedTax:
                    taxSummary.estimatedTax,

                estimatedNetIncome:
                    taxSummary.estimatedNetIncome,

                effectiveTaxRate:
                    taxSummary.effectiveTaxRate
            },

            taxInformation: {
                type: "estimated",
                disclaimer:
                    "This is an approximate tax estimate for educational purposes and is not official tax advice."
            },

            transactions
        });
    } catch (error) {
        next(error);
    }
}

module.exports = {
    createBooking,
    acceptBooking,
    rejectBooking,
    getMyClientBookings,
    getFreelancerBookings,
    getFreelancerIncome
};

// Code attributes

/*
Title: Nutrisense: Implementing Logging and Monitoring with BetterStack
Author(s): Dustin Pearson
Date: 2024
Version: Not applicable
Type: Medium article
Availability: https://dustin-nutrisense.medium.com/nutrisense-logging-and-monitoring-with-betterstack-5c098cdbde2e
*/