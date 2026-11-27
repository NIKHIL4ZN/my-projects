package com.familylinklive.api.routes

import com.familylinklive.api.db.DatabaseFactory.dbQuery
import com.familylinklive.api.db.Places
import com.familylinklive.api.models.PlaceDto
import com.familylinklive.api.models.PlaceRequest
import com.familylinklive.api.models.PlacesResponse
import io.ktor.http.*
import io.ktor.server.application.*
import io.ktor.server.auth.*
import io.ktor.server.auth.jwt.*
import io.ktor.server.request.*
import io.ktor.server.response.*
import io.ktor.server.routing.*
import org.jetbrains.exposed.sql.ResultRow
import org.jetbrains.exposed.sql.SqlExpressionBuilder.eq
import org.jetbrains.exposed.sql.and
import org.jetbrains.exposed.sql.deleteWhere
import org.jetbrains.exposed.sql.insert
import org.jetbrains.exposed.sql.selectAll
import org.jetbrains.exposed.sql.update
import java.util.UUID

/** CRUD for Places / geofences (REQ-22, REQ-23), scoped to the caller. */
// Exposed DSL query-building pattern - see the reference comment in
// AuthRoutes.kt, where this pattern is used first.
fun Route.placesRoutes() {

    post("/places") {
        val userId = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        val request = call.receive<PlaceRequest>()
        val newId = UUID.randomUUID().toString()
        val radius = request.radiusM.coerceIn(100, 2000)

        dbQuery {
            Places.insert {
                it[id] = newId
                it[ownerId] = userId
                it[name] = request.name
                it[lat] = request.lat
                it[lng] = request.lng
                it[radiusM] = radius
                it[autoMessage] = request.autoMessage
            }
        }
        call.respond(
            HttpStatusCode.Created,
            PlaceDto(newId, userId, request.name, request.lat, request.lng, radius, request.autoMessage)
        )
    }

    get("/places") {
        val userId = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        val places = dbQuery {
            Places.selectAll().where { Places.ownerId eq userId }.map { it.toPlaceDto() }
        }
        call.respond(PlacesResponse(places))
    }

    patch("/places/{id}") {
        val userId = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        val placeId = call.parameters["id"] ?: return@patch call.respond(HttpStatusCode.BadRequest)
        val request = call.receive<PlaceRequest>()
        val radius = request.radiusM.coerceIn(100, 2000)

        val updatedRows = dbQuery {
            Places.update({ (Places.id eq placeId) and (Places.ownerId eq userId) }) {
                it[name] = request.name
                it[lat] = request.lat
                it[lng] = request.lng
                it[radiusM] = radius
                it[autoMessage] = request.autoMessage
            }
        }
        if (updatedRows == 0) return@patch call.respond(HttpStatusCode.NotFound)
        call.respond(PlaceDto(placeId, userId, request.name, request.lat, request.lng, radius, request.autoMessage))
    }

    delete("/places/{id}") {
        val userId = call.principal<JWTPrincipal>()!!.payload.getClaim("userId").asString()
        val placeId = call.parameters["id"] ?: return@delete call.respond(HttpStatusCode.BadRequest)
        dbQuery { Places.deleteWhere { (Places.id eq placeId) and (Places.ownerId eq userId) } }
        call.respond(HttpStatusCode.NoContent)
    }
}

private fun ResultRow.toPlaceDto() = PlaceDto(
    id = this[Places.id],
    ownerId = this[Places.ownerId],
    name = this[Places.name],
    lat = this[Places.lat],
    lng = this[Places.lng],
    radiusM = this[Places.radiusM],
    autoMessage = this[Places.autoMessage]
)
