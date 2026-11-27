package com.familylinklive.app.ui.login

import android.content.Intent
import android.os.Bundle
import android.util.Log
import android.view.View
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.familylinklive.app.R
import com.familylinklive.app.data.ApiClient
import com.familylinklive.app.data.SessionManager
import com.familylinklive.app.data.models.LoginRequest
import com.familylinklive.app.data.models.SettingsRequest
import com.familylinklive.app.databinding.ActivityLoginBinding
import com.familylinklive.app.push.PushTokenRegistrar
import com.familylinklive.app.ui.main.MainActivity
import com.familylinklive.app.util.LocaleCodes
import com.familylinklive.app.util.LocaleHelper
import com.google.android.gms.auth.api.signin.GoogleSignIn
import com.google.android.gms.auth.api.signin.GoogleSignInClient
import com.google.android.gms.auth.api.signin.GoogleSignInOptions
import com.google.android.gms.common.api.ApiException
import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.auth.GoogleAuthProvider
import kotlinx.coroutines.launch

/**
 * REQ-1 / REQ-2: Single Sign-On using Google Sign-In + Firebase Authentication.
 *
 * Flow: Google Sign-In -> Firebase credential -> Firebase ID token -> our own
 * backend verifies that token (server-side, via Firebase Admin) and returns
 * an app-specific JWT used for every subsequent REST API call.
 *
 * Part 3: after a successful login the app also registers its push (FCM) token
 * and tells the server which language the user reads, so notifications arrive
 * in the right language.
 */
class LoginActivity : AppCompatActivity() {

    private lateinit var binding: ActivityLoginBinding
    private lateinit var googleSignInClient: GoogleSignInClient
    private lateinit var auth: FirebaseAuth
    private lateinit var session: SessionManager

    private val signInLauncher = registerForActivityResult(
        ActivityResultContracts.StartActivityForResult()
    ) { result ->
        val task = GoogleSignIn.getSignedInAccountFromIntent(result.data)
        try {
            val account = task.getResult(ApiException::class.java)
            Log.d(TAG, "Google account chosen, exchanging for a Firebase credential")
            firebaseAuthWithGoogle(account.idToken!!)
        } catch (e: ApiException) {
            Log.w(TAG, "Google sign-in failed, status code ${e.statusCode}")
            showError(getString(R.string.sign_in_failed, e.statusCode))
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        Log.d(TAG, "onCreate")
        binding = ActivityLoginBinding.inflate(layoutInflater)
        setContentView(binding.root)

        auth = FirebaseAuth.getInstance()
        session = SessionManager.getInstance(this)

        /*
        Title: Authenticate Using Google Sign-In with Android
        Author(s): Google
        Date: 2024
        Version: play-services-auth 21.2.0
        Type: Documentation
        Availability: https://firebase.google.com/docs/auth/android/google-signin
        */
        val gso = GoogleSignInOptions.Builder(GoogleSignInOptions.DEFAULT_SIGN_IN)
            .requestIdToken(getString(R.string.default_web_client_id))
            .requestEmail()
            .build()
        googleSignInClient = GoogleSignIn.getClient(this, gso)

        binding.btnGoogleSignIn.setOnClickListener {
            Log.d(TAG, "Continue with Google tapped")
            signInLauncher.launch(googleSignInClient.signInIntent)
        }

        if (session.isLoggedIn) {
            Log.d(TAG, "existing session found - skipping login screen")
            goToMain()
        }
    }

    /*
    Title: Authenticate Using Google Sign-In with Android - exchanging a
    Google credential for a Firebase user
    Author(s): Google
    Date: 2024
    Version: Firebase BOM 33.1.2
    Type: Documentation
    Availability: https://firebase.google.com/docs/auth/android/google-signin
    */
    private fun firebaseAuthWithGoogle(idToken: String) {
        setLoading(true)
        val credential = GoogleAuthProvider.getCredential(idToken, null)
        auth.signInWithCredential(credential)
            .addOnSuccessListener { result ->
                Log.d(TAG, "Firebase sign-in succeeded")
                result.user?.getIdToken(true)
                    ?.addOnSuccessListener { tokenResult -> loginToBackend(tokenResult.token ?: "") }
                    ?.addOnFailureListener { showError(it.message) }
            }
            .addOnFailureListener {
                Log.w(TAG, "Firebase sign-in failed: ${it.message}")
                showError(it.message)
            }
    }

    private fun loginToBackend(firebaseIdToken: String) {
        lifecycleScope.launch {
            try {
                val api = ApiClient.getService(this@LoginActivity)
                val response = api.login(LoginRequest(firebaseIdToken))
                session.token = response.token
                session.user = response.user
                Log.i(TAG, "backend login OK for user ${response.user.id}")

                PushTokenRegistrar.registerCurrent(this@LoginActivity)
                // Tell the server which language to write this user's notifications in.
                runCatching {
                    api.updateSettings(SettingsRequest(language = LocaleCodes.toServer(LocaleHelper.currentCode())))
                }
                goToMain()
            } catch (e: Exception) {
                Log.e(TAG, "backend login failed", e)
                showError(getString(R.string.server_unreachable, e.message))
            }
        }
    }

    private fun setLoading(loading: Boolean) {
        binding.progress.visibility = if (loading) View.VISIBLE else View.GONE
        binding.btnGoogleSignIn.isEnabled = !loading
    }

    private fun showError(message: String?) {
        setLoading(false)
        Toast.makeText(this, message ?: getString(R.string.something_went_wrong), Toast.LENGTH_LONG).show()
    }

    private fun goToMain() {
        startActivity(Intent(this, MainActivity::class.java))
        finish()
    }

    private companion object {
        const val TAG = "LoginActivity"
    }
}
