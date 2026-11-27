package com.familylinklive.app.util

import com.familylinklive.app.data.models.MessageDto
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/** Offline-mode logic: how server messages and still-queued messages are combined for the chat screen. */
class MessageMergerTest {

    private fun server(id: String, at: Long, clientId: String? = null) =
        MessageDto(id, "u1", "Sam", "msg-$id", at, clientId, pending = false)

    private fun pending(clientId: String, at: Long) =
        MessageDto("local-$clientId", "me", "Me", "pending-$clientId", at, clientId, pending = true)

    @Test
    fun `server messages come first in time order, pending ones last`() {
        val merged = MessageMerger.merge(
            server = listOf(server("b", 200), server("a", 100)),
            pending = listOf(pending("c1", 300))
        )
        assertEquals(listOf("a", "b", "local-c1"), merged.map { it.id })
    }

    @Test
    fun `a pending message disappears once the server has its copy`() {
        val merged = MessageMerger.merge(
            server = listOf(server("s1", 100, clientId = "c1")),
            pending = listOf(pending("c1", 100))
        )
        assertEquals(1, merged.size)
        assertTrue(!merged.single().pending)
    }

    @Test
    fun `only the matching pending message is removed`() {
        val merged = MessageMerger.merge(
            server = listOf(server("s1", 100, clientId = "c1")),
            pending = listOf(pending("c1", 100), pending("c2", 150))
        )
        assertEquals(listOf("s1", "local-c2"), merged.map { it.id })
    }

    @Test
    fun `pending messages stay in the order they were written`() {
        val merged = MessageMerger.merge(emptyList(), listOf(pending("c2", 200), pending("c1", 100)))
        assertEquals(listOf("local-c1", "local-c2"), merged.map { it.id })
    }

    @Test
    fun `empty inputs give an empty list`() {
        assertTrue(MessageMerger.merge(emptyList(), emptyList()).isEmpty())
    }
}
