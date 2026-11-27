package com.familylinklive.app.ui.map

import android.Manifest
import android.os.Bundle
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.core.content.ContextCompat
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import com.familylinklive.app.R
import com.familylinklive.app.data.ApiClient
import com.familylinklive.app.data.AppRepository
import com.familylinklive.app.data.SessionManager
import com.familylinklive.app.databinding.FragmentMapBinding
import com.familylinklive.app.ui.sos.SosCountdownDialogFragment
import com.familylinklive.app.util.awaitCurrentLocation
import com.familylinklive.app.util.awaitLastLocation
import com.familylinklive.app.util.hasFineLocationPermission
import com.google.android.gms.location.FusedLocationProviderClient
import com.google.android.gms.location.LocationServices
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import org.osmdroid.tileprovider.tilesource.TileSourceFactory
import org.osmdroid.util.GeoPoint
import org.osmdroid.views.overlay.Marker

/**
 * User-Defined Feature 1: Live Map (contacts only) - plus, in Part 3, the entry point for
 * User-Defined Feature 5 (Emergency SOS).
 *
 * REQ-21: renders each ACCEPTED contact's most recent location as a pin, refreshed every 15 s
 * while this screen is visible. Sharing your own location goes through the offline outbox, so it
 * works (and is queued) even with no connection.
 *
 * Map rendering uses osmdroid (OpenStreetMap tiles): free, no API key, no billing. Device location
 * comes from Google Play Services' Fused Location Provider, which is a separate, keyless API.
 */
class MapFragment : Fragment() {

    private var _binding: FragmentMapBinding? = null
    private val binding get() = _binding!!

    private lateinit var fusedLocationClient: FusedLocationProviderClient
    private lateinit var session: SessionManager
    private lateinit var repo: AppRepository
    private var refreshLoopStarted = false
    private var focusMarker: Marker? = null
    private var afterPermissionGranted: (() -> Unit)? = null

    private val locationPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { granted ->
        Log.i(TAG, "location permission granted=$granted")
        if (granted) afterPermissionGranted?.invoke()
        else Toast.makeText(requireContext(), R.string.location_permission_required, Toast.LENGTH_SHORT).show()
        afterPermissionGranted = null
    }

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?
    ): View {
        _binding = FragmentMapBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        Log.d(TAG, "onViewCreated")
        session = SessionManager.getInstance(requireContext())
        repo = AppRepository.get(requireContext())
        fusedLocationClient = LocationServices.getFusedLocationProviderClient(requireContext())

        /*
        Title: How to use the osmdroid library - creating and configuring a
        MapView
        Author(s): osmdroid contributors
        Date: 2023
        Version: 6.1.18
        Type: Documentation
        Availability: https://github.com/osmdroid/osmdroid/wiki/How-to-use-the-osmdroid-library
        */
        binding.map.setTileSource(TileSourceFactory.MAPNIK)
        binding.map.setMultiTouchControls(true)
        binding.map.controller.setZoom(12.0)
        // Sensible default centre (roughly Johannesburg) until we have a
        // real location; recentres automatically once the user shares theirs.
        binding.map.controller.setCenter(GeoPoint(-26.2041, 28.0473))

        binding.fabShareLocation.setOnClickListener { withLocationPermission { shareCurrentLocation() } }

        // SOS needs a deliberate press-and-hold, then a 5 s cancellable countdown.
        binding.btnSos.setOnClickListener {
            Toast.makeText(requireContext(), R.string.sos_hint, Toast.LENGTH_SHORT).show()
        }
        binding.btnSos.setOnLongClickListener {
            Log.i(TAG, "SOS button held - starting countdown")
            withLocationPermission {
                SosCountdownDialogFragment().show(childFragmentManager, "sos_countdown")
            }
            true
        }
        childFragmentManager.setFragmentResultListener(SosCountdownDialogFragment.RESULT_KEY, viewLifecycleOwner) { _, _ ->
            sendSos()
        }

        startContactRefreshLoop()
    }

    override fun onResume() {
        super.onResume()
        _binding?.map?.onResume()
        MapFocus.consume()?.let { focusOn(it) }
    }

    override fun onPause() {
        super.onPause()
        _binding?.map?.onPause()
    }

    private fun withLocationPermission(action: () -> Unit) {
        if (hasFineLocationPermission(requireContext())) {
            action()
        } else {
            afterPermissionGranted = action
            locationPermissionLauncher.launch(Manifest.permission.ACCESS_FINE_LOCATION)
        }
    }

    /*
    Title: Get the current location - FusedLocationProviderClient.getCurrentLocation
    Author(s): Android Developers
    Date: 2024
    Version: play-services-location 21.3.0
    Type: Documentation
    Availability: https://developer.android.com/develop/sensors-and-location/location/retrieve-current
    */
    private fun shareCurrentLocation() {
        if (!session.shareLocationEnabled) {
            Toast.makeText(requireContext(), R.string.location_sharing_off, Toast.LENGTH_SHORT).show()
            return
        }
        binding.fabShareLocation.isEnabled = false
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                val location = fusedLocationClient.awaitCurrentLocation()
                if (location == null) {
                    Toast.makeText(requireContext(), R.string.location_unavailable, Toast.LENGTH_SHORT).show()
                    return@launch
                }
                val result = repo.queueLocation(location.latitude, location.longitude)
                _binding?.map?.controller?.animateTo(GeoPoint(location.latitude, location.longitude))
                _binding?.map?.controller?.setZoom(14.0)
                val messageRes = if (result.remaining == 0) R.string.location_shared else R.string.location_queued
                Toast.makeText(requireContext(), messageRes, Toast.LENGTH_SHORT).show()
                refreshContacts()
            } catch (e: Exception) {
                Log.e(TAG, "sharing location failed", e)
                Toast.makeText(requireContext(), getString(R.string.sync_failed, e.message), Toast.LENGTH_SHORT).show()
            } finally {
                _binding?.fabShareLocation?.isEnabled = true
            }
        }
    }

    /** SOS countdown finished: get the best position available and send it (queued if offline). */
    private fun sendSos() {
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                // A stale fix is far better than no SOS at all, so fall back to the cached location.
                val location = fusedLocationClient.awaitCurrentLocation() ?: fusedLocationClient.awaitLastLocation()
                if (location == null) {
                    Log.w(TAG, "SOS aborted - no location available")
                    Toast.makeText(requireContext(), R.string.sos_no_location, Toast.LENGTH_LONG).show()
                    return@launch
                }
                val result = repo.queueSos(location.latitude, location.longitude)
                Log.i(TAG, "SOS queued/sent, remaining in outbox=${result.remaining}")
                val messageRes = if (result.remaining == 0) R.string.sos_sent else R.string.sos_queued
                Toast.makeText(requireContext(), messageRes, Toast.LENGTH_LONG).show()
            } catch (e: Exception) {
                Log.e(TAG, "SOS failed", e)
                Toast.makeText(requireContext(), getString(R.string.sync_failed, e.message), Toast.LENGTH_LONG).show()
            }
        }
    }

    private fun startContactRefreshLoop() {
        if (refreshLoopStarted) return
        refreshLoopStarted = true
        viewLifecycleOwner.lifecycleScope.launch {
            while (isAdded) {
                if (session.shareLocationEnabled) refreshContacts()
                delay(15_000L)
            }
        }
    }

    private suspend fun refreshContacts() {
        try {
            val api = ApiClient.getService(requireContext())
            val response = api.getContactLocations()

            val map = _binding?.map ?: return
            map.overlays.clear()
            response.contacts.forEach { contact ->
                val marker = Marker(map)
                marker.position = GeoPoint(contact.lat, contact.lng)
                marker.title = contact.displayName
                marker.setAnchor(Marker.ANCHOR_CENTER, Marker.ANCHOR_BOTTOM)
                map.overlays.add(marker)
            }
            focusMarker?.let { map.overlays.add(it) }
            map.invalidate()
        } catch (e: Exception) {
            // Offline or a transient error: keep showing the pins we already have; the next cycle retries.
            Log.d(TAG, "contact refresh skipped (${e.javaClass.simpleName})")
        }
    }

    /** Opened from an SOS / geofence notification or an Activity row: centre on that point and mark it. */
    private fun focusOn(request: MapFocus.Request) {
        val map = _binding?.map ?: return
        val point = GeoPoint(request.lat, request.lng)
        focusMarker?.let { map.overlays.remove(it) }
        val marker = Marker(map).apply {
            position = point
            title = when {
                request.sos -> getString(R.string.sos_marker_label, request.label.orEmpty())
                else -> request.label
            }
            if (request.sos) icon = ContextCompat.getDrawable(requireContext(), R.drawable.ic_sos_marker)
            setAnchor(Marker.ANCHOR_CENTER, Marker.ANCHOR_BOTTOM)
        }
        focusMarker = marker
        map.overlays.add(marker)
        map.controller.setZoom(16.0)
        map.controller.animateTo(point)
        marker.showInfoWindow()
        map.invalidate()
        Log.i(TAG, "map focused on notification point (sos=${request.sos})")
    }

    override fun onDestroyView() {
        super.onDestroyView()
        // The polling loop and the marker both belong to the view that is going away.
        refreshLoopStarted = false
        focusMarker = null
        _binding = null
    }

    private companion object {
        const val TAG = "MapFragment"
    }
}
