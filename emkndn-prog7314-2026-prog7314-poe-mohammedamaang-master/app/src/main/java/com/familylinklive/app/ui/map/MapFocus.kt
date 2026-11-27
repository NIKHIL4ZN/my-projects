package com.familylinklive.app.ui.map

/**
 * Hand-off from a tapped notification / activity row to the Map screen:
 * "when you appear, centre on this point and drop a marker". The Map
 * consumes it exactly once (see MapFragment.onResume).
 */
object MapFocus {
    data class Request(val lat: Double, val lng: Double, val label: String?, val sos: Boolean)

    @Volatile private var pending: Request? = null

    fun request(lat: Double, lng: Double, label: String?, sos: Boolean) {
        pending = Request(lat, lng, label, sos)
    }

    fun consume(): Request? {
        val request = pending
        pending = null
        return request
    }
}
