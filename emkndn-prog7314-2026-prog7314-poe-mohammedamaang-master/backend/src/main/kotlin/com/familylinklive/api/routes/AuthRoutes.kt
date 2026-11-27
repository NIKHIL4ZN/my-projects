package com.familylinklive.api.routes

import com.familylinklive.api.FirebaseAdmin
import com.familylinklive.api.db.DatabaseFactory.dbQuery
import com.familylinklive.api.db.Users
import com.familylinklive.api.models.LoginRequest
import com.familylinklive.api.models.LoginResponse
import com.familylinklive.api.models.toUserDto
import com.familylinklive.api.plugins.JwtConfig
import io.ktor.http.*
import io.ktor.server.application.*
import io.ktor.server.request.*
import io.ktor.server.response.*
import io.ktor.server.routing.*
import org.jetbrains.exposed.sql.insert
import org.jetbrains.exposed.sql.selectAll
import java.util.UUID

/**
 * POST /auth/login (REQ-1, REQ-2): exchanges a verified Firebase ID token
 * for our own application JWT, creating the user record on first login.
 */
fun Route.authRoutes() {
    post("/auth/login") {
        val request = call.receive<LoginRequest>()

        val firebaseUser = try {
            FirebaseAdmin.verifyIdToken(request.firebaseIdToken)
        } catch (e: Exception) {
            return@post call.respond(
                HttpStatusCode.Unauthorized,
                mapOf("error" to "Invalid Firebase ID token: ${e.message}")
            )
        }

        /*
        Title: DSL - querying, inserting, and updating rows with Exposed's
        type-safe query builder (selectAll().where{}, insert{}, update{})
        Author(s): JetBrains
        Date: 2024
        Version: Exposed 0.52.0
        Type: Documentation
        Availability: https://github.com/JetBrains/Exposed/wiki/DSL
        This query-building pattern is used identically throughout
        ContactsRoutes.kt, LocationRoutes.kt, PlacesRoutes.kt, ChatRoutes.kt
        and UserRoutes.kt.
        */
        val userId = dbQuery {
            val existing = Users.selectAll().where { Users.firebaseUid eq firebaseUser.uid }.singleOrNull()
            if (existing != null) {
                existing[Users.id]
            } else {
                val newId = UUID.randomUUID().toString()
                Users.insert {
                    it[id] = newId
                    it[firebaseUid] = firebaseUser.uid
                    it[displayName] = firebaseUser.name
                    it[email] = firebaseUser.email.trim().lowercase()
                    it[photoUrl] = firebaseUser.picture
                }
                newId
            }
        }

        val userDto = dbQuery { Users.selectAll().where { Users.id eq userId }.single().toUserDto() }

        call.respond(LoginResponse(token = JwtConfig.generateToken(userId), user = userDto))
    }
}
