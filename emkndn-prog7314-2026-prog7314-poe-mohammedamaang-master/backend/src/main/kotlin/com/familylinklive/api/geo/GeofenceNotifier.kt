package com.familylinklive.api.geo

import com.familylinklive.api.db.DatabaseFactory.dbQuery
import com.familylinklive.api.db.Messages
import com.familylinklive.api.db.Places
import com.familylinklive.api.db.Users
import com.familylinklive.api.db.activeContactsOf
import com.familylinklive.api.notifications.Localization
import com.familylinklive.api.notifications.Notifier
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch
import org.jetbrains.exposed.sql.insert
import org.jetbrains.exposed.sql.selectAll
import org.slf4j.LoggerFactory
import java.util.UUID

/**
 * User-Defined Feature 4 - Geofence alerts.
 *
 * Called after every stored location ping. Works out whether the user just
 * crossed the boundary of any of their own Places and, if so:
 *   1. pushes "X arrived at Home" / "X left Home" to every ACTIVE contact
 *      (in each contact's own language) and records it in their Firestore feed;
 *   2. on arrival, posts the Place's optional auto-message into each
 *      contact chat thread (Part 1 REQ-23).
 * Nothing is sent when the user has switched location sharing off.
 */
object GeofenceNotifier {
    private val log = LoggerFactory.getLogger("GeofenceNotifier")
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    private data class Snapshot(val userName: String, val places: List<PlaceDef>, val contactIds: List<Pair<String, String>>)

    fun onLocation(userId: String, previous: GeoPoint?, current: GeoPoint) {
        scope.launch {
            try {
                val snap = dbQuery {
                    val user = Users.selectAll().where { Users.id eq userId }.single()
                    if (!user[Users.shareLocation]) {
                        null
                    } else {
                        Snapshot(
                            userName = user[Users.displayName],
                            places = Places.selectAll().where { Places.ownerId eq userId }.map {
                                PlaceDef(it[Places.id], it[Places.name], it[Places.lat], it[Places.lng], it[Places.radiusM], it[Places.autoMessage])
                            },
                            contactIds = activeContactsOf(userId).map { it.contactRowId to it.otherUserId }
                        )
                    }
                } ?: return@launch

                if (snap.places.isEmpty() || snap.contactIds.isEmpty()) return@launch

                for (t in Geofence.transitions(snap.places, previous, current)) {
                    val arrived = t.transition == Transition.ARRIVED
                    log.info("Geofence {} {} ({} contacts)", t.transition, t.place.name, snap.contactIds.size)

                    for ((contactRowId, otherUserId) in snap.contactIds) {
                        Notifier.send(
                            otherUserId,
                            Notifier.Event(
                                type = if (arrived) "GEOFENCE_ARRIVED" else "GEOFENCE_LEFT",
                                key = if (arrived) Localization.Key.ARRIVED else Localization.Key.LEFT,
                                args = listOf(snap.userName, t.place.name),
                                data = mapOf(
                                    "contactId" to contactRowId,
                                    "contactName" to snap.userName,
                                    "lat" to current.lat.toString(),
                                    "lng" to current.lng.toString(),
                                    "label" to t.place.name
                                ),
                                feed = mapOf(
                                    "actorName" to snap.userName,
                                    "placeName" to t.place.name,
                                    "lat" to current.lat,
                                    "lng" to current.lng
                                )
                            )
                        )
                    }

                    val autoMessage = t.place.autoMessage
                    if (arrived && !autoMessage.isNullOrBlank()) {
                        val now = System.currentTimeMillis()
                        dbQuery {
                            for ((contactRowId, _) in snap.contactIds) {
                                Messages.insert {
                                    it[id] = UUID.randomUUID().toString()
                                    it[contactId] = contactRowId
                                    it[senderId] = userId
                                    it[body] = autoMessage
                                    it[clientMessageId] = "auto-${t.place.id}-$now"
                                    it[createdAt] = now
                                }
                            }
                        }
                    }
                }
            } catch (t: Throwable) {
                log.warn("Geofence evaluation failed: {}", t.message)
            }
        }
    }
}
