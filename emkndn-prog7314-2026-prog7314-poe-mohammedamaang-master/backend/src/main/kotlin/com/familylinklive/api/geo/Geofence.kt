package com.familylinklive.api.geo

import kotlin.math.asin
import kotlin.math.cos
import kotlin.math.pow
import kotlin.math.sin
import kotlin.math.sqrt

data class GeoPoint(val lat: Double, val lng: Double)

/** A saved Place (geofence) as the evaluator needs to see it. */
data class PlaceDef(
    val id: String,
    val name: String,
    val lat: Double,
    val lng: Double,
    val radiusM: Int,
    val autoMessage: String?
)

enum class Transition { ARRIVED, LEFT }

data class PlaceTransition(
    val place: PlaceDef,
    val transition: Transition
)

/**
 * Server-authoritative geofence maths (User-Defined Feature 4).
 *
 * The server decides arrivals/departures instead of the phone so that the
 * result is identical for every contact and can't be missed because a
 * client was asleep - the design choice made in Part 1 (Figure 7).
 */
object Geofence {

    private const val EARTH_RADIUS_M = 6_371_000.0

    /** Great-circle distance in metres between two coordinates (haversine formula). */
    fun distanceMetres(a: GeoPoint, b: GeoPoint): Double {
        val dLat = Math.toRadians(b.lat - a.lat)
        val dLng = Math.toRadians(b.lng - a.lng)
        val h = sin(dLat / 2).pow(2) +
            cos(Math.toRadians(a.lat)) * cos(Math.toRadians(b.lat)) * sin(dLng / 2).pow(2)
        return 2 * EARTH_RADIUS_M * asin(sqrt(h))
    }

    fun isInside(place: PlaceDef, point: GeoPoint): Boolean =
        distanceMetres(GeoPoint(place.lat, place.lng), point) <= place.radiusM

    /**
     * Compares the user's previous position with the new one against every
     * Place and reports each boundary crossing. With no previous position
     * the user is treated as having been outside, so the very first ping
     * inside a Place counts as an arrival.
     */
    fun transitions(places: List<PlaceDef>, previous: GeoPoint?, current: GeoPoint): List<PlaceTransition> =
        places.mapNotNull { place ->
            val wasInside = previous?.let { isInside(place, it) } ?: false
            val nowInside = isInside(place, current)
            when {
                !wasInside && nowInside -> PlaceTransition(place, Transition.ARRIVED)
                wasInside && !nowInside -> PlaceTransition(place, Transition.LEFT)
                else -> null
            }
        }
}
