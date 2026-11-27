package com.familylinklive.api.notifications

import com.familylinklive.api.FirebaseAdmin
import com.google.firebase.cloud.FirestoreClient

/**
 * Writes alert history to Cloud Firestore (NoSQL document database).
 *
 * Layout:  users/{recipientFirebaseUid}/activity/{autoId}
 * The Android app READS this collection directly with a real-time
 * snapshot listener (see ui/activity/ActivityFragment.kt), authorised by
 * Firestore security rules that only let a user read their own subtree.
 * Only this server (Admin SDK) can write, so clients can't forge alerts.
 */
object ActivityFeed {

    /*
    Title: Add data to Cloud Firestore - Admin SDK (Java)
    Author(s): Google
    Date: 2024
    Version: firebase-admin 9.3.0
    Type: Documentation
    Availability: https://firebase.google.com/docs/firestore/manage-data/add-data
    */
    fun record(recipientFirebaseUid: String, type: String, fields: Map<String, Any>) {
        if (!FirebaseAdmin.isReady) return
        val doc = HashMap<String, Any>(fields)
        doc["type"] = type
        doc["createdAt"] = System.currentTimeMillis()
        FirestoreClient.getFirestore()
            .collection("users").document(recipientFirebaseUid)
            .collection("activity")
            .add(doc)
            .get() // block until written so a failure is reported to the caller's logger
    }
}
