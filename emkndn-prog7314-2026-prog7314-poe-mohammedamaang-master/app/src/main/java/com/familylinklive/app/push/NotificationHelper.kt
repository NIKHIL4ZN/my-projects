package com.familylinklive.app.push

import android.Manifest
import android.annotation.SuppressLint
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import android.util.Log
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import androidx.core.content.ContextCompat
import com.familylinklive.app.R
import com.familylinklive.app.ui.main.MainActivity

/**
 * Builds and shows the notifications that arrive through Firebase Cloud Messaging.
 * Each kind of alert has its own channel so people can tune them separately
 * in system settings; SOS gets the loudest one.
 */
object NotificationHelper {
    private const val TAG = "NotificationHelper"

    const val CH_MESSAGES = "messages"
    const val CH_CONTACTS = "contacts"
    const val CH_GEOFENCE = "geofence"
    const val CH_SOS = "sos"
    const val CH_LOCATION = "location_sharing"
    const val EXTRA_TYPE = "type"

    /*
    Title: Create and manage notification channels
    Author(s): Android Developers
    Date: 2024
    Version: API level 26+
    Type: Documentation
    Availability: https://developer.android.com/develop/ui/views/notifications/channels
    */
    fun createChannels(context: Context) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return
        val manager = context.getSystemService(NotificationManager::class.java)

        fun channel(id: String, nameRes: Int, importance: Int) =
            NotificationChannel(id, context.getString(nameRes), importance)

        manager.createNotificationChannels(
            listOf(
                channel(CH_MESSAGES, R.string.channel_messages, NotificationManager.IMPORTANCE_HIGH),
                channel(CH_CONTACTS, R.string.channel_contacts, NotificationManager.IMPORTANCE_DEFAULT),
                channel(CH_GEOFENCE, R.string.channel_geofence, NotificationManager.IMPORTANCE_DEFAULT),
                channel(CH_SOS, R.string.channel_sos, NotificationManager.IMPORTANCE_HIGH).apply {
                    enableVibration(true)
                    vibrationPattern = longArrayOf(0, 500, 250, 500, 250, 500)
                    // Only takes effect if the user grants Do-Not-Disturb access; harmless otherwise.
                    setBypassDnd(true)
                },
                channel(CH_LOCATION, R.string.channel_location, NotificationManager.IMPORTANCE_LOW)
            )
        )
    }

    fun channelFor(type: String): String = when {
        type == "SOS" -> CH_SOS
        type.startsWith("GEOFENCE") -> CH_GEOFENCE
        type.startsWith("INVITE") -> CH_CONTACTS
        else -> CH_MESSAGES
    }

    @SuppressLint("MissingPermission")
    fun show(context: Context, type: String, title: String, body: String, data: Map<String, String>) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU &&
            ContextCompat.checkSelfPermission(context, Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED
        ) {
            Log.w(TAG, "notification dropped - POST_NOTIFICATIONS not granted")
            return
        }

        // Tapping the notification re-opens the app and tells it what to show (see MainActivity.handleIntent).
        val intent = Intent(context, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP
            putExtra(EXTRA_TYPE, type)
            data.forEach { (key, value) -> putExtra(key, value) }
        }
        val notificationId = (data["contactId"] ?: type).hashCode()
        val pendingIntent = PendingIntent.getActivity(
            context, notificationId, intent, PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )

        val isSos = type == "SOS"
        val notification = NotificationCompat.Builder(context, channelFor(type))
            .setSmallIcon(R.drawable.ic_notification)
            .setContentTitle(title)
            .setContentText(body)
            .setStyle(NotificationCompat.BigTextStyle().bigText(body))
            .setAutoCancel(true)
            .setContentIntent(pendingIntent)
            .setPriority(if (isSos) NotificationCompat.PRIORITY_MAX else NotificationCompat.PRIORITY_HIGH)
            .apply { if (isSos) setCategory(NotificationCompat.CATEGORY_ALARM) }
            .build()

        NotificationManagerCompat.from(context).notify(notificationId, notification)
        Log.i(TAG, "shown $type notification (channel ${channelFor(type)})")
    }
}
