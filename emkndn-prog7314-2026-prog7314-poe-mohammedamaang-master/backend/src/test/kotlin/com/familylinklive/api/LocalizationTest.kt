package com.familylinklive.api

import com.familylinklive.api.notifications.Localization
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNotEquals
import kotlin.test.assertTrue

/** Every notification must exist, be non-empty and carry the person's name in all three languages. */
class LocalizationTest {

    private val languages = listOf("EN", "AF", "ZU")

    @Test
    fun `every key renders in every language and mentions the actor`() {
        for (language in languages) {
            for (key in Localization.Key.values()) {
                val (title, body) = Localization.text(language, key, listOf("Thandi", "Home"))
                assertTrue(title.isNotBlank(), "$language/$key title blank")
                assertTrue(body.isNotBlank(), "$language/$key body blank")
                assertTrue("Thandi" in title + body, "$language/$key does not mention the name")
            }
        }
    }

    @Test
    fun `languages are genuinely different`() {
        val en = Localization.text("EN", Localization.Key.ARRIVED, listOf("Thandi", "Home")).second
        val af = Localization.text("AF", Localization.Key.ARRIVED, listOf("Thandi", "Home")).second
        val zu = Localization.text("ZU", Localization.Key.ARRIVED, listOf("Thandi", "Home")).second
        assertEquals("Thandi arrived at Home", en)
        assertNotEquals(en, af)
        assertNotEquals(en, zu)
        assertNotEquals(af, zu)
    }

    @Test
    fun `unknown language falls back to English and lowercase codes work`() {
        val fallback = Localization.text("FR", Localization.Key.SOS, listOf("Sam")).first
        val english = Localization.text("EN", Localization.Key.SOS, listOf("Sam")).first
        assertEquals(english, fallback)
        assertEquals(
            Localization.text("AF", Localization.Key.LEFT, listOf("Sam", "Work")),
            Localization.text("af", Localization.Key.LEFT, listOf("Sam", "Work"))
        )
    }

    @Test
    fun `message notification shows the message text as the body`() {
        val (_, body) = Localization.text("EN", Localization.Key.NEW_MESSAGE, listOf("Sam", "On my way"))
        assertEquals("On my way", body)
    }
}
