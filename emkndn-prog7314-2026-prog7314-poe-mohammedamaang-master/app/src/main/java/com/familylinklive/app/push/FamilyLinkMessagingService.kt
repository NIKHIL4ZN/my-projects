package com.familylinklive.app.push

import android.util.Log
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage

/*
Title: Receive messages in an Android app - FirebaseMessagingService
Author(s): Google
Date: 2024
Version: firebase-messaging (BoM 33.1.2)
Type: Documentation
Availability: https://firebase.google.com/docs/cloud-messaging/android/receive
*/
class FamilyLinkMessagingService : FirebaseMessagingService() {

    override fun onNewToken(token: String) {
        Log.i(TAG, "onNewToken - FCM issued a new registration token")
        PushTokenRegistrar.register(applicationContext, token)
    }

    /** Called for every data message the server sends, whether the app is open or closed. */
    override fun onMessageReceived(message: RemoteMessage) {
        val data = message.data
        val type = data["type"]
        Log.i(TAG, "onMessageReceived type=$type")
        if (type == null) return
        NotificationHelper.show(applicationContext, type, data["title"].orEmpty(), data["body"].orEmpty(), data)
    }

    private companion object {
        const val TAG = "FcmService"
    }
}
