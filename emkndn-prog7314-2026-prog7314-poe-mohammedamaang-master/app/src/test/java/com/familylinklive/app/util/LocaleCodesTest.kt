package com.familylinklive.app.util

import org.junit.Assert.assertEquals
import org.junit.Test

/** Multi-language support: mapping whatever the phone reports onto our three languages. */
class LocaleCodesTest {

    @Test
    fun `regional tags collapse to the base language`() {
        assertEquals("af", LocaleCodes.normalize("af-ZA"))
        assertEquals("zu", LocaleCodes.normalize("zu_ZA"))
        assertEquals("en", LocaleCodes.normalize("en-GB"))
    }

    @Test
    fun `case does not matter`() {
        assertEquals("af", LocaleCodes.normalize("AF"))
        assertEquals("zu", LocaleCodes.normalize("Zu"))
    }

    @Test
    fun `unsupported or missing languages fall back to English`() {
        assertEquals("en", LocaleCodes.normalize("fr-FR"))
        assertEquals("en", LocaleCodes.normalize(""))
        assertEquals("en", LocaleCodes.normalize(null))
    }

    @Test
    fun `server codes are upper case and always one of EN AF ZU`() {
        assertEquals("EN", LocaleCodes.toServer("en"))
        assertEquals("AF", LocaleCodes.toServer("af-ZA"))
        assertEquals("ZU", LocaleCodes.toServer("zu"))
        assertEquals("EN", LocaleCodes.toServer("xx"))
    }
}
