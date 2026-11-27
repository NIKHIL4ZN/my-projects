package com.familylinklive.app.data

import android.content.Context
import android.util.Log
import com.familylinklive.app.BuildConfig
import com.familylinklive.app.data.models.LoginRequest
import com.familylinklive.app.data.models.LoginResponse
import com.google.android.gms.tasks.Tasks
import com.google.firebase.auth.FirebaseAuth
import com.google.gson.Gson
import okhttp3.Authenticator
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import okhttp3.Response
import okhttp3.Route
import java.util.concurrent.TimeUnit

/**
 * Re-authenticates silently when the server answers 401.
 *
 * The backend's own JWT lives for 24 hours. Without this, an app that was
 * offline for a day (or simply left alone) would start failing every request -
 * including the offline outbox trying to sync - until the user signed in
 * again by hand. Here we ask Firebase for a fresh ID token, swap it for a
 * new backend JWT at /auth/login, and replay the failed request.
 */
class TokenAuthenticator(context: Context) : Authenticator {

    private val session = SessionManager.getInstance(context)
    private val gson = Gson()

    /*
    Title: Authenticator - responding to authentication challenges (401)
    Author(s): Square, Inc.
    Date: 2023
    Version: OkHttp 4.12.0
    Type: Documentation
    Availability: https://square.github.io/okhttp/3.x/okhttp/okhttp3/Authenticator.html
    */
    override fun authenticate(route: Route?, response: Response): Request? {
        // Never try to refresh while already logging in, and give up after one retry.
        if (response.request.url.encodedPath.endsWith("auth/login")) return null
        if (priorAttempts(response) >= 1) return null

        val firebaseUser = FirebaseAuth.getInstance().currentUser ?: return null
        return try {
            // Runs on OkHttp's network thread, so blocking here is safe.
            val idToken = Tasks.await(firebaseUser.getIdToken(true), 10, TimeUnit.SECONDS).token ?: return null
            val body = gson.toJson(LoginRequest(idToken)).toRequestBody("application/json".toMediaType())
            val loginRequest = Request.Builder().url(BuildConfig.API_BASE_URL + "auth/login").post(body).build()

            OkHttpClient().newCall(loginRequest).execute().use { loginResponse ->
                if (!loginResponse.isSuccessful) {
                    Log.w(TAG, "Silent re-login rejected: HTTP ${loginResponse.code}")
                    return null
                }
                val parsed = gson.fromJson(loginResponse.body?.string(), LoginResponse::class.java)
                session.token = parsed.token
                session.user = parsed.user
                Log.i(TAG, "Session token refreshed silently after a 401")
                response.request.newBuilder().header("Authorization", "Bearer ${parsed.token}").build()
            }
        } catch (e: Exception) {
            Log.w(TAG, "Silent re-login failed: ${e.javaClass.simpleName}")
            null
        }
    }

    private fun priorAttempts(response: Response): Int {
        var count = 0
        var prior = response.priorResponse
        while (prior != null) {
            count++
            prior = prior.priorResponse
        }
        return count
    }

    private companion object {
        const val TAG = "TokenAuthenticator"
    }
}
