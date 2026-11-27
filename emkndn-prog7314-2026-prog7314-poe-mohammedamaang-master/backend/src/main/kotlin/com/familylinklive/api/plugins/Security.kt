package com.familylinklive.api.plugins

import com.auth0.jwt.JWT
import com.auth0.jwt.algorithms.Algorithm
import io.ktor.http.*
import io.ktor.server.application.*
import io.ktor.server.auth.*
import io.ktor.server.auth.jwt.*
import io.ktor.server.response.*
import java.util.Date

/**
 * Issues and validates the app's own JWT (separate from the Firebase ID
 * token), so the mobile app only needs to re-authenticate with Firebase
 * once and then uses this shorter-lived, backend-issued token for every
 * REST call (REQ-11).
 */
/*
Title: java-jwt - a Java implementation of JSON Web Token (JWT), used here
to create and verify the app's HMAC-signed tokens
Author(s): Auth0
Date: 2023
Version: 4.4.0
Type: Documentation
Availability: https://github.com/auth0/java-jwt
*/
object JwtConfig {
    private val secret = System.getenv("JWT_SECRET") ?: "dev-secret-change-me-in-production"
    private const val issuer = "familylink-live-api"
    private const val audience = "familylink-live-app"
    private val algorithm = Algorithm.HMAC256(secret)

    val verifier = JWT.require(algorithm).withIssuer(issuer).build()

    fun generateToken(userId: String): String = JWT.create()
        .withIssuer(issuer)
        .withAudience(audience)
        .withClaim("userId", userId)
        .withExpiresAt(Date(System.currentTimeMillis() + 24 * 60 * 60 * 1000))
        .sign(algorithm)
}

/*
Title: JWT authentication - validating a JWT sent by the client and
exposing it as a JWTPrincipal
Author(s): JetBrains
Date: 2024
Version: Ktor 2.3.12
Type: Documentation
Availability: https://ktor.io/docs/server-jwt.html
*/
fun Application.configureSecurity() {
    install(Authentication) {
        jwt("auth-jwt") {
            verifier(JwtConfig.verifier)
            validate { credential ->
                val userId = credential.payload.getClaim("userId").asString()
                if (!userId.isNullOrEmpty()) JWTPrincipal(credential.payload) else null
            }
            challenge { _, _ ->
                call.respond(HttpStatusCode.Unauthorized, mapOf("error" to "Token is missing, invalid or expired"))
            }
        }
    }
}
