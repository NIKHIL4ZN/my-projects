package com.familylinklive.app.ui.places

import android.app.AlertDialog
import android.app.Dialog
import android.os.Bundle
import android.util.Log
import androidx.fragment.app.DialogFragment
import androidx.lifecycle.lifecycleScope
import com.familylinklive.app.R
import com.familylinklive.app.data.ApiClient
import com.familylinklive.app.data.models.PlaceDto
import com.familylinklive.app.data.models.PlaceRequest
import com.familylinklive.app.databinding.DialogAddPlaceBinding
import com.familylinklive.app.util.PlaceValidator
import com.familylinklive.app.util.awaitCurrentLocation
import com.familylinklive.app.util.hasFineLocationPermission
import com.google.android.gms.location.LocationServices
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import kotlinx.coroutines.launch
import java.io.IOException

/**
 * User-Defined Feature 2 (part of it): create/edit a Place (REQ-22), including the optional
 * arrival "auto-message" (REQ-23) that User-Defined Feature 4 (geofence alerts) posts into
 * each contact's chat when the user arrives. Shown either as "Add Place" (no arguments) or
 * "Edit Place" (PlaceDto fields passed as primitive Bundle args, avoiding a Parcelable dependency).
 */
class AddEditPlaceDialogFragment : DialogFragment() {

    private var _binding: DialogAddPlaceBinding? = null
    private val binding get() = _binding!!

    private var selectedLat: Double? = null
    private var selectedLng: Double? = null
    private var editingId: String? = null

    private val locationPermissionLauncher = registerForActivityResult(
        androidx.activity.result.contract.ActivityResultContracts.RequestPermission()
    ) { granted -> if (granted) useCurrentLocation() }

    override fun onCreateDialog(savedInstanceState: Bundle?): Dialog {
        _binding = DialogAddPlaceBinding.inflate(layoutInflater)

        editingId = arguments?.getString(ARG_ID)
        val radius = arguments?.getInt(ARG_RADIUS, 300) ?: 300
        val lat = arguments?.getDouble(ARG_LAT)
        val lng = arguments?.getDouble(ARG_LNG)

        binding.etPlaceName.setText(arguments?.getString(ARG_NAME))
        binding.etRadius.setText(radius.toString())
        binding.etAutoMessage.setText(arguments?.getString(ARG_AUTO))
        if (lat != null && lng != null && (lat != 0.0 || lng != 0.0)) {
            selectedLat = lat
            selectedLng = lng
            binding.tvCoordinates.text = getString(R.string.coordinates_format, lat, lng)
        }

        binding.btnUseCurrentLocation.setOnClickListener {
            if (hasFineLocationPermission(requireContext())) useCurrentLocation()
            else locationPermissionLauncher.launch(android.Manifest.permission.ACCESS_FINE_LOCATION)
        }

        /*
        Title: Dialogs - MaterialAlertDialogBuilder, and overriding the
        positive button to validate before dismissing
        Author(s): Google / Material Design
        Date: 2023
        Version: com.google.android.material 1.12.0
        Type: Documentation
        Availability: https://github.com/material-components/material-components-android/blob/master/docs/components/Dialog.md
        This pattern (custom positive-button click listener set via
        setOnShowListener, to keep the dialog open on invalid input) is
        reused identically in ui/contacts/InviteContactDialogFragment.kt.
        */
        val dialog = MaterialAlertDialogBuilder(requireContext())
            .setTitle(if (editingId == null) R.string.add_place else R.string.edit_place)
            .setView(binding.root)
            .setPositiveButton(R.string.save, null)
            .setNegativeButton(R.string.cancel, null)
            .create()

        dialog.setOnShowListener {
            dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener { savePlace() }
        }
        return dialog
    }

    private fun useCurrentLocation() {
        lifecycleScope.launch {
            val location = LocationServices.getFusedLocationProviderClient(requireContext()).awaitCurrentLocation()
            if (location != null && _binding != null) {
                selectedLat = location.latitude
                selectedLng = location.longitude
                binding.tvCoordinates.text = getString(R.string.coordinates_format, location.latitude, location.longitude)
            }
        }
    }

    private fun savePlace() {
        val name = binding.etPlaceName.text?.toString()?.trim().orEmpty()
        val radius = PlaceValidator.coerceRadius(binding.etRadius.text?.toString()?.toIntOrNull() ?: 300)
        val autoMessage = binding.etAutoMessage.text?.toString()?.trim()?.takeIf { it.isNotEmpty() }
        val lat = selectedLat
        val lng = selectedLng

        if (!PlaceValidator.isValidName(name) || lat == null || lng == null) {
            binding.tvCoordinates.text = getString(R.string.place_form_incomplete)
            return
        }

        lifecycleScope.launch {
            try {
                val api = ApiClient.getService(requireContext())
                val request = PlaceRequest(name, lat, lng, radius, autoMessage)
                if (editingId == null) api.createPlace(request) else api.updatePlace(editingId!!, request)
                Log.i(TAG, "place saved (edit=${editingId != null})")
                parentFragmentManager.setFragmentResult("place_saved", Bundle())
                dismiss()
            } catch (e: IOException) {
                _binding?.tvCoordinates?.text = getString(R.string.places_offline)
            } catch (e: Exception) {
                Log.e(TAG, "save place failed", e)
                _binding?.tvCoordinates?.text = getString(R.string.save_failed, e.message)
            }
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }

    companion object {
        private const val TAG = "AddEditPlace"
        private const val ARG_ID = "id"
        private const val ARG_NAME = "name"
        private const val ARG_LAT = "lat"
        private const val ARG_LNG = "lng"
        private const val ARG_RADIUS = "radius"
        private const val ARG_AUTO = "autoMessage"

        fun newInstance(place: PlaceDto?) = AddEditPlaceDialogFragment().apply {
            arguments = Bundle().apply {
                place?.let {
                    putString(ARG_ID, it.id)
                    putString(ARG_NAME, it.name)
                    putDouble(ARG_LAT, it.lat)
                    putDouble(ARG_LNG, it.lng)
                    putInt(ARG_RADIUS, it.radiusM)
                    putString(ARG_AUTO, it.autoMessage)
                }
            }
        }
    }
}
