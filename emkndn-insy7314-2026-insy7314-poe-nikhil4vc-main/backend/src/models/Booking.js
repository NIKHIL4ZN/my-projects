const mongoose = require("mongoose");

const bookingSchema = new mongoose.Schema(
    {
        gig: {
            type: mongoose.Schema.Types.ObjectId,
            ref: "Gig",
            required: true
        },

        client: {
            type: mongoose.Schema.Types.ObjectId,
            ref: "User",
            required: true
        },

        freelancer: {
            type: mongoose.Schema.Types.ObjectId,
            ref: "User",
            required: true
        },

        price: {
            type: Number,
            required: true,
            min: 1
        },

        status: {
            type: String,
            enum: [
                "pending",
                "booked",
                "rejected",
                "completed",
                "cancelled"
            ],
            default: "pending"
        }
    },
    {
        timestamps: true
    }
);

/*
A client can only have one pending or confirmed
booking for the same gig at a time.
*/
bookingSchema.index(
    {
        client: 1,
        gig: 1
    },
    {
        unique: true,
        partialFilterExpression: {
            status: {
                $in: ["pending", "booked"]
            }
        }
    }
);

module.exports = mongoose.model("Booking", bookingSchema);