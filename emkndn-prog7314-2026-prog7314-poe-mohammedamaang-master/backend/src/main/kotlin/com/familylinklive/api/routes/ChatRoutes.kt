package com.familylinklive.api.routes

import com.familylinklive.api.db.Contacts
import com.familylinklive.api.db.DatabaseFactory.dbQuery
import com.familylinklive.api.db.Messages
import com.familylinklive.api.db.Users
import com.familylinklive.api.models.MessageDto
import com.familylinklive.api.models.MessagesResponse
import com.familylinklive.api.models.SendMessageRequest
import com.familylinklive.api.notifications.Localization
import com.familylinklive.api.notifications.Notifier
import io.ktor.http.*
import io.ktor.server.application.*
import io.ktor.server.auth.*
import io.ktor.server.auth.jwt.*
import io.ktor.server.request.*
import io.ktor.server.response.*
import io.ktor.server.routing.*
import org.jetbrains.exposed.sql.SortOrder
import org.jetbrains.exposed.sql.and
import org.jetbrains.exposed.sql.insert
import org.jetbrains.exposed.sql.or
import org.jetbrains.exposed.sql.selectAll
import java.util.UUID

/**
 * POST /chat/send + GET /chat/messages: a private thread per Contact
 * relationship. Both endpoints check the caller is actually part of the
 * given contact relationship before reading/writing anything.
 *
 * Part 3: POST /chat/send is IDEMPOTENT on (sender, clientMessageId). The
 * app's offline outbox may retry a message whose first attempt actually
 * reached the server but whose reply was lost; the retry returns the
 * already-stored message instead of creating a duplicate.
 */
// Exposed DSL query-building pattern - see the reference comment in
// AuthRoutes.kt, where this pattern is used first.
fun Route.chatRoutes() {

    post("/chat/send") {
        val userId = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        val request = call.receive<SendMessageRequest>()

        val membership = dbQuery {
            Contacts.selectAll().where {
                (Contacts.id eq request.contactId) and
                    (Contacts.status eq "ACTIVE") and
                    ((Contacts.userIdA eq userId) or (Contacts.userIdB eq userId))
            }.singleOrNull()
        }
        if (membership == null) {
            return@post call.respond(
                HttpStatusCode.Forbidden,
                mapOf("error" to "Not an active contact - accept the invite before chatting")
            )
        }

        val senderName = dbQuery { Users.selectAll().where { Users.id eq userId }.single()[Users.displayName] }

        // Idempotency: same sender + same client id = same message.
        val duplicate = dbQuery {
            Messages.selectAll().where {
                (Messages.senderId eq userId) and (Messages.clientMessageId eq request.clientMessageId)
            }.singleOrNull()
        }
        if (duplicate != null) {
            return@post call.respond(
                MessageDto(
                    duplicate[Messages.id], userId, senderName, duplicate[Messages.body],
                    duplicate[Messages.createdAt], duplicate[Messages.clientMessageId]
                )
            )
        }

        val newId = UUID.randomUUID().toString()
        val now = System.currentTimeMillis()
        dbQuery {
            Messages.insert {
                it[id] = newId
                it[contactId] = request.contactId
                it[senderId] = userId
                it[body] = request.body
                it[clientMessageId] = request.clientMessageId
                it[createdAt] = now
            }
        }

        // Real-time push to the other person in the thread.
        val recipientId = if (membership[Contacts.userIdA] == userId) membership[Contacts.userIdB] else membership[Contacts.userIdA]
        Notifier.send(
            recipientId,
            Notifier.Event(
                type = "MESSAGE",
                key = Localization.Key.NEW_MESSAGE,
                args = listOf(senderName, request.body.take(120)),
                data = mapOf("contactId" to request.contactId, "contactName" to senderName)
            )
        )

        call.respond(MessageDto(newId, userId, senderName, request.body, now, request.clientMessageId))
    }

    get("/chat/messages") {
        val userId = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        val contactId = call.request.queryParameters["contactId"]
            ?: return@get call.respond(HttpStatusCode.BadRequest, mapOf("error" to "contactId query parameter is required"))

        val isMember = dbQuery {
            Contacts.selectAll().where {
                (Contacts.id eq contactId) and ((Contacts.userIdA eq userId) or (Contacts.userIdB eq userId))
            }.singleOrNull() != null
        }
        if (!isMember) {
            return@get call.respond(HttpStatusCode.Forbidden, mapOf("error" to "Not a participant in this conversation"))
        }

        val messages = dbQuery {
            (Messages innerJoin Users)
                .selectAll()
                .where { Messages.contactId eq contactId }
                .orderBy(Messages.createdAt, SortOrder.ASC)
                .map { row ->
                    MessageDto(
                        id = row[Messages.id],
                        senderId = row[Messages.senderId],
                        senderName = row[Users.displayName],
                        body = row[Messages.body],
                        createdAt = row[Messages.createdAt],
                        clientMessageId = row[Messages.clientMessageId]
                    )
                }
        }
        call.respond(MessagesResponse(messages))
    }
}
