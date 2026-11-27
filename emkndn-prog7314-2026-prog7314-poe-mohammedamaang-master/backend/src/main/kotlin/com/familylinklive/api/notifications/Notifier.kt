package com.familylinklive.api.notifications

import com.familylinklive.api.db.DatabaseFactory.dbQuery
import com.familylinklive.api.db.DeviceTokens
import com.familylinklive.api.db.Users
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch
import org.jetbrains.exposed.sql.selectAll
import org.slf4j.LoggerFactory

/**
 * One call = one alert for one recipient: a localised FCM push AND (for
 * alerts worth remembering) a Firestore activity document. Runs in the
 * background so HTTP responses never wait on Google's servers.
 */
object Notifier {
    private val log = LoggerFactory.getLogger("Notifier")
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    data class Event(
        /** MESSAGE | INVITE_RECEIVED | INVITE_ACCEPTED | GEOFENCE_ARRIVED | GEOFENCE_LEFT | SOS */
        val type: String,
        val key: Localization.Key,
        val args: List<String>,
        /** Extra values the app needs to react to a tap (contactId, lat, lng ...). */
        val data: Map<String, String> = emptyMap(),
        /** If non-null the event is also stored in the recipient's Firestore activity feed. */
        val feed: Map<String, Any>? = null
    )

    fun send(recipientUserId: String, event: Event) {
        scope.launch {
            try {
                deliver(recipientUserId, event)
            } catch (t: Throwable) {
                log.warn("Notification {} to {} failed: {}", event.type, recipientUserId, t.message)
            }
        }
    }

    private suspend fun deliver(recipientUserId: String, event: Event) {
        val recipient = dbQuery { Users.selectAll().where { Users.id eq recipientUserId }.singleOrNull() } ?: return
        val (title, body) = Localization.text(recipient[Users.language], event.key, event.args)

        event.feed?.let { fields ->
            runCatching { ActivityFeed.record(recipient[Users.firebaseUid], event.type, fields) }
                .onFailure { log.warn("Firestore activity write failed: {}", it.message) }
        }

        val tokens = dbQuery {
            DeviceTokens.selectAll().where { DeviceTokens.userId eq recipientUserId }.map { it[DeviceTokens.token] }
        }
        if (tokens.isNotEmpty()) {
            FcmSender.send(tokens, title, body, mapOf("type" to event.type) + event.data)
        }
    }
}
