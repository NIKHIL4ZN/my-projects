package com.familylinklive.app.ui.places

import android.os.Bundle
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.familylinklive.app.R
import com.familylinklive.app.data.ApiClient
import com.familylinklive.app.data.AppRepository
import com.familylinklive.app.data.models.PlaceDto
import com.familylinklive.app.databinding.FragmentPlacesBinding
import kotlinx.coroutines.launch
import java.io.IOException

/**
 * User-Defined Feature 2: Places (Geofence) management.
 * List (GET /places, cached for offline reading), create/edit (via AddEditPlaceDialogFragment)
 * and delete (DELETE /places/{id}) - REQ-22 and REQ-23.
 *
 * Part 3: the list is served from the Room cache when offline. Changing places needs the server
 * (the server owns the geofence maths), so edits are blocked with a clear message while offline.
 */
class PlacesFragment : Fragment() {

    private var _binding: FragmentPlacesBinding? = null
    private val binding get() = _binding!!
    private lateinit var adapter: PlacesAdapter

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?
    ): View {
        _binding = FragmentPlacesBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        Log.d(TAG, "onViewCreated")

        adapter = PlacesAdapter(
            onEdit = { place ->
                AddEditPlaceDialogFragment.newInstance(place).show(childFragmentManager, "edit_place")
            },
            onDelete = { place -> deletePlace(place) }
        )
        binding.recyclerPlaces.layoutManager = LinearLayoutManager(requireContext())
        binding.recyclerPlaces.adapter = adapter

        binding.swipeRefresh.setOnRefreshListener { loadPlaces() }
        binding.fabAddPlace.setOnClickListener {
            AddEditPlaceDialogFragment.newInstance(null).show(childFragmentManager, "add_place")
        }

        /*
        Title: Communicate with fragments - the Fragment Result API
        Author(s): Android Developers
        Date: 2023
        Version: androidx.fragment 1.8.2
        Type: Documentation
        Availability: https://developer.android.com/guide/fragments/communicate#fragment-result
        This DialogFragment-to-Fragment result pattern is reused identically
        by ui/contacts/ContactsFragment.kt, InviteContactDialogFragment.kt and
        ui/map/MapFragment.kt (SOS countdown).
        */
        childFragmentManager.setFragmentResultListener("place_saved", viewLifecycleOwner) { _, _ ->
            loadPlaces()
        }

        loadPlaces()
    }

    private fun loadPlaces() {
        binding.swipeRefresh.isRefreshing = true
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                val result = AppRepository.get(requireContext()).loadPlaces()
                adapter.submitList(result.data)
                _binding?.emptyState?.visibility = if (result.data.isEmpty()) View.VISIBLE else View.GONE
                Log.d(TAG, "loaded ${result.data.size} places (fromCache=${result.fromCache})")
            } catch (e: Exception) {
                Log.e(TAG, "loadPlaces failed", e)
                Toast.makeText(requireContext(), getString(R.string.places_load_failed, e.message), Toast.LENGTH_SHORT).show()
            } finally {
                _binding?.swipeRefresh?.isRefreshing = false
            }
        }
    }

    private fun deletePlace(place: PlaceDto) {
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                ApiClient.getService(requireContext()).deletePlace(place.id)
                Log.i(TAG, "deleted place ${place.id}")
                loadPlaces()
            } catch (e: IOException) {
                Toast.makeText(requireContext(), R.string.places_offline, Toast.LENGTH_SHORT).show()
            } catch (e: Exception) {
                Log.e(TAG, "delete failed", e)
                Toast.makeText(requireContext(), getString(R.string.place_delete_failed, e.message), Toast.LENGTH_SHORT).show()
            }
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }

    private companion object {
        const val TAG = "PlacesFragment"
    }
}
