package com.familylinklive.api.db

import kotlinx.coroutines.Dispatchers
import org.jetbrains.exposed.sql.Database
import org.jetbrains.exposed.sql.SchemaUtils
import org.jetbrains.exposed.sql.transactions.experimental.newSuspendedTransaction
import org.jetbrains.exposed.sql.transactions.transaction

/**
 * File-based H2 database (see Section 5.3 of the Part 1 design for the
 * planned production equivalent: PostgreSQL on Azure Database for
 * PostgreSQL). H2 is used here so the backend runs anywhere with zero
 * external setup, which keeps Part 2 easy to demo and to run in CI.
 */
object DatabaseFactory {

    private lateinit var database: Database

    /*
    Title: Getting started with Exposed - connecting to a database and
    creating tables
    Author(s): JetBrains
    Date: 2024
    Version: Exposed 0.52.0
    Type: Documentation
    Availability: https://github.com/JetBrains/Exposed/wiki/Getting-Started
    */
    fun init() {
        val databaseUrl = System.getenv("DATABASE_URL")
        database = if (!databaseUrl.isNullOrBlank()) {
            connectPostgres(databaseUrl)
        } else {
            val dbPath = System.getenv("DB_PATH") ?: "./familylink-data"
            println("Database: embedded H2 file at $dbPath (set DATABASE_URL to use PostgreSQL)")
            Database.connect(
                url = "jdbc:h2:file:$dbPath;DB_CLOSE_ON_EXIT=FALSE",
                driver = "org.h2.Driver"
            )
        }
        transaction(database) {
            SchemaUtils.createMissingTablesAndColumns(Users, Contacts, LocationPings, Places, Messages, DeviceTokens, SosEvents)
        }
    }

    /**
     * Part 3: persistent PostgreSQL for hosted deployments. Accepts the usual
     * provider connection string, e.g.  postgresql://user:password@host:5432/dbname
     * and converts it into a JDBC URL + credentials. Render's free tier wipes
     * the container disk every time the service sleeps, so an embedded H2
     * file would lose all users/contacts/messages - an external database
     * does not.
     */
    private fun connectPostgres(raw: String): Database {
        val uri = java.net.URI(raw.trim())
        val userInfo = (uri.userInfo ?: "").split(":", limit = 2)
        val port = if (uri.port == -1) 5432 else uri.port
        val jdbcUrl = "jdbc:postgresql://${uri.host}:$port${uri.path}?sslmode=require"
        println("Database: PostgreSQL at ${uri.host}:$port${uri.path}")
        return Database.connect(
            url = jdbcUrl,
            driver = "org.postgresql.Driver",
            user = userInfo.getOrElse(0) { "" },
            password = userInfo.getOrElse(1) { "" }
        )
    }

    /*
    Title: Transactions - running database transactions on a coroutine
    dispatcher with newSuspendedTransaction
    Author(s): JetBrains
    Date: 2024
    Version: Exposed 0.52.0
    Type: Documentation
    Availability: https://github.com/JetBrains/Exposed/wiki/Transactions
    */
    suspend fun <T> dbQuery(block: () -> T): T =
        newSuspendedTransaction(Dispatchers.IO, database) { block() }
}
