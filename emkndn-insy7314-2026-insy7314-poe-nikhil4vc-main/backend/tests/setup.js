require("dotenv").config();

const mongoose = require("mongoose");
const { connectDatabase } = require("../src/config/database");

beforeAll(async () => {
    await connectDatabase();
});

afterAll(async () => {
    await mongoose.connection.close();
});

// Code attributes

/*
Title: Node.js Express Testing with Jest and SuperTest
Author(s): Ermias Asmare
Date: 2023
Version: Not applicable
Type: Medium article
Availability: https://medium.com/@it.ermias.asmare/node-js-express-with-jest-and-supertest-e58aaf4c4514
*/