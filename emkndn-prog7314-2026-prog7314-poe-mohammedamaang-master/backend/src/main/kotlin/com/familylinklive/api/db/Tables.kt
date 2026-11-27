package com.familylinklive.api.db

import org.jetbrains.exposed.sql.Table

/** Mirrors the User entity from the Part 1 data model (Section 7), trimmed
 *  to the fields the Part 2 prototype actually reads/writes. */
object Users : Table("users") {
    val id = varchar("id", 64)
    val firebaseUid = varchar("firebase_uid", 128).uniqueIndex()
    val displayName = varchar("display_name", 128)
    val email = varchar("email", 128)
    val photoUrl = varchar("photo_url", 512).nullable()
    val language = varchar("language", 8).default("EN")
    val shareLocation = bool("share_location").default(true)
    val biometricEnabled = bool("biometric_enabled").default(false)
    override val primaryKey = PrimaryKey(id)
}

/**
 * Mirrors the Contact entity from the Part 1 data model (Section 7): a
 * mutual, consent-based relationship between two users.
 *
 * userIdA = the user who sent the invite; userIdB = the user who must
 * accept it. Status starts PENDING and moves to ACTIVE once userIdB calls
 * POST /contacts/{id}/accept. There is no REVOKED status - removing a
 * contact (DELETE /contacts/{id}) deletes the row outright, since nothing
 * in this app needs to remember a past relationship once it's ended.
 */
object Contacts : Table("contacts") {
    val id = varchar("id", 64)
    val userIdA = varchar("user_id_a", 64).references(Users.id)
    val userIdB = varchar("user_id_b", 64).references(Users.id)
    val status = varchar("status", 16).default("PENDING") // PENDING | ACTIVE
    val createdAt = long("created_at")
    override val primaryKey = PrimaryKey(id)
}

/** Mirrors LocationPing (Section 7). Every share writes a new row; the
 *  latest row per user is used to answer GET /map/contacts, which is now
 *  scoped to the caller's ACTIVE contacts only (see LocationRoutes.kt). */
object LocationPings : Table("location_pings") {
    val id = varchar("id", 64)
    val userId = varchar("user_id", 64).references(Users.id)
    val lat = double("lat")
    val lng = double("lng")
    val capturedAt = long("captured_at")
    override val primaryKey = PrimaryKey(id)
}

/** Mirrors Place (Section 7). */
object Places : Table("places") {
    val id = varchar("id", 64)
    val ownerId = varchar("owner_id", 64).references(Users.id)
    val name = varchar("name", 128)
    val lat = double("lat")
    val lng = double("lng")
    val radiusM = integer("radius_m")
    val autoMessage = varchar("auto_message", 256).nullable()
    override val primaryKey = PrimaryKey(id)
}

/**
 * Mirrors Message (Section 7): a per-contact private thread, identified by
 * contactId. Nullable only so SchemaUtils can add this column to a
 * database that already has rows from the earlier single-room prototype;
 * every message written from this point on always sets it.
 */
object Messages : Table("messages") {
    val id = varchar("id", 64)
    val contactId = varchar("contact_id", 64).references(Contacts.id).nullable()
    val senderId = varchar("sender_id", 64).references(Users.id)
    val body = varchar("body", 2000)
    val clientMessageId = varchar("client_message_id", 128)
    val createdAt = long("created_at")
    override val primaryKey = PrimaryKey(id)
}

/**
 * FCM registration tokens (Part 3). One user can have several devices, so
 * the token itself is the primary key; registering a token that already
 * exists simply re-assigns it to whoever is signed in on that device now.
 */
object DeviceTokens : Table("device_tokens") {
    val token = varchar("token", 512)
    val userId = varchar("user_id", 64).references(Users.id)
    val updatedAt = long("updated_at")
    override val primaryKey = PrimaryKey(token)
}

/** Emergency SOS events (Part 3, User-Defined Feature 5). */
object SosEvents : Table("sos_events") {
    val id = varchar("id", 64)
    val userId = varchar("user_id", 64).references(Users.id)
    val lat = double("lat")
    val lng = double("lng")
    val triggeredAt = long("triggered_at")
    override val primaryKey = PrimaryKey(id)
}
