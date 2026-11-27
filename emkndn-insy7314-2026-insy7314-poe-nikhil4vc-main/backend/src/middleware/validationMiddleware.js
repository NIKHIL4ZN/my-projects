const { validationResult } = require("express-validator");

function handleValidationErrors(req, res, next) {
    const errors = validationResult(req);

    if (!errors.isEmpty()) {
        return res.status(400).json({
            success: false,
            message: "Validation failed.",
            errors: errors.array().map((error) => ({
                field: error.path,
                message: error.msg
            }))
        });
    }

    next();
}

module.exports = {
    handleValidationErrors
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