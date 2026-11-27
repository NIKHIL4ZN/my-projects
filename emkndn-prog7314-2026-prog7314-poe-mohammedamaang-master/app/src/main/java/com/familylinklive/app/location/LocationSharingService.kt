package com.familylinklive.app.location

import android.app.PendingIntent
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.ServiceInfo
import android.location.Location
import android.os.Build
import android.os.IBinder
import android.os.Looper
import android.util.Log
import androidx.core.app.NotificationCompat
import androidx.core.app.ServiceCompat
import androidx.core.content.ContextCompat
import com.familylinklive.app.R
import com.familylinklive.app.data.AppRepository
import com.familylinklive.app.data.SessionManager
import com.familylinklive.app.push.NotificationHelper
import com.familylinklive.app.ui.main.MainActivity
import com.familylinklive.app.util.SmartPing
import com.google.android.gms.location.FusedLocationProviderClient
import com.google.android.gms.location.LocationCallback
import com.google.android.gms.location.LocationRequest
import com.google.android.gms.location.LocationResult
import com.google.android.gms.location.LocationServices
import com.google.android.gms.location.Priority
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.launch

/*
Title: Foreground services - location type
Author(s): Android Developers
Date: 2024
Version: API level 34
Type: Documentation
Availability: https://developer.android.com/develop/background-work/services/fgs/service-types#location
*/
/**
 * Optional "live location sharing" (Settings toggle). Runs as a visible foreground service so Android
 * lets it keep receiving GPS updates after the app is closed, WITHOUT needing the heavily-restricted
 * background-location permission. Every fix goes through the offline outbox, so it keeps working
 * through tunnels and dead zones and the server's geofence alerts see an unbroken trail.
 */
class LocationSharingService : Service() {

    private lateinit var client: FusedLocationProviderClient
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    private var currentIntervalMs = SmartPing.WALKING_MS

    private val callback = object : LocationCallback() {
        override fun onLocationResult(result: LocationResult) {
            result.lastLocation?.let { handle(it) }
        }
    }

    override fun onCreate() {
        super.onCreate()
        client = LocationServices.getFusedLocationProviderClient(this)
        NotificationHelper.createChannels(this)
        Log.i(TAG, "onCreate")
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        if (intent?.action == ACTION_STOP) {
            Log.i(TAG, "stop requested from the notification")
            SessionManager.getInstance(this).backgroundSharing = false
            stopForegroundCompat()
            stopSelf()
            return START_NOT_STICKY
        }
        startAsForeground()
        startUpdates(currentIntervalMs)
        return START_STICKY
    }

    private fun startAsForeground() {
        val openApp = PendingIntent.getActivity(
            this, 0, Intent(this, MainActivity::class.java), PendingIntent.FLAG_IMMUTABLE
        )
        val stop = PendingIntent.getService(
            this, 1, Intent(this, LocationSharingService::class.java).setAction(ACTION_STOP),
            PendingIntent.FLAG_IMMUTABLE
        )
        val notification = NotificationCompat.Builder(this, NotificationHelper.CH_LOCATION)
            .setSmallIcon(R.drawable.ic_notification)
            .setContentTitle(getString(R.string.notification_location_title))
            .setContentText(getString(R.string.notification_location_text))
            .setOngoing(true)
            .setContentIntent(openApp)
            .addAction(0, getString(R.string.notification_location_stop), stop)
            .build()
        ServiceCompat.startForeground(
            this, NOTIFICATION_ID, notification,
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) ServiceInfo.FOREGROUND_SERVICE_TYPE_LOCATION else 0
        )
    }

    private fun startUpdates(intervalMs: Long) {
        client.removeLocationUpdates(callback)
        val request = LocationRequest.Builder(Priority.PRIORITY_BALANCED_POWER_ACCURACY, intervalMs)
            .setMinUpdateIntervalMillis(intervalMs / 2)
            .setMinUpdateDistanceMeters(10f)
            .build()
        try {
            client.requestLocationUpdates(request, callback, Looper.getMainLooper())
            Log.i(TAG, "location updates every ${intervalMs / 1000}s")
        } catch (e: SecurityException) {
            Log.e(TAG, "location permission missing - stopping", e)
            stopSelf()
        }
    }

    private fun handle(location: Location) {
        val session = SessionManager.getInstance(this)
        if (!session.shareLocationEnabled || !session.isLoggedIn) return

        val capturedAt = if (location.time > 0) location.time else System.currentTimeMillis()
        scope.launch { AppRepository.get(applicationContext).queueLocation(location.latitude, location.longitude, capturedAt) }

        // Smart Ping: re-request at a different rhythm when the person starts or stops moving.
        val desired = SmartPing.intervalFor(if (location.hasSpeed()) location.speed else 0f)
        if (desired != currentIntervalMs) {
            Log.i(TAG, "speed changed - interval ${currentIntervalMs / 1000}s -> ${desired / 1000}s")
            currentIntervalMs = desired
            startUpdates(desired)
        }
    }

    private fun stopForegroundCompat() {
        ServiceCompat.stopForeground(this, ServiceCompat.STOP_FOREGROUND_REMOVE)
    }

    override fun onDestroy() {
        Log.i(TAG, "onDestroy")
        client.removeLocationUpdates(callback)
        scope.cancel()
        super.onDestroy()
    }

    override fun onBind(intent: Intent?): IBinder? = null

    companion object {
        private const val TAG = "LocationService"
        private const val NOTIFICATION_ID = 4101
        const val ACTION_STOP = "com.familylinklive.app.action.STOP_SHARING"

        fun start(context: Context) {
            ContextCompat.startForegroundService(context, Intent(context, LocationSharingService::class.java))
        }

        fun stop(context: Context) {
            context.stopService(Intent(context, LocationSharingService::class.java))
        }
    }
}
