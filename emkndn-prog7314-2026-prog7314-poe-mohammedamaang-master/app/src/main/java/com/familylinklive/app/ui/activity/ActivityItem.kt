package com.familylinklive.app.ui.activity

/** One alert from the Firestore activity feed (written by the backend, read by this app). */
data class ActivityItem(
    val id: String,
    /** GEOFENCE_ARRIVED | GEOFENCE_LEFT | SOS | INVITE_RECEIVED | INVITE_ACCEPTED */
    val type: String,
    val actorName: String,
    val placeName: String?,
    val lat: Double?,
    val lng: Double?,
    val createdAt: Long
)
