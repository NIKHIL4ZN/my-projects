package com.familylinklive.app.ui.contacts

import android.os.Bundle
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.core.os.bundleOf
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.navigation.fragment.findNavController
import androidx.recyclerview.widget.LinearLayoutManager
import com.familylinklive.app.R
import com.familylinklive.app.data.ApiClient
import com.familylinklive.app.data.AppRepository
import com.familylinklive.app.data.models.ContactDto
import com.familylinklive.app.databinding.FragmentContactsBinding
import kotlinx.coroutines.launch
import java.io.IOException

/**
 * Contacts hub: lists active contacts, incoming requests (Accept/Decline),
 * and outgoing pending invites, and is the entry point into a per-contact
 * chat thread (REQ-9 / Part 1 design Section 4.1.4).
 *
 * Part 3: the list is cached in Room so it still shows offline; invites, accepts and removals
 * need the server and say so clearly when there's no connection.
 */
class ContactsFragment : Fragment() {

    private var _binding: FragmentContactsBinding? = null
    private val binding get() = _binding!!
    private lateinit var adapter: ContactsAdapter

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?
    ): View {
        _binding = FragmentContactsBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        Log.d(TAG, "onViewCreated")

        adapter = ContactsAdapter(
            onOpenChat = { contact -> openChatThread(contact) },
            onAccept = { contact -> acceptContact(contact) },
            onRemove = { contact -> removeContact(contact) }
        )
        binding.recyclerContacts.layoutManager = LinearLayoutManager(requireContext())
        binding.recyclerContacts.adapter = adapter

        binding.swipeRefresh.setOnRefreshListener { loadContacts() }
        binding.fabInvite.setOnClickListener {
            InviteContactDialogFragment().show(childFragmentManager, "invite_contact")
        }

        // Fragment Result API pattern - see the reference comment in
        // ui/places/PlacesFragment.kt, where this pattern is used first.
        childFragmentManager.setFragmentResultListener("contact_changed", viewLifecycleOwner) { _, _ ->
            loadContacts()
        }

        loadContacts()
    }

    override fun onResume() {
        super.onResume()
        // Pick up anything that changed while the user was on the Chat
        // thread screen or another tab (e.g. an invite that got accepted).
        if (_binding != null) loadContacts()
    }

    private fun loadContacts() {
        binding.swipeRefresh.isRefreshing = true
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                val result = AppRepository.get(requireContext()).loadContacts()
                adapter.submitList(result.data)
                _binding?.emptyState?.visibility = if (result.data.isEmpty()) View.VISIBLE else View.GONE
                Log.d(TAG, "loaded ${result.data.size} contacts (fromCache=${result.fromCache})")
            } catch (e: Exception) {
                Log.e(TAG, "loadContacts failed", e)
                Toast.makeText(requireContext(), getString(R.string.contacts_load_failed, e.message), Toast.LENGTH_SHORT).show()
            } finally {
                _binding?.swipeRefresh?.isRefreshing = false
            }
        }
    }

    private fun acceptContact(contact: ContactDto) {
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                ApiClient.getService(requireContext()).acceptContact(contact.id)
                Log.i(TAG, "accepted contact ${contact.id}")
                Toast.makeText(requireContext(), R.string.contact_accepted, Toast.LENGTH_SHORT).show()
                loadContacts()
            } catch (e: IOException) {
                Toast.makeText(requireContext(), R.string.contacts_offline, Toast.LENGTH_SHORT).show()
            } catch (e: Exception) {
                Log.e(TAG, "accept failed", e)
                Toast.makeText(requireContext(), getString(R.string.contact_accept_failed, e.message), Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun removeContact(contact: ContactDto) {
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                ApiClient.getService(requireContext()).removeContact(contact.id)
                Log.i(TAG, "removed contact ${contact.id}")
                Toast.makeText(requireContext(), R.string.contact_removed, Toast.LENGTH_SHORT).show()
                loadContacts()
            } catch (e: IOException) {
                Toast.makeText(requireContext(), R.string.contacts_offline, Toast.LENGTH_SHORT).show()
            } catch (e: Exception) {
                Log.e(TAG, "remove failed", e)
                Toast.makeText(requireContext(), getString(R.string.contact_remove_failed, e.message), Toast.LENGTH_SHORT).show()
            }
        }
    }

    /*
    Title: Pass data between destinations - navigating with a Bundle of
    arguments (used here without the Safe Args Gradle plugin)
    Author(s): Android Developers
    Date: 2023
    Version: Navigation 2.7.7
    Type: Documentation
    Availability: https://developer.android.com/guide/navigation/navigation-pass-data
    */
    private fun openChatThread(contact: ContactDto) {
        val args = bundleOf(
            "contactId" to contact.id,
            "contactName" to contact.otherUserDisplayName
        )
        findNavController().navigate(R.id.action_contacts_to_chatThread, args)
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }

    private companion object {
        const val TAG = "ContactsFragment"
    }
}
