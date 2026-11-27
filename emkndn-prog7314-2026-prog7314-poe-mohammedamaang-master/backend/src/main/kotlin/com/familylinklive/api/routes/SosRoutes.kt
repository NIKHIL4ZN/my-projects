package com.familylinklive.api.routes

import com.familylinklive.api.db.DatabaseFactory.dbQuery
import com.familylinklive.api.db.LocationPings
import com.familylinklive.api.db.SosEvents
import com.familylinklive.api.db.Users
import com.familylinklive.api.db.activeContactsOf
import com.familylinklive.api.models.SosRequest
import com.familylinklive.api.models.SosResponse
import com.familylinklive.api.notifications.Localization
import com.familylinklive.api.notifications.Notifier
import io.ktor.http.*
import io.ktor.server.application.*
import io.ktor.server.auth.*
import io.ktor.server.auth.jwt.*
import io.ktor.server.request.*
import io.ktor.server.response.*
import io.ktor.server.routing.*
import org.jetbrains.exposed.sql.insert
import org.jetbrains.exposed.sql.selectAll
import java.util.UUID

/**
 * User-Defined Feature 5 - Emergency SOS.
 *
 * POST /sos/trigger stores the event and immediately alerts every ACTIVE
 * contact with a high-priority push and a Firestore activity entry carrying
 * the sender's exact coordinates. It deliberately ignores the sender's
 * "share my location" switch: in an emergency the alert always goes out.
 *
 * The optional clientId makes it idempotent, because the app may retry an
 * SOS from its offline outbox after the first attempt's reply was lost.
 */
// Exposed DSL query-building pattern - see the reference comment in
// AuthRoutes.kt, where this pattern is used first.
fun Route.sosRoutes() {
    post("/sos/trigger") {
        val userId = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        val request = call.receive<SosRequest>()
        val sosId = request.clientId?.takeIf { it.isNotBlank() && it.length <= 64 } ?: UUID.randomUUID().toString()

        val alreadyProcessed = dbQuery { SosEvents.selectAll().where { SosEvents.id eq sosId }.singleOrNull() != null }
        val contacts = dbQuery { activeContactsOf(userId) }
        if (alreadyProcessed) {
            return@post call.respond(SosResponse(sosId, contacts.size))
        }

        val now = System.currentTimeMillis()
        val senderName = dbQuery { Users.selectAll().where { Users.id eq userId }.single()[Users.displayName] }
        dbQuery {
            SosEvents.insert {
                it[id] = sosId
                it[SosEvents.userId] = userId
                it[lat] = request.lat
                it[lng] = request.lng
                it[triggeredAt] = now
            }
            // Also make this the sender's latest known position.
            LocationPings.insert {
                it[id] = UUID.randomUUID().toString()
                it[LocationPings.userId] = userId
                it[lat] = request.lat
                it[lng] = request.lng
                it[capturedAt] = now
            }
        }

        for (contact in contacts) {
            Notifier.send(
                contact.otherUserId,
                Notifier.Event(
                    type = "SOS",
                    key = Localization.Key.SOS,
                    args = listOf(senderName),
                    data = mapOf(
                        "contactId" to contact.contactRowId,
                        "contactName" to senderName,
                        "lat" to request.lat.toString(),
                        "lng" to request.lng.toString(),
                        "label" to senderName
                    ),
                    feed = mapOf("actorName" to senderName, "lat" to request.lat, "lng" to request.lng)
                )
            )
        }
        call.respond(SosResponse(sosId, contacts.size))
    }
}
