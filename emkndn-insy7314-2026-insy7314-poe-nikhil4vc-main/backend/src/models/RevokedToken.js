const mongoose = require("mongoose");

const revokedTokenSchema = new mongoose.Schema(
    {
        tokenHash: {
            type: String,
            required: true,
            unique: true,
            index: true
        },

        expiresAt: {
            type: Date,
            required: true,
            index: {
                expires: 0
            }
        }
    },
    {
        timestamps: true
    }
);

module.exports = mongoose.model(
    "RevokedToken",
    revokedTokenSchema
);

// Code attributes

/*
Title: Revoking Access to JWT Tokens with a Blacklist/Deny List
Author(s): Super Tokens
Date: 2022
Version: Not applicable
Type: Medium article
Availability: https://supertokens.medium.com/revoking-access-to-jwt-tokens-with-a-blacklist-deny-list-2ceae6096f
*/