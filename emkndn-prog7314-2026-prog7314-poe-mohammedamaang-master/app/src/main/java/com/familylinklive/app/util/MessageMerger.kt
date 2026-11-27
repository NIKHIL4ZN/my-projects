package com.familylinklive.app.util

import com.familylinklive.app.data.models.MessageDto

/** Combines what the server knows with what is still waiting in the offline outbox. */
object MessageMerger {
    /**
     * Server messages first (oldest to newest), then still-unsent local ones. A queued message is
     * dropped from the pending list as soon as the server's copy (same clientMessageId) shows up,
     * so a message never appears twice while it changes from "sending" to "sent".
     */
    fun merge(server: List<MessageDto>, pending: List<MessageDto>): List<MessageDto> {
        val serverClientIds = server.mapNotNull { it.clientMessageId }.toSet()
        val stillPending = pending.filter { it.clientMessageId == null || it.clientMessageId !in serverClientIds }
        return server.sortedBy { it.createdAt } + stillPending.sortedBy { it.createdAt }
    }
}
