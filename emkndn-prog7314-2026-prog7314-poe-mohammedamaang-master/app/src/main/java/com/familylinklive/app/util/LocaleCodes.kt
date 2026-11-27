package com.familylinklive.app.util

/** Pure helpers for the three supported languages: English, Afrikaans, isiZulu. */
object LocaleCodes {
    val supported = listOf("en", "af", "zu")

    /** "af-ZA", "AF", "af_ZA" -> "af"; anything unsupported (or null) -> "en". */
    fun normalize(tag: String?): String {
        val language = tag?.substringBefore('-')?.substringBefore('_')?.lowercase()
        return if (language != null && language in supported) language else "en"
    }

    /** The backend stores languages upper-case: EN / AF / ZU. */
    fun toServer(code: String): String = normalize(code).uppercase()
}
