package com.familylinklive.app.util

import org.junit.Assert.assertEquals
import org.junit.Test

/** The battery-saving "Smart Ping" rhythm chosen from the person's speed. */
class SmartPingTest {

    @Test
    fun `standing still pings every five minutes`() {
        assertEquals(SmartPing.STATIONARY_MS, SmartPing.intervalFor(0f))
        assertEquals(SmartPing.STATIONARY_MS, SmartPing.intervalFor(0.5f))
    }

    @Test
    fun `walking pace pings every minute`() {
        assertEquals(SmartPing.WALKING_MS, SmartPing.intervalFor(1.4f))
        assertEquals(SmartPing.WALKING_MS, SmartPing.intervalFor(4.9f))
    }

    @Test
    fun `driving speed pings every fifteen seconds`() {
        assertEquals(SmartPing.DRIVING_MS, SmartPing.intervalFor(5f))
        assertEquals(SmartPing.DRIVING_MS, SmartPing.intervalFor(27f))
    }

    @Test
    fun `faster movement never means a slower rhythm`() {
        val speeds = listOf(0f, 0.7f, 0.8f, 2f, 4.9f, 5f, 30f)
        val intervals = speeds.map { SmartPing.intervalFor(it) }
        assertEquals(intervals.sortedDescending(), intervals)
    }
}
