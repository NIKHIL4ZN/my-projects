package com.familylinklive.app.sync

/** Decides what the outbox does when the server answers with an HTTP error. */
object SyncPolicy {
    /**
     * Temporary problems are retried later: 401 (the silent re-login may succeed next time),
     * 408 (timeout), 429 (rate limited) and any 5xx (server error / Render still waking up).
     * Other 4xx answers (e.g. 403 "not a contact any more") will never succeed, so the item is
     * dropped instead of blocking everything queued behind it forever.
     */
    fun isRetryable(httpCode: Int): Boolean =
        httpCode == 401 || httpCode == 408 || httpCode == 429 || httpCode >= 500
}
