package com.familylinklive.app.ui.main

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import android.util.Log
import android.view.View
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.core.os.bundleOf
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import androidx.navigation.NavController
import androidx.navigation.fragment.NavHostFragment
import androidx.navigation.ui.setupWithNavController
import com.familylinklive.app.R
import com.familylinklive.app.data.ApiClient
import com.familylinklive.app.data.SessionManager
import com.familylinklive.app.data.local.AppDatabase
import com.familylinklive.app.data.models.SettingsRequest
import com.familylinklive.app.databinding.ActivityMainBinding
import com.familylinklive.app.location.LocationSharingService
import com.familylinklive.app.push.NotificationHelper
import com.familylinklive.app.push.PushTokenRegistrar
import com.familylinklive.app.sync.NetworkMonitor
import com.familylinklive.app.ui.map.MapFocus
import com.familylinklive.app.util.AppScope
import com.familylinklive.app.util.BiometricHelper
import com.familylinklive.app.util.SignOut
import com.familylinklive.app.util.hasFineLocationPermission
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.launch

/**
 * Post-login hub: hosts the five bottom-navigation destinations (Map, Contacts, Places,
 * Activity, Settings) from the Part 1 navigation wireflow, and in Part 3 also owns:
 *  - the biometric app-lock (fingerprint / face) shown on cold start and after 30 s in the background;
 *  - the offline / "syncing" banner;
 *  - routing a tapped push notification to the right screen.
 */
class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding
    private lateinit var session: SessionManager
    private lateinit var navController: NavController

    private var unlockedThisSession = false
    private var promptShowing = false

    private val notificationPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { granted ->
        Log.i(TAG, "POST_NOTIFICATIONS granted=$granted")
        if (!granted) Toast.makeText(this, R.string.notification_permission_denied, Toast.LENGTH_LONG).show()
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        Log.d(TAG, "onCreate")

        session = SessionManager.getInstance(this)
        if (!session.isLoggedIn) {
            Log.w(TAG, "no session - back to login")
            SignOut.openLogin(this)
            finish()
            return
        }

        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        /*
        Title: Update UI components with NavController - integrate with
        BottomNavigationView
        Author(s): Android Developers
        Date: 2024
        Version: Navigation 2.7.7
        Type: Documentation
        Availability: https://developer.android.com/guide/navigation/navigation-ui
        */
        val navHostFragment = supportFragmentManager
            .findFragmentById(binding.navHostFragment.id) as NavHostFragment
        navController = navHostFragment.navController
        binding.bottomNav.setupWithNavController(navController)

        binding.btnUnlock.setOnClickListener { promptBiometric() }
        binding.btnLockSignOut.setOnClickListener { signOut() }

        observeConnectivity()
        requestNotificationPermissionOnce()
        PushTokenRegistrar.registerCurrent(this)
        if (savedInstanceState == null) maybeOfferBiometric()
        handleIntent(intent)
    }

    override fun onStart() {
        super.onStart()
        if (!::binding.isInitialized) return
        Log.d(TAG, "onStart")
        if (needsUnlock()) lock()
        if (session.backgroundSharing && hasFineLocationPermission(this)) LocationSharingService.start(this)
    }

    override fun onStop() {
        if (::binding.isInitialized) {
            session.lastBackgroundAt = System.currentTimeMillis()
            Log.d(TAG, "onStop - app went to the background")
        }
        super.onStop()
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        if (::binding.isInitialized) handleIntent(intent)
    }

    /** Lets other screens switch tab (e.g. the Activity feed jumping to the Map). */
    fun selectTab(itemId: Int) {
        binding.bottomNav.selectedItemId = itemId
    }

    // ------------------------------------------------------------- biometric app-lock

    private fun needsUnlock(): Boolean {
        if (!session.biometricEnabled) return false
        if (!BiometricHelper.canAuthenticate(this)) {
            Log.w(TAG, "biometric lock is on but no biometric/screen lock is available - not locking")
            return false
        }
        val awayMs = System.currentTimeMillis() - session.lastBackgroundAt
        return !unlockedThisSession || awayMs > LOCK_TIMEOUT_MS
    }

    private fun lock() {
        Log.i(TAG, "locking the app")
        binding.lockOverlay.visibility = View.VISIBLE
        promptBiometric()
    }

    private fun promptBiometric() {
        if (promptShowing) return
        promptShowing = true
        BiometricHelper.authenticate(
            activity = this,
            title = getString(R.string.biometric_prompt_title),
            subtitle = getString(R.string.biometric_prompt_subtitle),
            negativeText = getString(R.string.biometric_cancel),
            onSuccess = {
                promptShowing = false
                unlockedThisSession = true
                binding.tvLockMessage.visibility = View.GONE
                binding.lockOverlay.visibility = View.GONE
            },
            onFailure = { _, message ->
                promptShowing = false
                binding.tvLockMessage.text = message
                binding.tvLockMessage.visibility = View.VISIBLE
            }
        )
    }

    /** First run: offer to switch the lock on (Part 1 "Biometric Setup" screen). Shown only once. */
    private fun maybeOfferBiometric() {
        if (session.biometricOffered || session.biometricEnabled) return
        if (!BiometricHelper.canAuthenticate(this)) {
            Log.d(TAG, "biometric offer skipped - not available on this device")
            return
        }
        session.biometricOffered = true
        MaterialAlertDialogBuilder(this)
            .setTitle(R.string.biometric_offer_title)
            .setMessage(R.string.biometric_offer_message)
            .setPositiveButton(R.string.biometric_enable) { _, _ ->
                BiometricHelper.authenticate(
                    activity = this,
                    title = getString(R.string.biometric_prompt_title),
                    subtitle = getString(R.string.biometric_prompt_subtitle),
                    negativeText = getString(R.string.biometric_cancel),
                    onSuccess = {
                        session.biometricEnabled = true
                        unlockedThisSession = true
                        AppScope.scope.launch {
                            runCatching { ApiClient.getService(applicationContext).updateSettings(SettingsRequest(biometricEnabled = true)) }
                        }
                        Toast.makeText(this, R.string.biometric_enabled_toast, Toast.LENGTH_SHORT).show()
                    },
                    onFailure = { code, _ -> Log.w(TAG, "biometric enrolment cancelled ($code)") }
                )
            }
            .setNegativeButton(R.string.biometric_skip, null)
            .show()
    }

    private fun signOut() {
        val context = applicationContext
        lifecycleScope.launch {
            SignOut.perform(context)
            SignOut.openLogin(context)
            finish()
        }
    }

    // ------------------------------------------------------------- offline banner

    private fun observeConnectivity() {
        val monitor = NetworkMonitor.get(this)
        val outbox = AppDatabase.get(this).outboxDao()
        lifecycleScope.launch {
            repeatOnLifecycle(Lifecycle.State.STARTED) {
                combine(monitor.online, outbox.countFlow()) { online, pending -> online to pending }
                    .collect { (online, pending) -> updateBanner(online, pending) }
            }
        }
    }

    private fun updateBanner(online: Boolean, pending: Int) {
        val banner = binding.tvBanner
        when {
            !online -> {
                banner.text = if (pending > 0) getString(R.string.banner_offline_pending, pending) else getString(R.string.banner_offline)
                banner.setBackgroundColor(ContextCompat.getColor(this, R.color.brand_warning))
                banner.visibility = View.VISIBLE
            }
            pending > 0 -> {
                banner.text = getString(R.string.banner_syncing, pending)
                banner.setBackgroundColor(ContextCompat.getColor(this, R.color.brand_accent))
                banner.visibility = View.VISIBLE
            }
            else -> banner.visibility = View.GONE
        }
    }

    // ------------------------------------------------------------- notifications

    private fun requestNotificationPermissionOnce() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) return
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.POST_NOTIFICATIONS) == PackageManager.PERMISSION_GRANTED) return
        val flags = getSharedPreferences("familylink_flags", MODE_PRIVATE)
        if (flags.getBoolean("notification_permission_asked", false)) return
        flags.edit().putBoolean("notification_permission_asked", true).apply()
        notificationPermissionLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
    }

    /** A tapped notification carries a "type" extra; open the screen that makes sense for it. */
    private fun handleIntent(intent: Intent?) {
        val type = intent?.getStringExtra(NotificationHelper.EXTRA_TYPE) ?: return
        Log.i(TAG, "opened from a notification: $type")
        val lat = intent.getStringExtra("lat")?.toDoubleOrNull()
        val lng = intent.getStringExtra("lng")?.toDoubleOrNull()
        val label = intent.getStringExtra("label")

        when {
            type == "MESSAGE" -> {
                val contactId = intent.getStringExtra("contactId")
                if (contactId != null) {
                    navController.navigate(
                        R.id.chatThreadFragment,
                        bundleOf("contactId" to contactId, "contactName" to intent.getStringExtra("contactName").orEmpty())
                    )
                }
            }
            type == "SOS" || type.startsWith("GEOFENCE") -> {
                if (lat != null && lng != null) MapFocus.request(lat, lng, label, sos = type == "SOS")
                selectTab(R.id.mapFragment)
            }
            type.startsWith("INVITE") -> selectTab(R.id.contactsFragment)
        }
        // Consume it so rotating the screen doesn't replay the navigation.
        intent.removeExtra(NotificationHelper.EXTRA_TYPE)
    }

    private companion object {
        const val TAG = "MainActivity"
        const val LOCK_TIMEOUT_MS = 30_000L
    }
}
