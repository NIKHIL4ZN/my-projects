package com.familylinklive.api

import com.familylinklive.api.db.DatabaseFactory
import com.familylinklive.api.plugins.configureSecurity
import com.familylinklive.api.plugins.configureSerialization
import com.familylinklive.api.routes.authRoutes
import com.familylinklive.api.routes.chatRoutes
import com.familylinklive.api.routes.contactsRoutes
import com.familylinklive.api.routes.locationRoutes
import com.familylinklive.api.routes.placesRoutes
import com.familylinklive.api.routes.sosRoutes
import com.familylinklive.api.routes.userRoutes
import io.ktor.http.*
import io.ktor.server.application.*
import io.ktor.server.auth.*
import io.ktor.server.engine.*
import io.ktor.server.netty.*
import io.ktor.server.plugins.callloging.*
import io.ktor.server.plugins.cors.routing.*
import io.ktor.server.plugins.statuspages.*
import io.ktor.server.response.*
import io.ktor.server.routing.*
import org.slf4j.event.Level

/**
 * FamilyLink Live REST API - the custom, self-hosted backend described in
 * the Part 1 Planning and Design document, Section 5 (reduced to the
 * endpoints the Part 2 prototype implements end-to-end).
 *
 * Run locally with: ./gradlew run   (see backend/README section in the
 * project root README.md for full setup instructions).
 */
/*
Title: Create a server - embeddedServer with the Netty engine
Author(s): JetBrains
Date: 2024
Version: Ktor 2.3.12
Type: Documentation
Availability: https://ktor.io/docs/server-create-and-configure.html
*/
fun main() {
    val port = findAvailablePort(System.getenv("PORT")?.toIntOrNull() ?: 8080)
    embeddedServer(
        Netty,
        port = port,
        host = "0.0.0.0",
    ) {
        module()
    }.start(wait = true)
}

private fun findAvailablePort(defaultPort: Int): Int {
    if (System.getenv("PORT") != null) return defaultPort
    var p = defaultPort
    while (p < (defaultPort + 10)) {
        try {
            java.net.ServerSocket(p).use { return p }
        } catch (_: Exception) {
            println("Port $p is already in use; trying ${p + 1}...")
            p++
        }
    }
    return defaultPort
}

fun Application.module() {
    FirebaseAdmin.init()
    DatabaseFactory.init()

    install(CallLogging) { level = Level.INFO }

    /*
    Title: CORS - Cross-Origin Resource Sharing plugin
    Author(s): JetBrains
    Date: 2024
    Version: Ktor 2.3.12
    Type: Documentation
    Availability: https://ktor.io/docs/server-cors.html
    */
    install(CORS) {
        anyHost()
        allowHeader(HttpHeaders.ContentType)
        allowHeader(HttpHeaders.Authorization)
        allowMethod(HttpMethod.Get)
        allowMethod(HttpMethod.Post)
        allowMethod(HttpMethod.Patch)
        allowMethod(HttpMethod.Delete)
    }

    /*
    Title: Status pages - handling exceptions and mapping them to HTTP
    status codes
    Author(s): JetBrains
    Date: 2024
    Version: Ktor 2.3.12
    Type: Documentation
    Availability: https://ktor.io/docs/server-status-pages.html
    */
    install(StatusPages) {
        exception<io.ktor.server.plugins.BadRequestException> { call, cause ->
            call.respond(
                HttpStatusCode.BadRequest,
                mapOf("error" to (cause.message ?: "Malformed request body"))
            )
        }
        exception<Throwable> { call, cause ->
            call.respond(
                HttpStatusCode.InternalServerError,
                mapOf("error" to (cause.message ?: "Unexpected server error"))
            )
        }
    }

    configureSerialization()
    configureSecurity()

    /*
    Title: Routing - defining routes with the routing DSL
    Author(s): JetBrains
    Date: 2024
    Version: Ktor 2.3.12
    Type: Documentation
    Availability: https://ktor.io/docs/server-routing.html
    */
    routing {
        get("/") { call.respondText("FamilyLink Live API is running.") }

        authRoutes()

        authenticate("auth-jwt") {
            userRoutes()
            contactsRoutes()
            locationRoutes()
            placesRoutes()
            chatRoutes()
            sosRoutes()
        }
    }
}
