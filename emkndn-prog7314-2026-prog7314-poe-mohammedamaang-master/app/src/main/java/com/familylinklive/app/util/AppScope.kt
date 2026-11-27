package com.familylinklive.app.util

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob

/**
 * A coroutine scope that outlives any single screen. Used for small "fire and forget" syncs
 * (e.g. telling the server about a language change) that must still finish even though the
 * activity is recreated straight afterwards, which would cancel a lifecycleScope.
 */
object AppScope {
    val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
}
