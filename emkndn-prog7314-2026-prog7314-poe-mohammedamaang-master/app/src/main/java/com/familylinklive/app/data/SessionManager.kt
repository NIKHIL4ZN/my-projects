package com.familylinklive.app.data

import android.content.Context
import android.content.SharedPreferences
import android.util.Log
import com.familylinklive.app.data.models.UserDto
import com.google.gson.Gson

/**
 * Stores the signed-in user's session (app JWT + profile) and the local
 * "share my location" preference (REQ-5) in SharedPreferences.
 *
 * A production build would use EncryptedSharedPreferences; plain
 * SharedPreferences is used here to keep the prototype's setup simple.
 */
/*
Title: Save key-value data - SharedPreferences
Author(s): Android Developers
Date: 2023
Version: androidx.core-ktx 1.13.1
Type: Documentation
Availability: https://developer.android.com/training/data-storage/shared-preferences
*/
class SessionManager private constructor(context: Context) {

    private val prefs: SharedPreferences =
        context.getSharedPreferences("familylink_session", Context.MODE_PRIVATE)
    private val gson = Gson()

    var token: String?
        get() = prefs.getString(KEY_TOKEN, null)
        set(value) = prefs.edit().putString(KEY_TOKEN, value).apply()

    var user: UserDto?
        get() = prefs.getString(KEY_USER, null)?.let {
            runCatching { gson.fromJson(it, UserDto::class.java) }.getOrNull()
        }
        set(value) = prefs.edit().putString(KEY_USER, value?.let { gson.toJson(it) }).apply()

    var shareLocationEnabled: Boolean
        get() = prefs.getBoolean(KEY_SHARE_LOCATION, true)
        set(value) = prefs.edit().putBoolean(KEY_SHARE_LOCATION, value).apply()

    /** Part 3: app-lock with fingerprint / face (see util/BiometricHelper.kt). */
    var biometricEnabled: Boolean
        get() = prefs.getBoolean(KEY_BIOMETRIC, false)
        set(value) = prefs.edit().putBoolean(KEY_BIOMETRIC, value).apply()

    /** True once the "secure your app?" offer has been shown, so it only appears once. */
    var biometricOffered: Boolean
        get() = prefs.getBoolean(KEY_BIOMETRIC_OFFERED, false)
        set(value) = prefs.edit().putBoolean(KEY_BIOMETRIC_OFFERED, value).apply()

    /** When the app last went to the background; used to decide if the lock must be shown again. */
    var lastBackgroundAt: Long
        get() = prefs.getLong(KEY_LAST_BACKGROUND, 0L)
        set(value) = prefs.edit().putLong(KEY_LAST_BACKGROUND, value).apply()

    /** Part 3: keep sharing location while the app is closed (foreground service). */
    var backgroundSharing: Boolean
        get() = prefs.getBoolean(KEY_BACKGROUND_SHARING, false)
        set(value) = prefs.edit().putBoolean(KEY_BACKGROUND_SHARING, value).apply()

    /** Latest FCM registration token, kept so it can be removed from the server on sign-out. */
    var fcmToken: String?
        get() = prefs.getString(KEY_FCM_TOKEN, null)
        set(value) = prefs.edit().putString(KEY_FCM_TOKEN, value).apply()

    val isLoggedIn: Boolean get() = !token.isNullOrEmpty()

    fun clear() {
        Log.d(TAG, "clear(): session wiped (sign-out)")
        prefs.edit().clear().apply()
    }

    companion object {
        private const val KEY_TOKEN = "token"
        private const val KEY_USER = "user"
        private const val KEY_SHARE_LOCATION = "share_location"
        private const val KEY_BIOMETRIC = "biometric_enabled"
        private const val KEY_BIOMETRIC_OFFERED = "biometric_offered"
        private const val KEY_LAST_BACKGROUND = "last_background_at"
        private const val KEY_BACKGROUND_SHARING = "background_sharing"
        private const val KEY_FCM_TOKEN = "fcm_token"
        private const val TAG = "SessionManager"

        /*
        Title: Save data in a local database using Room - thread-safe
        singleton instance pattern
        Author(s): Android Developers
        Date: 2023
        Version: Kotlin 1.9.24
        Type: Documentation
        Availability: https://developer.android.com/training/data-storage/room
        */
        @Volatile private var instance: SessionManager? = null

        fun getInstance(context: Context): SessionManager =
            instance ?: synchronized(this) {
                instance ?: SessionManager(context.applicationContext).also { instance = it }
            }
    }
}
