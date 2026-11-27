package com.familylinklive.app.util

/**
 * "Smart Ping" from the Part 1 design: ask for location updates often while
 * the person is moving fast and rarely while they are standing still, to
 * save battery. Speed (m/s) comes straight from the GPS fix.
 */
object SmartPing {
    const val DRIVING_MS = 15_000L
    const val WALKING_MS = 60_000L
    const val STATIONARY_MS = 300_000L

    fun intervalFor(speedMps: Float): Long = when {
        speedMps >= 5f -> DRIVING_MS
        speedMps >= 0.8f -> WALKING_MS
        else -> STATIONARY_MS
    }
}
