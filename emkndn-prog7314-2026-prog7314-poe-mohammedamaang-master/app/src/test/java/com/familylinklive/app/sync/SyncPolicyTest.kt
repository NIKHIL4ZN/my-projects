package com.familylinklive.app.sync

import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/** Which server answers make the offline outbox keep an item and retry, versus drop it. */
class SyncPolicyTest {

    @Test
    fun `temporary problems are retried`() {
        listOf(401, 408, 429, 500, 502, 503, 504).forEach { code ->
            assertTrue("HTTP $code should be retried", SyncPolicy.isRetryable(code))
        }
    }

    @Test
    fun `permanent client errors are not retried`() {
        listOf(400, 403, 404, 409, 422).forEach { code ->
            assertFalse("HTTP $code should be dropped", SyncPolicy.isRetryable(code))
        }
    }
}
