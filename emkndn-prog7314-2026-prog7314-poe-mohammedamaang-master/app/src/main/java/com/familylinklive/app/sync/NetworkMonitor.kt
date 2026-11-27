package com.familylinklive.app.sync

import android.content.Context
import android.net.ConnectivityManager
import android.net.Network
import android.net.NetworkCapabilities
import android.util.Log
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

/**
 * Watches connectivity for the whole app. The MainActivity banner observes
 * [online], and the moment a connection comes back it triggers an
 * immediate outbox flush (WorkManager is the safety net behind it).
 */
class NetworkMonitor private constructor(context: Context) {

    private val appContext = context.applicationContext
    private val connectivity = appContext.getSystemService(ConnectivityManager::class.java)
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    private val _online = MutableStateFlow(isCurrentlyOnline())
    val online: StateFlow<Boolean> = _online.asStateFlow()
    private var started = false

    private fun isCurrentlyOnline(): Boolean {
        val caps = connectivity.getNetworkCapabilities(connectivity.activeNetwork)
        return caps?.hasCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET) == true
    }

    /*
    Title: Monitor connectivity status and connection metering - registerDefaultNetworkCallback
    Author(s): Android Developers
    Date: 2024
    Version: API level 24+
    Type: Documentation
    Availability: https://developer.android.com/training/monitoring-device-state/connectivity-status-type
    */
    fun start() {
        if (started) return
        started = true
        try {
            connectivity.registerDefaultNetworkCallback(object : ConnectivityManager.NetworkCallback() {
                override fun onAvailable(network: Network) {
                    Log.i(TAG, "network available - going online")
                    _online.value = true
                    scope.launch {
                        val result = SyncManager.flush(appContext)
                        if (result.remaining > 0) SyncManager.schedule(appContext)
                    }
                }

                override fun onLost(network: Network) {
                    Log.i(TAG, "network lost - going offline")
                    _online.value = false
                }
            })
        } catch (e: SecurityException) {
            Log.e(TAG, "cannot watch connectivity (ACCESS_NETWORK_STATE missing?)", e)
        }
    }

    companion object {
        private const val TAG = "NetworkMonitor"
        @Volatile private var instance: NetworkMonitor? = null

        fun get(context: Context): NetworkMonitor =
            instance ?: synchronized(this) {
                instance ?: NetworkMonitor(context).also { instance = it }
            }
    }
}
