package com.familylinklive.app.util

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Unit tests for PlaceValidator, run automatically by GitHub Actions on every
 * push (see .github/workflows/android-ci.yml -> ./gradlew testDebugUnitTest).
 */
class PlaceValidatorTest {

    @Test
    fun `radius within 100 to 2000 metres is valid`() {
        assertTrue(PlaceValidator.isValidRadius(100))
        assertTrue(PlaceValidator.isValidRadius(2000))
        assertTrue(PlaceValidator.isValidRadius(300))
    }

    @Test
    fun `radius outside 100 to 2000 metres is invalid`() {
        assertFalse(PlaceValidator.isValidRadius(50))
        assertFalse(PlaceValidator.isValidRadius(5000))
    }

    @Test
    fun `blank place names are invalid`() {
        assertFalse(PlaceValidator.isValidName(""))
        assertFalse(PlaceValidator.isValidName("   "))
        assertTrue(PlaceValidator.isValidName("Home"))
    }

    @Test
    fun `coerceRadius clamps out-of-range values into the valid band`() {
        assertEquals(100, PlaceValidator.coerceRadius(10))
        assertEquals(2000, PlaceValidator.coerceRadius(9000))
        assertEquals(500, PlaceValidator.coerceRadius(500))
    }

    @Test
    fun `place name at the 128 character boundary is valid`() {
        val name128 = "A".repeat(128)
        val name129 = "A".repeat(129)
        assertTrue(PlaceValidator.isValidName(name128))
        assertFalse(PlaceValidator.isValidName(name129))
    }
}
