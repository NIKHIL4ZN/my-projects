const express = require("express");
const helmet = require("helmet");
const cors = require("cors");
const rateLimit = require("express-rate-limit");
const logger = require("./utils/logger");

const app = express();

const authRoutes = require("./routes/authRoutes");
const gigRoutes = require("./routes/gigRoutes");
const bookingRoutes = require("./routes/bookingRoutes");
const userRoutes = require("./routes/userRoutes");

// Security headers
app.use(
    helmet({
        contentSecurityPolicy: {
            directives: {
                defaultSrc: ["'self'"],
                scriptSrc: ["'self'"],
                styleSrc: ["'self'"],
                imgSrc: ["'self'", "data:"],
                connectSrc: ["'self'"],
                objectSrc: ["'none'"],
                baseUri: ["'self'"],
                frameAncestors: ["'none'"],
                formAction: ["'self'"]
            }
        },
        crossOriginResourcePolicy: {
            policy: "same-site"
        }
    })
);

// Allow frontend/API communication only from trusted origins
const allowedOrigins = [
    "http://localhost:5173",
    "http://localhost:8080"
];

// Allow frontend/API communication
app.use(
    cors({
        origin(origin, callback) {
            // Allow requests without an Origin header,
            // such as Postman/Newman and server-to-server requests.
            if (!origin || allowedOrigins.includes(origin)) {
                return callback(null, true);
            }

            return callback(
                new Error("Origin not allowed by CORS.")
            );
        },
        credentials: true
    })
);

// Parse JSON request bodies
app.use(express.json({ limit: "10kb" }));

// Rate limiting
const generalLimiter = rateLimit({
    windowMs: 15 * 60 * 1000,
    max: 100,
    standardHeaders: true,
    legacyHeaders: false,
    message: {
        success: false,
        message: "Too many requests. Please try again later."
    }
});

// Disable rate limiting during automated tests
if (process.env.NODE_ENV !== "test") {
    app.use(generalLimiter);
}

app.use("/api/auth", authRoutes);
app.use("/api/gigs", gigRoutes);
app.use("/api/bookings", bookingRoutes);
app.use("/api/users", userRoutes);

// Health check
app.get("/api/health", (req, res) => {
    res.status(200).json({
        success: true,
        message: "HustleHub+ API is running"
    });
});

// 404 handler
app.use((req, res) => {
    res.status(404).json({
        success: false,
        message: "Route not found"
    });
});

// Global error handler
app.use((err, req, res, _next) => {
    logger.error(
        "Unhandled application error",
        {
            method: req.method,
            path: req.originalUrl,
            errorName: err.name,
            errorMessage: err.message
        }
    );

    res.status(500).json({
        success: false,
        message: "An internal server error occurred."
    });
});

module.exports = app;

// Code attributes

/*
Title: Security Headers Made Simple: A Practical Guide
Author(s): Aanchalgoel
Date: 2025
Version: Not applicable
Type: Medium article
Availability: https://medium.com/@aanchalgoel11678/security-headers-made-simple-a-practical-guide-3c58c00e0f8d
*/

/*
Title: A Developer's Guide to HTTP Security Headers
Author(s): Barrion
Date: 2026
Version: Not applicable
Type: Online security guide
Availability: https://barrion.io/blog/security-headers-guide
*/

/*
Title: CORS Middleware
Author(s): Troy Goode and Express.js Contributors
Date: n.d.
Version: 2.8.6
Type: Documentation
Availability: https://expressjs.com/en/resources/middleware/cors/
*/

/*
Title: Rate Limiting in System Design
Author(s): GeeksforGeeks
Date: 2026
Version: Not applicable
Type: Online article
Availability: https://www.geeksforgeeks.org/system-design/rate-limiting-in-system-design/
*/

/*
Title: Nutrisense: Implementing Logging and Monitoring with BetterStack
Author(s): Dustin Pearson
Date: 2024
Version: Not applicable
Type: Medium article
Availability: https://dustin-nutrisense.medium.com/nutrisense-logging-and-monitoring-with-betterstack-5c098cdbde2e
*/