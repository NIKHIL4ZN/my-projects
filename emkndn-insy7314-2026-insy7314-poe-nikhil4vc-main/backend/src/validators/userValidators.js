const { body } = require("express-validator");

const updateRoleValidation = [
    body("role")
        .notEmpty()
        .withMessage("Role is required.")
        .isIn([
            "client",
            "freelancer",
            "admin"
        ])
        .withMessage(
            "Role must be client, freelancer or admin."
        )
];

module.exports = {
    updateRoleValidation
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