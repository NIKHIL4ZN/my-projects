const { body } = require("express-validator");

const createBookingValidation = [
    body("gigId")
    .isString()
        .withMessage("Gig ID must be a string.")
        .trim()
        .notEmpty()
        .withMessage("Gig ID is required.")
        .isMongoId()
        .withMessage("Gig ID must be a valid MongoDB ID.")
];

module.exports = {
    createBookingValidation
};

// Code attributes

/*
Title: Getting Started
Author(s): express-validator Contributors
Date: n.d.
Version: 7.3.0
Type: Documentation
Availability: https://express-validator.github.io/docs/guides/getting-started/
*/