package com.familylinklive.api.routes

import com.familylinklive.api.db.Contacts
import com.familylinklive.api.geo.GeoPoint
import com.familylinklive.api.geo.GeofenceNotifier
import com.familylinklive.api.db.DatabaseFactory.dbQuery
import com.familylinklive.api.db.LocationPings
import com.familylinklive.api.db.Users
import com.familylinklive.api.models.ContactLocationDto
import com.familylinklive.api.models.ContactLocationsResponse
import com.familylinklive.api.models.LocationUpdateRequest
import com.familylinklive.api.models.LocationUpdateResponse
import io.ktor.server.application.*
import io.ktor.server.auth.*
import io.ktor.server.auth.jwt.*
import io.ktor.server.request.*
import io.ktor.server.response.*
import io.ktor.server.routing.*
import org.jetbrains.exposed.sql.SortOrder
import org.jetbrains.exposed.sql.and
//import org.jetbrains.exposed.sql.inList
import org.jetbrains.exposed.sql.insert
import org.jetbrains.exposed.sql.innerJoin
import org.jetbrains.exposed.sql.or
import org.jetbrains.exposed.sql.selectAll
import java.util.UUID

/**
 * POST /location/update (REQ-21): appends a new location ping for the caller.
 *
 * GET /map/contacts (REQ-21): returns the latest ping for each of the
 * caller's ACTIVE contacts (see ContactsRoutes.kt) who currently have
 * location sharing switched on - i.e. real mutual-consent visibility, not
 * "everyone who has ever signed in" as in the earlier prototype.
 */
// Exposed DSL query-building pattern - see the reference comment in
// AuthRoutes.kt, where this pattern is used first.
fun Route.locationRoutes() {
    post("/location/update") {
        val userId = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        val request = call.receive<LocationUpdateRequest>()

        // The previous position must be read BEFORE this ping is stored,
        // otherwise "where were they a moment ago" would be this very ping.
        val previous = dbQuery {
            LocationPings.selectAll()
                .where { LocationPings.userId eq userId }
                .orderBy(LocationPings.capturedAt, SortOrder.DESC)
                .limit(1)
                .map { GeoPoint(it[LocationPings.lat], it[LocationPings.lng]) }
                .firstOrNull()
        }

        dbQuery {
            LocationPings.insert {
                it[id] = UUID.randomUUID().toString()
                it[LocationPings.userId] = userId
                it[lat] = request.lat
                it[lng] = request.lng
                it[capturedAt] = request.capturedAt
            }
        }

        // User-Defined Feature 4: geofence arrival/departure alerts.
        GeofenceNotifier.onLocation(userId, previous, GeoPoint(request.lat, request.lng))

        call.respond(LocationUpdateResponse(accepted = 1))
    }

    get("/map/contacts") {
        val userId = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()

        val contacts = dbQuery {
            val activeContactIds = Contacts.selectAll()
                .where {
                    ((Contacts.userIdA eq userId) or (Contacts.userIdB eq userId)) and (Contacts.status eq "ACTIVE")
                }
                .map { row -> if (row[Contacts.userIdA] == userId) row[Contacts.userIdB] else row[Contacts.userIdA] }
                .toSet()

            if (activeContactIds.isEmpty()) {
                emptyList()
            } else {
                val latestPerUser = linkedMapOf<String, ContactLocationDto>()
                (LocationPings innerJoin Users)
                    .selectAll()
                    .where { (Users.id inList activeContactIds) and (Users.shareLocation eq true) }
                    .orderBy(LocationPings.capturedAt, SortOrder.ASC)
                    .forEach { row ->
                        // Ascending order + map overwrite -> last write per user wins == latest ping.
                        latestPerUser[row[Users.id]] = ContactLocationDto(
                            userId = row[Users.id],
                            displayName = row[Users.displayName],
                            lat = row[LocationPings.lat],
                            lng = row[LocationPings.lng],
                            capturedAt = row[LocationPings.capturedAt]
                        )
                    }
                latestPerUser.values.toList()
            }
        }
        call.respond(ContactLocationsResponse(contacts))
    }
}
