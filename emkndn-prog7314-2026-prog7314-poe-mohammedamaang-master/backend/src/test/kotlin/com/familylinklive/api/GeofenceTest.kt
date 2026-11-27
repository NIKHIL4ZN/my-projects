package com.familylinklive.api

import com.familylinklive.api.geo.Geofence
import com.familylinklive.api.geo.GeoPoint
import com.familylinklive.api.geo.PlaceDef
import com.familylinklive.api.geo.Transition
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertTrue

/** Unit tests for the server-side geofence maths behind User-Defined Feature 4. */
class GeofenceTest {

    // Home in Johannesburg, 200 m radius.
    private val home = PlaceDef("p1", "Home", -26.2041, 28.0473, 200, "Home safe")
    private val atHome = GeoPoint(-26.2041, 28.0473)
    private val farAway = GeoPoint(-26.2300, 28.0900) // several km away

    @Test
    fun `distance between identical points is zero`() {
        assertEquals(0.0, Geofence.distanceMetres(atHome, atHome), 0.001)
    }

    @Test
    fun `one thousandth of a degree of latitude is about 111 metres`() {
        val d = Geofence.distanceMetres(GeoPoint(0.0, 0.0), GeoPoint(0.001, 0.0))
        assertTrue(d in 110.0..112.0, "expected ~111 m but was $d")
    }

    @Test
    fun `point inside the radius is inside and point outside is not`() {
        assertTrue(Geofence.isInside(home, atHome))
        assertTrue(!Geofence.isInside(home, farAway))
    }

    @Test
    fun `moving from outside to inside is an arrival`() {
        val result = Geofence.transitions(listOf(home), farAway, atHome)
        assertEquals(1, result.size)
        assertEquals(Transition.ARRIVED, result.first().transition)
        assertEquals("Home", result.first().place.name)
    }

    @Test
    fun `moving from inside to outside is a departure`() {
        val result = Geofence.transitions(listOf(home), atHome, farAway)
        assertEquals(Transition.LEFT, result.single().transition)
    }

    @Test
    fun `staying inside or staying outside produces no alert`() {
        assertTrue(Geofence.transitions(listOf(home), atHome, atHome).isEmpty())
        assertTrue(Geofence.transitions(listOf(home), farAway, farAway).isEmpty())
    }

    @Test
    fun `first ever ping inside a place counts as an arrival`() {
        val result = Geofence.transitions(listOf(home), previous = null, current = atHome)
        assertEquals(Transition.ARRIVED, result.single().transition)
    }

    @Test
    fun `first ever ping outside any place produces nothing`() {
        assertTrue(Geofence.transitions(listOf(home), previous = null, current = farAway).isEmpty())
    }
}
