const { body } = require("express-validator");

const createGigValidation = [
    body("title")
    .isString()
        .trim()
        .notEmpty()
        .withMessage("Title is required.")
        .isLength({ min: 3, max: 100 })
        .withMessage("Title must be between 3 and 100 characters.")
        .escape(),

    body("description")
    .isString()
        .trim()
        .notEmpty()
        .withMessage("Description is required.")
        .isLength({ min: 10, max: 1000 })
        .withMessage("Description must be between 10 and 1000 characters.")
        .escape(),

    body("category")
    .isString()
        .trim()
        .notEmpty()
        .withMessage("Category is required.")
        .isLength({ max: 50 })
        .withMessage("Category must not exceed 50 characters.")
        .escape(),

    body("price")
        .notEmpty()
        .withMessage("Price is required.")
        .isFloat({ min: 1 })
        .withMessage("Price must be at least 1.")
        .toFloat()
];

const updateGigValidation = [
    body("title")
        .optional()
        .isString()
        .trim()
        .isLength({ min: 3, max: 100 })
        .withMessage("Title must be between 3 and 100 characters.")
        .escape(),

    body("description")
        .optional()
        .isString()
        .trim()
        .isLength({ min: 10, max: 1000 })
        .withMessage("Description must be between 10 and 1000 characters.")
        .escape(),

    body("category")
        .optional()
        .isString()
        .trim()
        .isLength({ max: 50 })
        .withMessage("Category must not exceed 50 characters.")
        .escape(),

    body("price")
        .optional()
        .isFloat({ min: 1 })
        .withMessage("Price must be at least 1.")
        .toFloat(),

    body("isActive")
        .optional()
        .isBoolean()
        .withMessage("isActive must be true or false.")
        .toBoolean()
];

module.exports = {
    createGigValidation,
    updateGigValidation
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