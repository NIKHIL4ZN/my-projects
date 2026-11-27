package com.familylinklive.app.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey

/**
 * One queued action waiting to reach the server (Part 3 - Offline mode).
 * Everything the user does without a connection - sending a chat message,
 * sharing their location, raising an SOS - is written here FIRST and only
 * deleted once the server has acknowledged it, so nothing is lost if the
 * network drops or the app is killed mid-request.
 */
@Entity(tableName = "outbox")
data class OutboxItem(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    val type: String,
    val contactId: String? = null,
    val body: String? = null,
    val lat: Double? = null,
    val lng: Double? = null,
    /** Idempotency key sent to the server so a retried request can never be applied twice. */
    val clientId: String,
    val createdAt: Long
) {
    companion object {
        const val TYPE_MESSAGE = "MESSAGE"
        const val TYPE_LOCATION = "LOCATION"
        const val TYPE_SOS = "SOS"
    }
}

/** Read cache: contacts as last seen online, so the list still works offline. */
@Entity(tableName = "cached_contacts")
data class CachedContact(
    @PrimaryKey val id: String,
    val otherUserId: String,
    val otherUserDisplayName: String,
    val otherUserPhotoUrl: String?,
    val status: String,
    val isIncomingRequest: Boolean
)

/** Read cache: chat history per contact thread. */
@Entity(tableName = "cached_messages")
data class CachedMessage(
    @PrimaryKey val id: String,
    val contactId: String,
    val senderId: String,
    val senderName: String,
    val body: String,
    val createdAt: Long,
    val clientMessageId: String?
)

/** Read cache: the user's saved Places. */
@Entity(tableName = "cached_places")
data class CachedPlace(
    @PrimaryKey val id: String,
    val ownerId: String,
    val name: String,
    val lat: Double,
    val lng: Double,
    val radiusM: Int,
    val autoMessage: String?
)
