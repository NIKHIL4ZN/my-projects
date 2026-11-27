
const mongoose = require("mongoose");

async function connectDatabase() {
    try {
        const isTestEnvironment =
            process.env.NODE_ENV === "test";

        let connectionUri;
        let connectionOptions = {};

        if (isTestEnvironment) {
            // Automated tests must use a dedicated
            // MongoDB connection configuration.
            connectionUri = process.env.MONGODB_E2E_URI;

            if (!connectionUri) {
                throw new Error(
                    "MONGODB_E2E_URI must be configured for automated tests."
                );
            }

            // Explicitly select a separate database
            // for Playwright E2E test data.
            connectionOptions = {
                dbName: "hustlehub_e2e"
            };
        } else {
            // Preserve the existing database connection
            // for normal development and production.
            connectionUri = process.env.MONGODB_URI;
        }

        if (!connectionUri) {
            throw new Error(
                "MongoDB connection URI is not configured."
            );
        }

        await mongoose.connect(
            connectionUri,
            connectionOptions
        );

        console.log("MongoDB connected successfully.");
        console.log(
            `Database: ${mongoose.connection.name}`
        );
    } catch (error) {
        console.error(
            "MongoDB connection failed:",
            error.message
        );

        process.exit(1);
    }
}

module.exports = {
    connectDatabase
};

// Code attributes

/*
Title: How to Use MERN Stack: A Complete Guide
Author(s): MongoDB
Date: n.d.
Version: Node.js 20.11.0
Type: Tutorial
Availability: https://www.mongodb.com/resources/languages/mern-stack-tutorial
*/
