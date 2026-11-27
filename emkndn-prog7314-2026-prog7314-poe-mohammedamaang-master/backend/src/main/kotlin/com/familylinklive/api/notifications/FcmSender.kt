package com.familylinklive.api.notifications

import com.familylinklive.api.FirebaseAdmin
import com.familylinklive.api.db.DatabaseFactory.dbQuery
import com.familylinklive.api.db.DeviceTokens
import com.google.firebase.messaging.AndroidConfig
import com.google.firebase.messaging.FirebaseMessaging
import com.google.firebase.messaging.FirebaseMessagingException
import com.google.firebase.messaging.Message
import com.google.firebase.messaging.MessagingErrorCode
import org.jetbrains.exposed.sql.SqlExpressionBuilder.eq
import org.jetbrains.exposed.sql.deleteWhere
import org.slf4j.LoggerFactory

/**
 * Sends real-time push notifications through Firebase Cloud Messaging
 * (Part 3 - Real-time Notifications).
 *
 * Messages are DATA-only with HIGH priority: the app builds the visible
 * notification itself so it can pick the right channel and show text in
 * the user's chosen language, whether the app is open or closed.
 */
object FcmSender {
    private val log = LoggerFactory.getLogger("FcmSender")

    /*
    Title: Send messages to specific devices - Firebase Admin SDK (Java)
    Author(s): Google
    Date: 2024
    Version: firebase-admin 9.3.0
    Type: Documentation
    Availability: https://firebase.google.com/docs/cloud-messaging/send-message
    */
    suspend fun send(tokens: List<String>, title: String, body: String, data: Map<String, String>) {
        if (!FirebaseAdmin.isReady) {
            log.info("FCM skipped - Firebase Admin is not initialised")
            return
        }
        for (token in tokens) {
            try {
                val message = Message.builder()
                    .setToken(token)
                    .putAllData(data + mapOf("title" to title, "body" to body))
                    .setAndroidConfig(AndroidConfig.builder().setPriority(AndroidConfig.Priority.HIGH).build())
                    .build()
                val id = FirebaseMessaging.getInstance().send(message)
                log.info("FCM sent ({}) type={}", id, data["type"])
            } catch (e: FirebaseMessagingException) {
                log.warn("FCM send failed: {} / {}", e.messagingErrorCode, e.message)
                // A token that Google says is dead (app uninstalled / token rotated) is removed so we stop retrying it.
                if (e.messagingErrorCode == MessagingErrorCode.UNREGISTERED ||
                    e.messagingErrorCode == MessagingErrorCode.INVALID_ARGUMENT
                ) {
                    dbQuery { DeviceTokens.deleteWhere { DeviceTokens.token eq token } }
                }
            } catch (e: Exception) {
                log.warn("FCM send error: {}", e.message)
            }
        }
    }
}
