package com.familylinklive.api.routes

import com.familylinklive.api.db.Contacts
import com.familylinklive.api.db.DatabaseFactory.dbQuery
import com.familylinklive.api.db.DeviceTokens
import com.familylinklive.api.db.LocationPings
import com.familylinklive.api.db.Messages
import com.familylinklive.api.db.Places
import com.familylinklive.api.db.SosEvents
import com.familylinklive.api.db.Users
import com.familylinklive.api.models.FcmTokenRequest
import com.familylinklive.api.models.SettingsRequest
import com.familylinklive.api.models.toUserDto
import com.familylinklive.api.notifications.AccountCleanup
import com.familylinklive.api.storage.BlobStorage
import io.ktor.http.*
import io.ktor.server.application.*
import io.ktor.server.auth.*
import io.ktor.server.auth.jwt.*
import io.ktor.server.request.*
import io.ktor.server.response.*
import io.ktor.server.routing.*
import org.jetbrains.exposed.sql.SqlExpressionBuilder.eq
import org.jetbrains.exposed.sql.SqlExpressionBuilder.inList
import org.jetbrains.exposed.sql.and
import org.jetbrains.exposed.sql.deleteWhere
//import org.jetbrains.exposed.sql.inList
import org.jetbrains.exposed.sql.or
import org.jetbrains.exposed.sql.insert
import org.jetbrains.exposed.sql.selectAll
import org.jetbrains.exposed.sql.update

private val SUPPORTED_LANGUAGES = setOf("EN", "AF", "ZU")
private const val MAX_PHOTO_BYTES = 2 * 1024 * 1024

// Exposed DSL query-building pattern - see the reference comment in
// AuthRoutes.kt, where this pattern is used first.
fun Route.userRoutes() {

    /** PATCH /users/me/settings: language (EN/AF/ZU), location sharing, biometric lock flag. */
    patch("/users/me/settings") {
        val uid = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        val request = call.receive<SettingsRequest>()

        val language = request.language?.uppercase()
        if (language != null && language !in SUPPORTED_LANGUAGES) {
            return@patch call.respond(HttpStatusCode.BadRequest, mapOf("error" to "Unsupported language: ${request.language}"))
        }

        dbQuery {
            Users.update({ Users.id eq uid }) { row ->
                language?.let { row[Users.language] = it }
                request.shareLocation?.let { row[Users.shareLocation] = it }
                request.biometricEnabled?.let { row[Users.biometricEnabled] = it }
            }
        }
        call.respond(dbQuery { Users.selectAll().where { Users.id eq uid }.single().toUserDto() })
    }

    /** POST /users/me/fcm-token: registers (or re-assigns) this device's push token to the caller. */
    post("/users/me/fcm-token") {
        val uid = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        val request = call.receive<FcmTokenRequest>()
        if (request.token.isBlank() || request.token.length > 512) {
            return@post call.respond(HttpStatusCode.BadRequest, mapOf("error" to "Invalid token"))
        }
        val now = System.currentTimeMillis()
        dbQuery {
            val exists = DeviceTokens.selectAll().where { DeviceTokens.token eq request.token }.singleOrNull() != null
            if (exists) {
                DeviceTokens.update({ DeviceTokens.token eq request.token }) {
                    it[DeviceTokens.userId] = uid
                    it[DeviceTokens.updatedAt] = now
                }
            } else {
                DeviceTokens.insert {
                    it[DeviceTokens.token] = request.token
                    it[DeviceTokens.userId] = uid
                    it[DeviceTokens.updatedAt] = now
                }
            }
        }
        call.respond(HttpStatusCode.NoContent)
    }

    /** DELETE /users/me/fcm-token?token=...: called on sign-out so a signed-out phone stops getting alerts. */
    delete("/users/me/fcm-token") {
        val uid = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        val token = call.request.queryParameters["token"]
            ?: return@delete call.respond(HttpStatusCode.BadRequest, mapOf("error" to "token query parameter is required"))
        dbQuery { DeviceTokens.deleteWhere { (DeviceTokens.token eq token) and (DeviceTokens.userId eq uid) } }
        call.respond(HttpStatusCode.NoContent)
    }

    /** POST /users/me/photo: raw JPEG bytes -> Azure Blob Storage -> URL saved on the user. */
    post("/users/me/photo") {
        val uid = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        if (!BlobStorage.isConfigured) {
            return@post call.respond(HttpStatusCode.ServiceUnavailable, mapOf("error" to "Blob storage is not configured on this server"))
        }
        val bytes = call.receive<ByteArray>()
        if (bytes.isEmpty() || bytes.size > MAX_PHOTO_BYTES) {
            return@post call.respond(HttpStatusCode.BadRequest, mapOf("error" to "Photo must be between 1 byte and 2 MB"))
        }
        val url = BlobStorage.uploadProfilePhoto(uid, bytes)
        dbQuery { Users.update({ Users.id eq uid }) { it[Users.photoUrl] = url } }
        call.respond(dbQuery { Users.selectAll().where { Users.id eq uid }.single().toUserDto() })
    }

    /**
     * DELETE /users/me: permanently deletes the caller's account and everything tied to it.
     * Required by Google Play for any app that lets people create an account (Part 3 - store readiness).
     */
    delete("/users/me") {
        val uid = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        val firebaseUid = dbQuery { Users.selectAll().where { Users.id eq uid }.singleOrNull()?.get(Users.firebaseUid) }
            ?: return@delete call.respond(HttpStatusCode.NoContent) // already gone

        dbQuery {
            val contactIds = Contacts.selectAll()
                .where { (Contacts.userIdA eq uid) or (Contacts.userIdB eq uid) }
                .map { it[Contacts.id] }
            // Children before parents, because of the foreign keys.
            Messages.deleteWhere { (Messages.senderId eq uid) or (Messages.contactId inList contactIds) }
            Contacts.deleteWhere { Contacts.id inList contactIds }
            LocationPings.deleteWhere { LocationPings.userId eq uid }
            Places.deleteWhere { Places.ownerId eq uid }
            SosEvents.deleteWhere { SosEvents.userId eq uid }
            DeviceTokens.deleteWhere { DeviceTokens.userId eq uid }
            Users.deleteWhere { Users.id eq uid }
        }
        AccountCleanup.purge(uid, firebaseUid)
        call.respond(HttpStatusCode.NoContent)
    }
}
