package com.familylinklive.api.routes

import com.familylinklive.api.db.Contacts
import com.familylinklive.api.db.DatabaseFactory.dbQuery
import com.familylinklive.api.db.Users
import com.familylinklive.api.models.ContactDto
import com.familylinklive.api.models.ContactsResponse
import com.familylinklive.api.models.InviteRequest
import com.familylinklive.api.notifications.Localization
import com.familylinklive.api.notifications.Notifier
import io.ktor.http.*
import io.ktor.server.application.*
import io.ktor.server.auth.*
import io.ktor.server.auth.jwt.*
import io.ktor.server.request.*
import io.ktor.server.response.*
import io.ktor.server.routing.*
import org.jetbrains.exposed.sql.SqlExpressionBuilder.eq
import org.jetbrains.exposed.sql.and
import org.jetbrains.exposed.sql.deleteWhere
import org.jetbrains.exposed.sql.insert
import org.jetbrains.exposed.sql.or
import org.jetbrains.exposed.sql.selectAll
import org.jetbrains.exposed.sql.update
import java.util.UUID

/**
 * Contact management (REQ-9 / Part 1 Section 5.2 endpoints 3-5): email-based
 * invite, list (including pending requests in both directions), accept, and
 * remove. Replaces the single shared "Family Circle" from the earlier
 * prototype - the Map and Chat features now only work between two users
 * once a Contacts row between them exists with status ACTIVE.
 */
// Exposed DSL query-building pattern (selectAll().where{}, insert{},
// update{}, deleteWhere{}) - see the reference comment in
// AuthRoutes.kt, where this pattern is used first.
fun Route.contactsRoutes() {

    post("/contacts/invite") {
        val userId = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        val request = call.receive<InviteRequest>()
        val targetEmail = request.email.trim().lowercase()

        val targetUser = dbQuery {
            Users.selectAll().where { Users.email eq targetEmail }.singleOrNull()
        } ?: return@post call.respond(
            HttpStatusCode.NotFound,
            mapOf("error" to "No FamilyLink Live user found with that email. Ask them to sign in to the app at least once, then try inviting them again.")
        )

        val targetId = targetUser[Users.id]
        if (targetId == userId) {
            return@post call.respond(HttpStatusCode.BadRequest, mapOf("error" to "You can't add yourself as a contact"))
        }

        val existing = dbQuery {
            Contacts.selectAll().where {
                ((Contacts.userIdA eq userId) and (Contacts.userIdB eq targetId)) or
                    ((Contacts.userIdA eq targetId) and (Contacts.userIdB eq userId))
            }.singleOrNull()
        }
        if (existing != null) {
            return@post call.respond(HttpStatusCode.Conflict, mapOf("error" to "A contact request with this user already exists"))
        }

        val newId = UUID.randomUUID().toString()
        val now = System.currentTimeMillis()
        dbQuery {
            Contacts.insert {
                it[id] = newId
                it[userIdA] = userId
                it[userIdB] = targetId
                it[status] = "PENDING"
                it[createdAt] = now
            }
        }

        val inviterName = dbQuery { Users.selectAll().where { Users.id eq userId }.single()[Users.displayName] }
        Notifier.send(
            targetId,
            Notifier.Event(
                type = "INVITE_RECEIVED",
                key = Localization.Key.INVITE_RECEIVED,
                args = listOf(inviterName),
                data = mapOf("contactId" to newId),
                feed = mapOf("actorName" to inviterName)
            )
        )

        call.respond(
            HttpStatusCode.Created,
            ContactDto(
                id = newId,
                otherUserId = targetId,
                otherUserDisplayName = targetUser[Users.displayName],
                otherUserPhotoUrl = targetUser[Users.photoUrl],
                status = "PENDING",
                isIncomingRequest = false
            )
        )
    }

    get("/contacts") {
        val userId = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()

        val contacts = dbQuery {
            Contacts.selectAll()
                .where { (Contacts.userIdA eq userId) or (Contacts.userIdB eq userId) }
                .map { row ->
                    val otherId = if (row[Contacts.userIdA] == userId) row[Contacts.userIdB] else row[Contacts.userIdA]
                    val otherUser = Users.selectAll().where { Users.id eq otherId }.single()
                    val incoming = row[Contacts.status] == "PENDING" && row[Contacts.userIdB] == userId
                    ContactDto(
                        id = row[Contacts.id],
                        otherUserId = otherId,
                        otherUserDisplayName = otherUser[Users.displayName],
                        otherUserPhotoUrl = otherUser[Users.photoUrl],
                        status = row[Contacts.status],
                        isIncomingRequest = incoming
                    )
                }
        }
        call.respond(ContactsResponse(contacts))
    }

    post("/contacts/{id}/accept") {
        val userId = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        val contactId = call.parameters["id"] ?: return@post call.respond(HttpStatusCode.BadRequest)

        // Only the invitee (userIdB) can accept, and only while still PENDING.
        val pending = dbQuery {
            Contacts.selectAll().where {
                (Contacts.id eq contactId) and (Contacts.userIdB eq userId) and (Contacts.status eq "PENDING")
            }.singleOrNull()
        }
        if (pending == null) {
            return@post call.respond(HttpStatusCode.NotFound, mapOf("error" to "No pending invite found for you to accept"))
        }

        dbQuery {
            Contacts.update({ Contacts.id eq contactId }) { it[status] = "ACTIVE" }
        }

        // Tell the person who sent the invite.
        val accepterName = dbQuery { Users.selectAll().where { Users.id eq userId }.single()[Users.displayName] }
        Notifier.send(
            pending[Contacts.userIdA],
            Notifier.Event(
                type = "INVITE_ACCEPTED",
                key = Localization.Key.INVITE_ACCEPTED,
                args = listOf(accepterName),
                data = mapOf("contactId" to contactId, "contactName" to accepterName),
                feed = mapOf("actorName" to accepterName)
            )
        )
        call.respond(HttpStatusCode.OK, mapOf("status" to "ACTIVE"))
    }

    delete("/contacts/{id}") {
        val userId = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        val contactId = call.parameters["id"] ?: return@delete call.respond(HttpStatusCode.BadRequest)

        // Works for: declining an incoming request, cancelling an outgoing
        // request, or removing an active contact - all the same operation,
        // as long as the caller is one of the two people in it.
        dbQuery {
            Contacts.deleteWhere {
                (Contacts.id eq contactId) and ((Contacts.userIdA eq userId) or (Contacts.userIdB eq userId))
            }
        }
        call.respond(HttpStatusCode.NoContent)
    }
}
