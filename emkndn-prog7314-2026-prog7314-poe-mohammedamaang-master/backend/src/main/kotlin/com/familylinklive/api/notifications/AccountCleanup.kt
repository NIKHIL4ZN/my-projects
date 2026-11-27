package com.familylinklive.api.notifications

import com.familylinklive.api.FirebaseAdmin
import com.familylinklive.api.storage.BlobStorage
import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.cloud.FirestoreClient
import org.slf4j.LoggerFactory

/**
 * The parts of "delete my account" that live OUTSIDE our own database. Each step is
 * independent and best-effort: one cloud service being down must not leave the person
 * half-deleted in the others, so every failure is logged and the rest still run.
 */
object AccountCleanup {
    private val log = LoggerFactory.getLogger("AccountCleanup")

    suspend fun purge(userId: String, firebaseUid: String) {
        runCatching { BlobStorage.deleteProfilePhoto(userId) }
            .onFailure { log.warn("Blob cleanup failed: {}", it.message) }

        if (!FirebaseAdmin.isReady) return

        runCatching {
            // Firestore: delete the user's activity feed documents.
            FirestoreClient.getFirestore().collection("users").document(firebaseUid)
                .collection("activity").listDocuments().forEach { it.delete().get() }
        }.onFailure { log.warn("Firestore cleanup failed: {}", it.message) }

        runCatching { FirebaseAuth.getInstance().deleteUser(firebaseUid) }
            .onFailure { log.warn("Firebase Auth user deletion failed: {}", it.message) }
    }
}
