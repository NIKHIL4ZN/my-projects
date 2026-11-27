package com.familylinklive.app.sync

import android.content.Context
import android.util.Log
import androidx.work.BackoffPolicy
import androidx.work.Constraints
import androidx.work.ExistingWorkPolicy
import androidx.work.NetworkType
import androidx.work.OneTimeWorkRequestBuilder
import androidx.work.WorkManager
import com.familylinklive.app.data.ApiClient
import com.familylinklive.app.data.SessionManager
import com.familylinklive.app.data.local.AppDatabase
import com.familylinklive.app.data.local.OutboxItem
import com.familylinklive.app.data.models.LocationUpdateRequest
import com.familylinklive.app.data.models.SendMessageRequest
import com.familylinklive.app.data.models.SosRequest
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import retrofit2.HttpException
import java.io.IOException
import java.util.concurrent.TimeUnit
import kotlin.coroutines.cancellation.CancellationException

/**
 * Part 3 - Offline mode with synchronisation.
 *
 * flush() sends everything waiting in the Room outbox, oldest first, and
 * stops at the first network failure so the original order is preserved.
 * It is triggered three ways: straight after an action (if online), the
 * moment connectivity returns (NetworkMonitor), and by WorkManager, which
 * retries with back-off even if the app has been closed.
 */
object SyncManager {
    private const val TAG = "SyncManager"
    private const val WORK_NAME = "outbox-sync"

    data class FlushResult(val sent: Int, val remaining: Int, val retryable: Boolean)

    private val mutex = Mutex()

    suspend fun flush(context: Context): FlushResult = mutex.withLock { doFlush(context.applicationContext) }

    private suspend fun doFlush(context: Context): FlushResult {
        val dao = AppDatabase.get(context).outboxDao()
        if (!SessionManager.getInstance(context).isLoggedIn) {
            Log.d(TAG, "flush skipped - nobody is signed in")
            return FlushResult(0, dao.count(), false)
        }

        val api = ApiClient.getService(context)
        val items = dao.all()
        if (items.isEmpty()) return FlushResult(0, 0, false)
        Log.i(TAG, "flush: ${items.size} queued action(s)")

        var sent = 0
        for (item in items) {
            try {
                when (item.type) {
                    OutboxItem.TYPE_MESSAGE ->
                        api.sendMessage(SendMessageRequest(item.contactId!!, item.body!!, item.clientId))
                    OutboxItem.TYPE_LOCATION ->
                        api.updateLocation(LocationUpdateRequest(item.lat!!, item.lng!!, item.createdAt))
                    OutboxItem.TYPE_SOS ->
                        api.triggerSos(SosRequest(item.lat!!, item.lng!!, item.clientId))
                }
                dao.delete(item.id)
                sent++
                Log.d(TAG, "synced ${item.type} #${item.id}")
            } catch (e: CancellationException) {
                throw e
            } catch (e: IOException) {
                Log.w(TAG, "network unavailable - ${items.size - sent} left in outbox (${e.javaClass.simpleName})")
                return FlushResult(sent, dao.count(), true)
            } catch (e: HttpException) {
                if (SyncPolicy.isRetryable(e.code())) {
                    Log.w(TAG, "server answered ${e.code()} - will retry later")
                    return FlushResult(sent, dao.count(), true)
                }
                Log.e(TAG, "server rejected ${item.type} #${item.id} with ${e.code()} - dropping it")
                dao.delete(item.id)
            }
        }
        return FlushResult(sent, dao.count(), false)
    }

    /*
    Title: Define work requests - constraints and back-off (WorkManager)
    Author(s): Android Developers
    Date: 2024
    Version: WorkManager 2.9.0
    Type: Documentation
    Availability: https://developer.android.com/develop/background-work/background-tasks/persistent/getting-started/define-work
    */
    fun schedule(context: Context) {
        val request = OneTimeWorkRequestBuilder<SyncWorker>()
            .setConstraints(Constraints.Builder().setRequiredNetworkType(NetworkType.CONNECTED).build())
            .setBackoffCriteria(BackoffPolicy.EXPONENTIAL, 15, TimeUnit.SECONDS)
            .build()
        WorkManager.getInstance(context.applicationContext)
            .enqueueUniqueWork(WORK_NAME, ExistingWorkPolicy.KEEP, request)
        Log.d(TAG, "WorkManager sync scheduled (runs as soon as a connection exists)")
    }
}
