package com.familylinklive.api.storage

import io.ktor.client.HttpClient
import io.ktor.client.engine.cio.CIO
import io.ktor.client.request.delete
import io.ktor.client.request.header
import io.ktor.client.request.put
import io.ktor.client.request.setBody
import io.ktor.client.statement.bodyAsText
import io.ktor.http.ContentType
import io.ktor.http.contentType
import io.ktor.http.isSuccess

/**
 * Azure Blob Storage (Part 3 - blob storage requirement).
 *
 * Profile photos are stored as block blobs in a container. The server holds
 * a container-level SAS URL (AZURE_BLOB_SAS_URL) that only permits creating,
 * writing and deleting blobs - never listing or reading the account key - so a leaked environment
 * variable can't wipe the storage account the way an account key could.
 * Photos are publicly READABLE (container access level "Blob") so the app
 * can load them straight from the returned URL without going through us.
 */
object BlobStorage {

    private val sasUrl: String? = System.getenv("AZURE_BLOB_SAS_URL")?.takeIf { it.isNotBlank() }
    private val client = HttpClient(CIO)

    val isConfigured: Boolean get() = sasUrl != null

    /*
    Title: Put Blob - uploading a block blob with the Blob service REST API
    Author(s): Microsoft
    Date: 2024
    Version: Blob service REST API, x-ms-version 2022-11-02
    Type: Documentation
    Availability: https://learn.microsoft.com/en-us/rest/api/storageservices/put-blob
    */
    suspend fun uploadProfilePhoto(userId: String, bytes: ByteArray): String {
        val sas = requireNotNull(sasUrl) { "AZURE_BLOB_SAS_URL is not configured" }
        val baseUrl = sas.substringBefore("?").trimEnd('/')
        val sasToken = sas.substringAfter("?", "")
        val blobPath = "$userId/avatar.jpg"

        val response = client.put("$baseUrl/$blobPath?$sasToken") {
            header("x-ms-blob-type", "BlockBlob")
            header("x-ms-version", "2022-11-02")
            header("x-ms-blob-content-type", "image/jpeg")
            contentType(ContentType.Image.JPEG)
            setBody(bytes)
        }
        if (!response.status.isSuccess()) {
            error("Azure Blob upload failed: ${response.status} ${response.bodyAsText().take(300)}")
        }
        // Cache-busting query so the app reloads the picture after it changes.
        return "$baseUrl/$blobPath?v=${System.currentTimeMillis()}"
    }

    /** Removes a user's profile photo when they delete their account (the SAS needs the "d" permission). */
    suspend fun deleteProfilePhoto(userId: String) {
        val sas = sasUrl ?: return
        val baseUrl = sas.substringBefore("?").trimEnd('/')
        val sasToken = sas.substringAfter("?", "")
        client.delete("$baseUrl/$userId/avatar.jpg?$sasToken") {
            header("x-ms-version", "2022-11-02")
        } // a 404 (no photo was ever uploaded) is fine, so the status is not checked
    }
}
