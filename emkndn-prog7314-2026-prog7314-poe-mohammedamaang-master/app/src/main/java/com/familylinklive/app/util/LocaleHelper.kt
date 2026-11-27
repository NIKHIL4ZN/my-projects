package com.familylinklive.app.util

import androidx.appcompat.app.AppCompatDelegate
import androidx.core.os.LocaleListCompat
import java.util.Locale

/*
Title: Per-app language preferences
Author(s): Android Developers
Date: 2024
Version: androidx.appcompat 1.7.0
Type: Documentation
Availability: https://developer.android.com/guide/topics/resources/app-languages
*/
object LocaleHelper {

    /** The language the UI is showing right now: "en", "af" or "zu". */
    fun currentCode(): String {
        val chosen = AppCompatDelegate.getApplicationLocales()
        val tag = if (!chosen.isEmpty()) chosen.get(0)?.toLanguageTag() else Locale.getDefault().toLanguageTag()
        return LocaleCodes.normalize(tag)
    }

    /** Switches the whole app's language immediately (the activity is recreated by AppCompat). */
    fun apply(code: String) {
        AppCompatDelegate.setApplicationLocales(LocaleListCompat.forLanguageTags(LocaleCodes.normalize(code)))
    }

    /** Each language is always shown in its own name, whatever language the UI is in. */
    fun displayName(code: String): String = when (code) {
        "af" -> "Afrikaans"
        "zu" -> "isiZulu"
        else -> "English"
    }
}
