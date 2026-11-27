package com.familylinklive.api.models

import kotlinx.serialization.Serializable

@Serializable data class LoginRequest(val firebaseIdToken: String)
@Serializable data class LoginResponse(val token: String, val user: UserDto)

@Serializable data class UserDto(
    val id: String,
    val displayName: String,
    val email: String,
    val photoUrl: String? = null,
    val language: String = "EN",
    val shareLocation: Boolean = true,
    val biometricEnabled: Boolean = false
)

@Serializable data class SettingsRequest(
    val language: String? = null,
    val shareLocation: Boolean? = null,
    val biometricEnabled: Boolean? = null
)

@Serializable data class SosRequest(val lat: Double, val lng: Double, val clientId: String? = null)
@Serializable data class SosResponse(val sosId: String, val notifiedContacts: Int)
@Serializable data class FcmTokenRequest(val token: String)

@Serializable data class LocationUpdateRequest(val lat: Double, val lng: Double, val capturedAt: Long)
@Serializable data class LocationUpdateResponse(val accepted: Int)

@Serializable data class ContactLocationDto(
    val userId: String,
    val displayName: String,
    val lat: Double,
    val lng: Double,
    val capturedAt: Long
)
@Serializable data class ContactLocationsResponse(val contacts: List<ContactLocationDto>)

@Serializable data class PlaceRequest(
    val name: String,
    val lat: Double,
    val lng: Double,
    val radiusM: Int,
    val autoMessage: String? = null
)
@Serializable data class PlaceDto(
    val id: String,
    val ownerId: String,
    val name: String,
    val lat: Double,
    val lng: Double,
    val radiusM: Int,
    val autoMessage: String? = null
)
@Serializable data class PlacesResponse(val places: List<PlaceDto>)

@Serializable data class SendMessageRequest(val contactId: String, val body: String, val clientMessageId: String)
@Serializable data class MessageDto(
    val id: String,
    val senderId: String,
    val senderName: String,
    val body: String,
    val createdAt: Long,
    val clientMessageId: String? = null
)
@Serializable data class MessagesResponse(val messages: List<MessageDto>)

@Serializable data class InviteRequest(val email: String)
@Serializable data class ContactDto(
    val id: String,
    val otherUserId: String,
    val otherUserDisplayName: String,
    val otherUserPhotoUrl: String? = null,
    val status: String, // "PENDING" | "ACTIVE"
    // True when the caller is the one who needs to accept this invite -
    // i.e. it shows up as "X wants to connect" with Accept/Decline actions,
    // rather than "Waiting for X to accept".
    val isIncomingRequest: Boolean
)
@Serializable data class ContactsResponse(val contacts: List<ContactDto>)
