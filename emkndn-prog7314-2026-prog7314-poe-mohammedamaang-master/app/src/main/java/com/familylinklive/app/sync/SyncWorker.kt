package com.familylinklive.app.sync

import android.content.Context
import android.util.Log
import androidx.work.CoroutineWorker
import androidx.work.WorkerParameters

/** Background job that drains the offline outbox once the device is online again. */
class SyncWorker(context: Context, params: WorkerParameters) : CoroutineWorker(context, params) {

    override suspend fun doWork(): Result {
        val result = SyncManager.flush(applicationContext)
        Log.i(TAG, "worker finished: sent=${result.sent} remaining=${result.remaining} retryable=${result.retryable}")
        return if (result.remaining > 0 && result.retryable) Result.retry() else Result.success()
    }

    private companion object {
        const val TAG = "SyncWorker"
    }
}
