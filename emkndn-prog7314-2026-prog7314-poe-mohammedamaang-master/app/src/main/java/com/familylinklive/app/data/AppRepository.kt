package com.familylinklive.app.data

import android.content.Context
import android.util.Log
import com.familylinklive.app.data.local.AppDatabase
import com.familylinklive.app.data.local.OutboxItem
import com.familylinklive.app.data.local.toDto
import com.familylinklive.app.data.local.toEntity
import com.familylinklive.app.data.models.ContactDto
import com.familylinklive.app.data.models.MessageDto
import com.familylinklive.app.data.models.PlaceDto
import com.familylinklive.app.sync.SyncManager
import com.familylinklive.app.util.MessageMerger
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.util.UUID
import kotlin.coroutines.cancellation.CancellationException

/**
 * The single door between the screens and the data (Part 3 - Offline mode).
 *
 *  - READS try the server first and refresh the Room cache; if the server
 *    can't be reached they quietly return the cached copy instead.
 *  - WRITES (message / location / SOS) are saved to the Room outbox first,
 *    then sent straight away if possible; if not, WorkManager syncs them
 *    when a connection returns.
 */
class AppRepository private constructor(context: Context) {

    private val appContext = context.applicationContext
    private val db = AppDatabase.get(appContext)
    private val session = SessionManager.getInstance(appContext)
    private val api: ApiService get() = ApiClient.getService(appContext)

    data class LoadResult<T>(val data: T, val fromCache: Boolean)

    // ------------------------------------------------------------ reads

    suspend fun loadContacts(): LoadResult<List<ContactDto>> = try {
        val fresh = api.getContacts().contacts
        db.cacheDao().replaceContacts(fresh.map { it.toEntity() })
        LoadResult(fresh, fromCache = false)
    } catch (e: CancellationException) {
        throw e
    } catch (e: Exception) {
        Log.w(TAG, "loadContacts offline/failed (${e.javaClass.simpleName}) - serving cache")
        LoadResult(db.cacheDao().contacts().map { it.toDto() }, fromCache = true)
    }

    suspend fun loadPlaces(): LoadResult<List<PlaceDto>> = try {
        val fresh = api.getPlaces().places
        db.cacheDao().replacePlaces(fresh.map { it.toEntity() })
        LoadResult(fresh, fromCache = false)
    } catch (e: CancellationException) {
        throw e
    } catch (e: Exception) {
        Log.w(TAG, "loadPlaces offline/failed (${e.javaClass.simpleName}) - serving cache")
        LoadResult(db.cacheDao().places().map { it.toDto() }, fromCache = true)
    }

    /** Server history (or cache) plus any messages still waiting in the outbox, shown as "sending". */
    suspend fun loadMessages(contactId: String): LoadResult<List<MessageDto>> {
        var fromCache = false
        val server: List<MessageDto> = try {
            val fresh = api.getMessages(contactId).messages
            db.cacheDao().replaceMessages(contactId, fresh.map { it.toEntity(contactId) })
            fresh
        } catch (e: CancellationException) {
            throw e
        } catch (e: Exception) {
            Log.w(TAG, "loadMessages offline/failed (${e.javaClass.simpleName}) - serving cache")
            fromCache = true
            db.cacheDao().messages(contactId).map { it.toDto() }
        }

        val me = session.user
        val pending = db.outboxDao().pendingMessages(contactId).map {
            MessageDto(
                id = "local-${it.clientId}",
                senderId = me?.id.orEmpty(),
                senderName = me?.displayName.orEmpty(),
                body = it.body.orEmpty(),
                createdAt = it.createdAt,
                clientMessageId = it.clientId,
                pending = true
            )
        }
        return LoadResult(MessageMerger.merge(server, pending), fromCache)
    }

    // ----------------------------------------------------------- writes

    suspend fun queueMessage(contactId: String, body: String): SyncManager.FlushResult =
        enqueue(
            OutboxItem(
                type = OutboxItem.TYPE_MESSAGE, contactId = contactId, body = body,
                clientId = UUID.randomUUID().toString(), createdAt = System.currentTimeMillis()
            )
        )

    suspend fun queueLocation(lat: Double, lng: Double, capturedAt: Long = System.currentTimeMillis()): SyncManager.FlushResult =
        enqueue(
            OutboxItem(
                type = OutboxItem.TYPE_LOCATION, lat = lat, lng = lng,
                clientId = UUID.randomUUID().toString(), createdAt = capturedAt
            )
        )

    suspend fun queueSos(lat: Double, lng: Double): SyncManager.FlushResult =
        enqueue(
            OutboxItem(
                type = OutboxItem.TYPE_SOS, lat = lat, lng = lng,
                clientId = UUID.randomUUID().toString(), createdAt = System.currentTimeMillis()
            )
        )

    private suspend fun enqueue(item: OutboxItem): SyncManager.FlushResult {
        db.outboxDao().insert(item)
        if (item.type == OutboxItem.TYPE_LOCATION) db.outboxDao().trimLocations()
        Log.d(TAG, "queued ${item.type} in the outbox")
        val result = SyncManager.flush(appContext)
        if (result.remaining > 0) SyncManager.schedule(appContext)
        return result
    }

    /** Wipes every cached/queued row - called on sign-out so the next person never sees someone else's data. */
    suspend fun clearLocalData() = withContext(Dispatchers.IO) {
        db.clearAllTables()
        Log.d(TAG, "local database cleared")
    }

    companion object {
        private const val TAG = "AppRepository"
        @Volatile private var instance: AppRepository? = null

        fun get(context: Context): AppRepository =
            instance ?: synchronized(this) {
                instance ?: AppRepository(context).also { instance = it }
            }
    }
}
