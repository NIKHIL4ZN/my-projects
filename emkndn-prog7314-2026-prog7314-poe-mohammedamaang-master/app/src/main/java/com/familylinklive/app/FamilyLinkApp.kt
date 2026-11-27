package com.familylinklive.app

import android.app.Application
import android.util.Log
import com.familylinklive.app.push.NotificationHelper
import com.familylinklive.app.sync.NetworkMonitor
import com.google.firebase.FirebaseApp
import org.osmdroid.config.Configuration
import java.io.File

/**
 * Application entry point. Runs once per process, before any screen exists, and sets up
 * everything that must be ready app-wide: Firebase, the map library, notification channels
 * and the connectivity watcher that drives offline sync.
 */
class FamilyLinkApp : Application() {

    /*
    Title: Get Started with Firebase Authentication on Android
    Author(s): Google
    Date: 2024
    Version: Firebase BOM 33.1.2
    Type: Documentation
    Availability: https://firebase.google.com/docs/auth/android/start
    */
    override fun onCreate() {
        super.onCreate()
        Log.i(TAG, "onCreate - process starting (version ${BuildConfig.VERSION_NAME})")
        FirebaseApp.initializeApp(this)

        /*
        Title: How to use the osmdroid library
        Author(s): osmdroid contributors
        Date: 2023
        Version: 6.1.18
        Type: Documentation
        Availability: https://github.com/osmdroid/osmdroid/wiki/How-to-use-the-osmdroid-library
        */
        val osmConfig = Configuration.getInstance()
        val osmPrefs = getSharedPreferences("${packageName}_osmdroid", MODE_PRIVATE)
        osmConfig.load(this, osmPrefs)
        osmConfig.userAgentValue = packageName

        // Use app-private external storage for the tile cache so no
        // WRITE_EXTERNAL_STORAGE permission is needed.
        val basePath = File(getExternalFilesDir(null), "osmdroid")
        osmConfig.osmdroidBasePath = basePath
        osmConfig.osmdroidTileCache = File(basePath, "tiles")

        NotificationHelper.createChannels(this)
        NetworkMonitor.get(this).start()
    }

    private companion object {
        const val TAG = "FamilyLinkApp"
    }
}
