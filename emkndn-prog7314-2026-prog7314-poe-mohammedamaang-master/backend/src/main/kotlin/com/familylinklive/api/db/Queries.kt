package com.familylinklive.api.db

import org.jetbrains.exposed.sql.and
import org.jetbrains.exposed.sql.or
import org.jetbrains.exposed.sql.selectAll

/** One accepted contact relationship, seen from a given user's point of view. */
data class ActiveContact(val contactRowId: String, val otherUserId: String)

/**
 * All ACTIVE contacts of [userId]. Must be called inside a dbQuery { } block.
 * Shared by the map, geofence alerts, SOS and chat notifications so the
 * "who is this user actually connected to" rule lives in exactly one place.
 */
fun activeContactsOf(userId: String): List<ActiveContact> =
    Contacts.selectAll()
        .where {
            ((Contacts.userIdA eq userId) or (Contacts.userIdB eq userId)) and (Contacts.status eq "ACTIVE")
        }
        .map { row ->
            ActiveContact(
                contactRowId = row[Contacts.id],
                otherUserId = if (row[Contacts.userIdA] == userId) row[Contacts.userIdB] else row[Contacts.userIdA]
            )
        }
