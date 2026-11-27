package com.familylinklive.app.push

import android.content.Context
import android.util.Log
import com.familylinklive.app.data.ApiClient
import com.familylinklive.app.data.SessionManager
import com.familylinklive.app.data.models.FcmTokenRequest
import com.google.firebase.messaging.FirebaseMessaging
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch

/** Keeps the server's record of "which phone belongs to which user" up to date for push delivery. */
object PushTokenRegistrar {
    private const val TAG = "PushTokenRegistrar"
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    /** Fetches the current FCM token and registers it. Safe to call often (e.g. every app start). */
    fun registerCurrent(context: Context) {
        FirebaseMessaging.getInstance().token
            .addOnSuccessListener { token -> register(context, token) }
            .addOnFailureListener { Log.w(TAG, "could not fetch FCM token: ${it.message}") }
    }

    fun register(context: Context, token: String) {
        val session = SessionManager.getInstance(context)
        session.fcmToken = token
        if (!session.isLoggedIn) return
        scope.launch {
            try {
                ApiClient.getService(context).registerFcmToken(FcmTokenRequest(token))
                Log.i(TAG, "FCM token registered with the server")
            } catch (e: Exception) {
                Log.w(TAG, "FCM token registration failed (${e.javaClass.simpleName}); will retry next launch")
            }
        }
    }

    /** Called on sign-out so a signed-out phone stops receiving the previous user's alerts. */
    suspend fun unregister(context: Context) {
        val token = SessionManager.getInstance(context).fcmToken ?: return
        try {
            ApiClient.getService(context).unregisterFcmToken(token)
            Log.i(TAG, "FCM token removed from the server")
        } catch (e: Exception) {
            Log.w(TAG, "could not remove FCM token (${e.javaClass.simpleName})")
        }
    }
}
