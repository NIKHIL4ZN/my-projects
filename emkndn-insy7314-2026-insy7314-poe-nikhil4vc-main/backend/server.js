require("dotenv").config();

const fs = require("fs");
const https = require("https");
const path = require("path");

const app = require("./src/app");
const { connectDatabase } = require("./src/config/database");

const PORT = process.env.HTTPS_PORT || 5000;

const sslOptions = {
    key: fs.readFileSync(
        path.join(__dirname, "certificates", "server.key")
    ),
    cert: fs.readFileSync(
        path.join(__dirname, "certificates", "server.crt")
    )
};

async function startServer() {
    await connectDatabase();

    https.createServer(sslOptions, app).listen(PORT, () => {
        console.log(
            `HustleHub+ HTTPS server running on https://localhost:${PORT}`
        );
    });
}

startServer();

// Code attributes

/*
Title: Configure Git to accept a particular self-signed server certificate for a particular HTTPS remote
Author(s): zwol
Date: 2012
Version: Not applicable
Type: Stack Overflow question
Availability: https://stackoverflow.com/questions/9072376/configure-git-to-accept-a-particular-self-signed-server-certificate-for-a-partic
*/


