package com.familylinklive.app.data.local

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import androidx.room.Transaction
import kotlinx.coroutines.flow.Flow

@Dao
interface OutboxDao {
    @Insert
    suspend fun insert(item: OutboxItem): Long

    /** Oldest first, so queued actions reach the server in the order the user performed them. */
    @Query("SELECT * FROM outbox ORDER BY createdAt ASC, id ASC")
    suspend fun all(): List<OutboxItem>

    @Query("SELECT * FROM outbox WHERE type = 'MESSAGE' AND contactId = :contactId ORDER BY createdAt ASC, id ASC")
    suspend fun pendingMessages(contactId: String): List<OutboxItem>

    @Query("DELETE FROM outbox WHERE id = :id")
    suspend fun delete(id: Long)

    @Query("SELECT COUNT(*) FROM outbox")
    suspend fun count(): Int

    /** Live count that drives the "N waiting to sync" banner. */
    @Query("SELECT COUNT(*) FROM outbox")
    fun countFlow(): Flow<Int>

    /** A long offline stretch must not grow the queue forever: keep only the newest 200 location pings. */
    @Query(
        "DELETE FROM outbox WHERE type = 'LOCATION' AND id NOT IN " +
            "(SELECT id FROM outbox WHERE type = 'LOCATION' ORDER BY createdAt DESC LIMIT 200)"
    )
    suspend fun trimLocations()
}

@Dao
abstract class CacheDao {

    @Query("SELECT * FROM cached_contacts ORDER BY otherUserDisplayName ASC")
    abstract suspend fun contacts(): List<CachedContact>

    @Query("DELETE FROM cached_contacts")
    abstract suspend fun clearContacts()

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    abstract suspend fun insertContacts(items: List<CachedContact>)

    @Transaction
    open suspend fun replaceContacts(items: List<CachedContact>) {
        clearContacts()
        insertContacts(items)
    }

    @Query("SELECT * FROM cached_messages WHERE contactId = :contactId ORDER BY createdAt ASC")
    abstract suspend fun messages(contactId: String): List<CachedMessage>

    @Query("DELETE FROM cached_messages WHERE contactId = :contactId")
    abstract suspend fun clearMessages(contactId: String)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    abstract suspend fun insertMessages(items: List<CachedMessage>)

    @Transaction
    open suspend fun replaceMessages(contactId: String, items: List<CachedMessage>) {
        clearMessages(contactId)
        insertMessages(items)
    }

    @Query("SELECT * FROM cached_places ORDER BY name ASC")
    abstract suspend fun places(): List<CachedPlace>

    @Query("DELETE FROM cached_places")
    abstract suspend fun clearPlaces()

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    abstract suspend fun insertPlaces(items: List<CachedPlace>)

    @Transaction
    open suspend fun replacePlaces(items: List<CachedPlace>) {
        clearPlaces()
        insertPlaces(items)
    }
}
