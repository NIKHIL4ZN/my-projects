const bcrypt = require("bcryptjs");
const jwt = require("jsonwebtoken");
const {revokeToken} = require("../utils/tokenRevocation");
const logger = require("../utils/logger");

const {
    findUserByEmail,
    findUserById,
    createUser,
    saveUser
} = require("../models/userModel");

const {
    jwtSecret,
    jwtExpiresIn
} = require("../config/env");

const MAX_FAILED_LOGIN_ATTEMPTS = 5;
const ACCOUNT_LOCK_TIME = 15 * 60 * 1000;

// Register
async function register(req, res, next) {
    try {
        const {
            name,
            email,
            password,
            role = "client"
        } = req.body;

        const existingUser = await findUserByEmail(email);

        if (existingUser) {
            return res.status(409).json({
                success: false,
                message: "An account with this email already exists."
            });
        }

        const passwordHash = await bcrypt.hash(password, 12);

        const user = {
            name,
            email: email.toLowerCase(),
            passwordHash,
            role,
            createdAt: new Date().toISOString()
        };

        const savedUser = await createUser(user);

        return res.status(201).json({
            success: true,
            message: "Account created successfully.",
            user: {
                id: savedUser._id.toString(),
                name: savedUser.name,
                email: savedUser.email,
                role: savedUser.role
            }
        });
    } catch (error) {
        next(error);
    }
}

// Login
async function login(req, res, next) {
    try {
        const { email, password } = req.body;

        const user = await findUserByEmail(email);

        if (!user) {
            logger.warn(
                "Failed login attempt",
                {
                    email,
                    reason: "User not found"
                }
            );

            return res.status(401).json({
                success: false,
                message: "Invalid email or password."
            });
        }

        // Prevent login while the account is temporarily locked
        if (
            user.lockUntil &&
            user.lockUntil > new Date()
        ) {
            logger.warn(
                "Login attempt on locked account",
                {
                    userId: user._id.toString(),
                    email: user.email,
                    lockUntil: user.lockUntil
                }
            );

            return res.status(423).json({
                success: false,
                message:
                    "Too many failed login attempts. Please wait 15 minutes before trying again."
            });
        }

        // Reset the lock if the lock period has expired
        if (
            user.lockUntil &&
            user.lockUntil <= new Date()
        ) {
            user.failedLoginAttempts = 0;
            user.lockUntil = null;

            await saveUser(user);
        }

        const passwordMatches = await bcrypt.compare(
            password,
            user.passwordHash
        );

        if (!passwordMatches) {
            user.failedLoginAttempts =
                (user.failedLoginAttempts || 0) + 1;

            logger.warn(
                "Failed login attempt",
                {
                    userId: user._id.toString(),
                    email: user.email,
                    reason: "Incorrect password",
                    failedLoginAttempts:
                        user.failedLoginAttempts
                }
            );

            if (
                user.failedLoginAttempts >=
                MAX_FAILED_LOGIN_ATTEMPTS
            ) {
                user.lockUntil = new Date(
                    Date.now() + ACCOUNT_LOCK_TIME
                );

                logger.warn(
                    "Account locked",
                    {
                        userId: user._id.toString(),
                        email: user.email,
                        failedLoginAttempts:
                            user.failedLoginAttempts,
                        lockUntil: user.lockUntil
                    }
                );

                await saveUser(user);

                return res.status(423).json({
                    success: false,
                    message:
                        "Too many failed login attempts. Please wait 15 minutes before trying again."
                });
            }

            await saveUser(user);

            return res.status(401).json({
                success: false,
                message: "Invalid email or password."
            });
        }

        // Successful login clears previous failed attempts
        if (
            user.failedLoginAttempts > 0 ||
            user.lockUntil
        ) {
            user.failedLoginAttempts = 0;
            user.lockUntil = null;

            await saveUser(user);
        }

        logger.info(
            "Successful login",
            {
                userId: user._id.toString(),
                email: user.email,
                role: user.role
            }
        );

        const token = jwt.sign(
            {
                sub: user._id.toString(),
                email: user.email,
                role: user.role
            },
            jwtSecret,
            {
                expiresIn: jwtExpiresIn
            }
        );

        return res.status(200).json({
            success: true,
            message: "Login successful.",
            token,
            user: {
                id: user._id.toString(),
                name: user.name,
                email: user.email,
                role: user.role
            }
        });
    } catch (error) {
        next(error);
    }
}

// Logs user out and adds JWT to denylist
async function logout(req, res, next) {
    try {
        const authHeader = req.headers.authorization;

        const token = authHeader.split(" ")[1];

        const expiresAt = new Date(
            req.user.exp * 1000
        );

        await revokeToken(
            token,
            expiresAt
        );

        logger.info(
    "User logged out",
    {
        userId: req.user.sub,
        email: req.user.email,
        role: req.user.role
    }
);

        return res.status(200).json({
            success: true,
            message: "Logout successful."
        });
    } catch (error) {
        next(error);
    }
}

// Gets current user
async function getCurrentUser(req, res, next) {
    try {
        const user = await findUserById(req.user.sub);

        if (!user) {
            return res.status(404).json({
                success: false,
                message: "User account not found."
            });
        }

        return res.status(200).json({
            success: true,
            user: {
                id: user.id,
                name: user.name,
                email: user.email,
                role: user.role
            }
        });
    } catch (error) {
        next(error);
    }
}

module.exports = {
    register,
    login,
    logout,
    getCurrentUser
};

// Code attributes

/*
Title: How to Hash Passwords with bcrypt in Node.js
Author(s): Dennis Temoye Charity
Date: 2024
Version: Not applicable
Type: Online tutorial
Availability: https://www.freecodecamp.org/news/how-to-hash-passwords-with-bcrypt-in-nodejs/
*/

/*
Title: Nutrisense: Implementing Logging and Monitoring with BetterStack
Author(s): Dustin Pearson
Date: 2024
Version: Not applicable
Type: Medium article
Availability: https://dustin-nutrisense.medium.com/nutrisense-logging-and-monitoring-with-betterstack-5c098cdbde2e
*/