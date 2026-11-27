package com.familylinklive.app.ui.activity

import android.os.Bundle
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.fragment.app.Fragment
import androidx.recyclerview.widget.LinearLayoutManager
import com.familylinklive.app.R
import com.familylinklive.app.databinding.FragmentActivityBinding
import com.familylinklive.app.ui.main.MainActivity
import com.familylinklive.app.ui.map.MapFocus
import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.firestore.FirebaseFirestore
import com.google.firebase.firestore.ListenerRegistration
import com.google.firebase.firestore.Query

/**
 * Part 3 - NoSQL database READ side.
 *
 * Shows the alert history stored in Cloud Firestore (a NoSQL document database): arrivals, departures,
 * SOS alerts and contact requests. The backend WRITES these documents (see ActivityFeed.kt on the
 * server); this screen READS them directly with a real-time snapshot listener, so a new alert appears
 * here the instant it is written, with no polling. Firestore also keeps an on-device cache, so the
 * history stays visible offline (the yellow "saved activity" strip tells you when that's the case).
 *
 * Firestore security rules only let a user read their own users/{uid}/activity collection.
 */
class ActivityFragment : Fragment() {

    private var _binding: FragmentActivityBinding? = null
    private val binding get() = _binding!!
    private lateinit var adapter: ActivityAdapter
    private var registration: ListenerRegistration? = null

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?
    ): View {
        _binding = FragmentActivityBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        Log.d(TAG, "onViewCreated")

        adapter = ActivityAdapter { item -> onItemClicked(item) }
        binding.recyclerActivity.layoutManager = LinearLayoutManager(requireContext())
        binding.recyclerActivity.adapter = adapter
    }

    override fun onStart() {
        super.onStart()
        startListening()
    }

    override fun onStop() {
        registration?.remove()
        registration = null
        super.onStop()
    }

    /*
    Title: Get realtime updates with Cloud Firestore - snapshot listeners
    Author(s): Google
    Date: 2024
    Version: firebase-firestore (BoM 33.1.2)
    Type: Documentation
    Availability: https://firebase.google.com/docs/firestore/query-data/listen
    */
    private fun startListening() {
        val uid = FirebaseAuth.getInstance().currentUser?.uid
        if (uid == null) {
            Log.w(TAG, "no Firebase user - cannot read the activity feed")
            Toast.makeText(requireContext(), R.string.activity_signin_required, Toast.LENGTH_LONG).show()
            return
        }

        registration?.remove()
        registration = FirebaseFirestore.getInstance()
            .collection("users").document(uid).collection("activity")
            .orderBy("createdAt", Query.Direction.DESCENDING)
            .limit(50)
            .addSnapshotListener { snapshot, error ->
                val b = _binding ?: return@addSnapshotListener
                if (error != null) {
                    Log.w(TAG, "Firestore listener error: ${error.code}")
                    Toast.makeText(requireContext(), getString(R.string.activity_load_failed, error.message), Toast.LENGTH_SHORT).show()
                    return@addSnapshotListener
                }
                if (snapshot == null) return@addSnapshotListener

                val items = snapshot.documents.map { doc ->
                    ActivityItem(
                        id = doc.id,
                        type = doc.getString("type").orEmpty(),
                        actorName = doc.getString("actorName").orEmpty(),
                        placeName = doc.getString("placeName"),
                        lat = doc.getDouble("lat"),
                        lng = doc.getDouble("lng"),
                        createdAt = doc.getLong("createdAt") ?: 0L
                    )
                }
                Log.d(TAG, "snapshot: ${items.size} items, fromCache=${snapshot.metadata.isFromCache}")
                adapter.submitList(items)
                b.emptyState.visibility = if (items.isEmpty()) View.VISIBLE else View.GONE
                b.tvCacheHint.visibility = if (snapshot.metadata.isFromCache && items.isNotEmpty()) View.VISIBLE else View.GONE
            }
    }

    private fun onItemClicked(item: ActivityItem) {
        val lat = item.lat
        val lng = item.lng
        if (lat != null && lng != null) {
            MapFocus.request(lat, lng, item.placeName ?: item.actorName, sos = item.type == "SOS")
            (activity as? MainActivity)?.selectTab(R.id.mapFragment)
        }
    }

    override fun onDestroyView() {
        registration?.remove()
        registration = null
        _binding = null
        super.onDestroyView()
    }

    private companion object {
        const val TAG = "ActivityFragment"
    }
}
