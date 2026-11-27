package com.familylinklive.app.data.local

import com.familylinklive.app.data.models.ContactDto
import com.familylinklive.app.data.models.MessageDto
import com.familylinklive.app.data.models.PlaceDto

fun ContactDto.toEntity() = CachedContact(id, otherUserId, otherUserDisplayName, otherUserPhotoUrl, status, isIncomingRequest)
fun CachedContact.toDto() = ContactDto(id, otherUserId, otherUserDisplayName, otherUserPhotoUrl, status, isIncomingRequest)

fun MessageDto.toEntity(contactId: String) =
    CachedMessage(id, contactId, senderId, senderName, body, createdAt, clientMessageId)
fun CachedMessage.toDto() = MessageDto(id, senderId, senderName, body, createdAt, clientMessageId, pending = false)

fun PlaceDto.toEntity() = CachedPlace(id, ownerId, name, lat, lng, radiusM, autoMessage)
fun CachedPlace.toDto() = PlaceDto(id, ownerId, name, lat, lng, radiusM, autoMessage)
