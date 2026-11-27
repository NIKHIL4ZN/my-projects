package com.familylinklive.app.data.local

import android.content.Context
import androidx.room.Database
import androidx.room.Room
import androidx.room.RoomDatabase

/*
Title: Save data in a local database using Room
Author(s): Android Developers
Date: 2024
Version: Room 2.6.1
Type: Documentation
Availability: https://developer.android.com/training/data-storage/room
*/
@Database(
    entities = [OutboxItem::class, CachedContact::class, CachedMessage::class, CachedPlace::class],
    version = 1,
    exportSchema = false
)
abstract class AppDatabase : RoomDatabase() {
    abstract fun outboxDao(): OutboxDao
    abstract fun cacheDao(): CacheDao

    companion object {
        @Volatile private var instance: AppDatabase? = null

        fun get(context: Context): AppDatabase =
            instance ?: synchronized(this) {
                instance ?: Room.databaseBuilder(context.applicationContext, AppDatabase::class.java, "familylink.db")
                    .fallbackToDestructiveMigration()
                    .build()
                    .also { instance = it }
            }
    }
}
