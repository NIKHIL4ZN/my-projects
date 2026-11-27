const crypto = require("crypto");

const RevokedToken = require("../models/RevokedToken");

function hashToken(token) {
    return crypto
        .createHash("sha256")
        .update(token)
        .digest("hex");
}

async function revokeToken(token, expiresAt) {
    const tokenHash = hashToken(token);

    await RevokedToken.create({
        tokenHash,
        expiresAt
    });
}

async function isTokenRevoked(token) {
    const tokenHash = hashToken(token);

    const revokedToken = await RevokedToken.findOne({
        tokenHash
    });

    return Boolean(revokedToken);
}

module.exports = {
    revokeToken,
    isTokenRevoked
};

// Code attributes

/*
Title: Revoking Access to JWT Tokens with a Blacklist/Deny List
Author(s): Super Tokens
Date: 2022
Version: Not applicable
Type: Medium article
Availability: https://supertokens.medium.com/revoking-access-to-jwt-tokens-with-a-blacklist-deny-list-2ceae6096f
*/