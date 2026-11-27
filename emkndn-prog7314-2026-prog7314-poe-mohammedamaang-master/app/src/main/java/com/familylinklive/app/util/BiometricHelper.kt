package com.familylinklive.app.util

import android.content.Context
import android.os.Build
import android.util.Log
import androidx.biometric.BiometricManager
import androidx.biometric.BiometricManager.Authenticators.BIOMETRIC_WEAK
import androidx.biometric.BiometricManager.Authenticators.DEVICE_CREDENTIAL
import androidx.biometric.BiometricPrompt
import androidx.core.content.ContextCompat
import androidx.fragment.app.FragmentActivity

/*
Title: Show a biometric authentication dialog
Author(s): Android Developers
Date: 2024
Version: androidx.biometric 1.1.0
Type: Documentation
Availability: https://developer.android.com/identity/sign-in/biometric-auth
*/
object BiometricHelper {
    private const val TAG = "BiometricHelper"

    /**
     * Fingerprint or face (class 2 / "weak" or better). On Android 11+ the device PIN / pattern is
     * accepted as a fallback; on older versions the prompt can't combine the two, so it shows a
     * "Cancel" button instead.
     */
    private fun authenticators(): Int =
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) BIOMETRIC_WEAK or DEVICE_CREDENTIAL else BIOMETRIC_WEAK

    fun canAuthenticate(context: Context): Boolean {
        val status = BiometricManager.from(context).canAuthenticate(authenticators())
        Log.d(TAG, "canAuthenticate status=$status")
        return status == BiometricManager.BIOMETRIC_SUCCESS
    }

    fun authenticate(
        activity: FragmentActivity,
        title: String,
        subtitle: String,
        negativeText: String,
        onSuccess: () -> Unit,
        onFailure: (errorCode: Int, message: CharSequence) -> Unit
    ) {
        val prompt = BiometricPrompt(
            activity,
            ContextCompat.getMainExecutor(activity),
            object : BiometricPrompt.AuthenticationCallback() {
                override fun onAuthenticationSucceeded(result: BiometricPrompt.AuthenticationResult) {
                    Log.i(TAG, "biometric authentication succeeded")
                    onSuccess()
                }

                override fun onAuthenticationError(errorCode: Int, errString: CharSequence) {
                    Log.w(TAG, "biometric authentication error $errorCode")
                    onFailure(errorCode, errString)
                }

                override fun onAuthenticationFailed() {
                    // A single non-matching attempt: the system prompt stays open and lets the user retry.
                    Log.d(TAG, "biometric attempt did not match")
                }
            }
        )
        val info = BiometricPrompt.PromptInfo.Builder()
            .setTitle(title)
            .setSubtitle(subtitle)
            .setAllowedAuthenticators(authenticators())
            .apply { if (Build.VERSION.SDK_INT < Build.VERSION_CODES.R) setNegativeButtonText(negativeText) }
            .build()
        prompt.authenticate(info)
    }
}
