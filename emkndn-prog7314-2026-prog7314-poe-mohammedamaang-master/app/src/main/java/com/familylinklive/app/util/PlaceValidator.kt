package com.familylinklive.app.util

/**
 * Shared validation rules for Place (geofence) input, used by the Add/Edit
 * Place dialog and covered by unit tests in app/src/test.
 */
object PlaceValidator {
    private const val MIN_RADIUS_M = 100
    private const val MAX_RADIUS_M = 2000

    fun isValidRadius(radiusM: Int): Boolean = radiusM in MIN_RADIUS_M..MAX_RADIUS_M

    fun isValidName(name: String): Boolean = name.isNotBlank() && name.length <= 128

    fun coerceRadius(radiusM: Int): Int = radiusM.coerceIn(MIN_RADIUS_M, MAX_RADIUS_M)
}
