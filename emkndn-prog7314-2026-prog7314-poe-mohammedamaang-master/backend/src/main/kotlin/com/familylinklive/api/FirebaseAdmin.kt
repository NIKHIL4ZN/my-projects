package com.familylinklive.api

import com.google.auth.oauth2.GoogleCredentials
import com.google.firebase.FirebaseApp
import com.google.firebase.FirebaseOptions
import com.google.firebase.auth.FirebaseAuth
import java.io.File
import java.io.FileInputStream

/**
 * Verifies the Firebase ID tokens produced by the Android app's Google
 * Sign-In flow, using the Firebase Admin SDK (server-side, so the client can
 * never forge a token).
 *
 * Requires a Firebase service account key. See README Step 2 for how to
 * download it from the Firebase Console. The file is intentionally
 * git-ignored - never commit it.
 */
object FirebaseAdmin {

    private var initialized = false

    /** True once a service account was loaded - FCM and Firestore calls are skipped (not failed) when false, e.g. in CI. */
    val isReady: Boolean get() = initialized

    /*
    Title: Add the Firebase Admin SDK to your server - initialising with a
    service account
    Author(s): Google
    Date: 2024
    Version: firebase-admin 9.3.0
    Type: Documentation
    Availability: https://firebase.google.com/docs/admin/setup
    */
    fun init() {
        if (initialized) return

        val credentialsPath = System.getenv("FIREBASE_CREDENTIALS_PATH") ?: "serviceAccountKey.json"
        val file = File(credentialsPath)

        if (!file.exists()) {
            println(
                "WARNING: $credentialsPath not found. /auth/login will fail until you add it " +
                    "(see README Step 2: Firebase project setup)."
            )
            return
        }

        val options = FirebaseOptions.builder()
            .setCredentials(GoogleCredentials.fromStream(FileInputStream(file)))
            .build()
        FirebaseApp.initializeApp(options)
        initialized = true
        println("Firebase Admin initialised from $credentialsPath")
    }

    /*
    Title: Verify ID tokens using the Firebase Admin SDK
    Author(s): Google
    Date: 2024
    Version: firebase-admin 9.3.0
    Type: Documentation
    Availability: https://firebase.google.com/docs/auth/admin/verify-id-tokens
    */
    fun verifyIdToken(idToken: String): DecodedFirebaseUser {
        check(initialized) { "Firebase Admin was not initialised - see README Step 2." }
        val decoded = FirebaseAuth.getInstance().verifyIdToken(idToken)
        return DecodedFirebaseUser(
            uid = decoded.uid,
            email = decoded.email ?: "",
            name = decoded.name ?: decoded.email ?: "FamilyLink User",
            picture = decoded.picture
        )
    }
}

data class DecodedFirebaseUser(
    val uid: String,
    val email: String,
    val name: String,
    val picture: String?
)
