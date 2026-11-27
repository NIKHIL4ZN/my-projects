const { Logtail } = require("@logtail/node");

const isTestEnvironment =
    process.env.NODE_ENV === "test";

let logtail = null;

if (
    !isTestEnvironment &&
    process.env.LOGTAIL_SOURCE_TOKEN &&
    process.env.LOGTAIL_INGESTING_HOST
) {
    logtail = new Logtail(
    process.env.LOGTAIL_SOURCE_TOKEN,
    {
        endpoint:
            `https://${process.env.LOGTAIL_INGESTING_HOST}`
    }
);
}

function info(message, context = {}) {
    if (isTestEnvironment) {
        return;
    }

    console.log(`[INFO] ${message}`, context);

    if (logtail) {
        logtail.info(message, context);
    }
}

function warn(message, context = {}) {
    if (isTestEnvironment) {
        return;
    }

    console.warn(`[WARN] ${message}`, context);

    if (logtail) {
        logtail.warn(message, context);
    }
}

function error(message, context = {}) {
    if (isTestEnvironment) {
        return;
    }

    console.error(`[ERROR] ${message}`, context);

    if (logtail) {
        logtail.error(message, context);
    }
}

module.exports = {
    info,
    warn,
    error
};

// Code attributes

/*
Title: Nutrisense: Implementing Logging and Monitoring with BetterStack
Author(s): Dustin Pearson
Date: 2024
Version: Not applicable
Type: Medium article
Availability: https://dustin-nutrisense.medium.com/nutrisense-logging-and-monitoring-with-betterstack-5c098cdbde2e
*/