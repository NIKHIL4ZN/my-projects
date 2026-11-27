package com.familylinklive.app.data.models

// These request/response shapes intentionally mirror the endpoint spec in the
// Part 1 Planning and Design document (Section 5.2), reduced to the fields the
// Part 2 prototype actually needs end-to-end.

data class LoginRequest(val firebaseIdToken: String)
data class LoginResponse(val token: String, val user: UserDto)

data class UserDto(
    val id: String,
    val displayName: String,
    val email: String,
    val photoUrl: String? = null,
    val language: String = "EN",
    val shareLocation: Boolean = true,
    val biometricEnabled: Boolean = false
)

data class SettingsRequest(
    val language: String? = null,
    val shareLocation: Boolean? = null,
    val biometricEnabled: Boolean? = null
)

data class FcmTokenRequest(val token: String)
data class SosRequest(val lat: Double, val lng: Double, val clientId: String)
data class SosResponse(val sosId: String, val notifiedContacts: Int)

data class LocationUpdateRequest(val lat: Double, val lng: Double, val capturedAt: Long)
data class LocationUpdateResponse(val accepted: Int)

data class ContactLocationDto(
    val userId: String,
    val displayName: String,
    val lat: Double,
    val lng: Double,
    val capturedAt: Long
)
data class ContactLocationsResponse(val contacts: List<ContactLocationDto>)

data class PlaceRequest(
    val name: String,
    val lat: Double,
    val lng: Double,
    val radiusM: Int,
    val autoMessage: String? = null
)
data class PlaceDto(
    val id: String,
    val ownerId: String,
    val name: String,
    val lat: Double,
    val lng: Double,
    val radiusM: Int,
    val autoMessage: String? = null
)
data class PlacesResponse(val places: List<PlaceDto>)

data class SendMessageRequest(val contactId: String, val body: String, val clientMessageId: String)
data class MessageDto(
    val id: String,
    val senderId: String,
    val senderName: String,
    val body: String,
    val createdAt: Long,
    /** Idempotency key - lets the app recognise its own queued message once the server has it. */
    val clientMessageId: String? = null,
    /** Local only (never sent by the server): true while the message is still in the offline outbox. */
    val pending: Boolean = false
)
data class MessagesResponse(val messages: List<MessageDto>)

data class InviteRequest(val email: String)
data class ContactDto(
    val id: String,
    val otherUserId: String,
    val otherUserDisplayName: String,
    val otherUserPhotoUrl: String? = null,
    val status: String, // "PENDING" | "ACTIVE"
    val isIncomingRequest: Boolean
)
data class ContactsResponse(val contacts: List<ContactDto>)
