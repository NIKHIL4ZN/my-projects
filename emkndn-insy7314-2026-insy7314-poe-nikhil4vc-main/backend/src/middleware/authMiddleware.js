const jwt = require("jsonwebtoken");
const {isTokenRevoked} = require("../utils/tokenRevocation");

const {
    jwtSecret
} = require("../config/env");

async function authenticateToken(req, res, next) {
    const authHeader = req.headers.authorization;

    if (!authHeader || !authHeader.startsWith("Bearer ")) {
        return res.status(401).json({
            success: false,
            message: "Authentication token is required."
        });
    }

    const token = authHeader.split(" ")[1];

    try {
        const decoded = jwt.verify(
            token,
            jwtSecret
        );

        const revoked = await isTokenRevoked(token);

if (revoked) {
    return res.status(401).json({
        success: false,
        message: "Authentication token has been revoked."
    });
}

        req.user = decoded;

        next();
    } catch (error) {
        if (error.name === "TokenExpiredError") {
            return res.status(401).json({
                success: false,
                message: "Authentication token has expired."
            });
        }

        return res.status(401).json({
            success: false,
            message: "Invalid authentication token."
        });
    }
}

module.exports = {
    authenticateToken
};

// Code attributes

/*
Title: JWT Authentication in Node.js
Author(s): GeeksforGeeks
Date: 2026
Version: Not applicable
Type: Online tutorial
Availability: https://www.geeksforgeeks.org/node-js/jwt-authentication-with-node-js/
*/

/*
Title: Revoking Access to JWT Tokens with a Blacklist/Deny List
Author(s): Super Tokens
Date: 2022
Version: Not applicable
Type: Medium article
Availability: https://supertokens.medium.com/revoking-access-to-jwt-tokens-with-a-blacklist-deny-list-2ceae6096f
*/

/*
Title: Nutrisense: Implementing Logging and Monitoring with BetterStack
Author(s): Dustin Pearson
Date: 2024
Version: Not applicable
Type: Medium article
Availability: https://dustin-nutrisense.medium.com/nutrisense-logging-and-monitoring-with-betterstack-5c098cdbde2e
*/