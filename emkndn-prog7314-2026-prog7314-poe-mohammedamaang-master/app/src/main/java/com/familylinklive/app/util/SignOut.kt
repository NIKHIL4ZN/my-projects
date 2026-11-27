package com.familylinklive.app.util

import android.content.Context
import android.content.Intent
import android.util.Log
import com.familylinklive.app.data.AppRepository
import com.familylinklive.app.data.SessionManager
import com.familylinklive.app.location.LocationSharingService
import com.familylinklive.app.push.PushTokenRegistrar
import com.familylinklive.app.ui.login.LoginActivity
import com.google.android.gms.auth.api.signin.GoogleSignIn
import com.google.android.gms.auth.api.signin.GoogleSignInOptions
import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.messaging.FirebaseMessaging

/** One place that fully signs the user out, used by Settings and by the lock screen. */
object SignOut {
    private const val TAG = "SignOut"

    /** [accountDeleted] = the server already removed this account (and its push token), so skip telling it. */
    suspend fun perform(context: Context, accountDeleted: Boolean = false) {
        val app = context.applicationContext
        Log.i(TAG, "signing out")
        // Order matters: the token must be removed from the server while we can still authenticate.
        if (!accountDeleted) PushTokenRegistrar.unregister(app)
        LocationSharingService.stop(app)
        AppRepository.get(app).clearLocalData()
        runCatching { FirebaseMessaging.getInstance().deleteToken() }
        FirebaseAuth.getInstance().signOut()
        GoogleSignIn.getClient(app, GoogleSignInOptions.Builder(GoogleSignInOptions.DEFAULT_SIGN_IN).build()).signOut()
        SessionManager.getInstance(app).clear()
    }

    fun openLogin(context: Context) {
        context.startActivity(
            Intent(context, LoginActivity::class.java).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK)
        )
    }
}
