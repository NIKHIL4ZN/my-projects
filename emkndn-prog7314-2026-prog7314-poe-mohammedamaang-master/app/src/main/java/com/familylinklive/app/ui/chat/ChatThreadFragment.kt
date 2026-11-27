package com.familylinklive.app.ui.chat

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
import com.familylinklive.app.data.AppRepository
import com.familylinklive.app.data.SessionManager
import com.familylinklive.app.databinding.FragmentChatThreadBinding
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

/**
 * User-Defined Feature 3: private, per-contact chat thread
 * (POST /chat/send, GET /chat/messages?contactId=...). Reached from
 * ContactsFragment by tapping an ACTIVE contact, or straight from a push notification;
 * contactId and contactName are plain Bundle arguments (see nav_graph.xml).
 *
 * Part 3 - Offline mode: sending never fails on a dead connection. The message is saved to the
 * Room outbox and appears immediately with "Sending…"; it is delivered by the sync engine as soon
 * as a connection exists, and the server ignores duplicates (idempotent clientMessageId).
 * History is cached, so previous messages stay readable offline.
 */
class ChatThreadFragment : Fragment() {

    private var _binding: FragmentChatThreadBinding? = null
    private val binding get() = _binding!!
    private lateinit var adapter: ChatAdapter
    private lateinit var session: SessionManager
    private lateinit var repo: AppRepository
    private lateinit var contactId: String

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?
    ): View {
        _binding = FragmentChatThreadBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        session = SessionManager.getInstance(requireContext())
        repo = AppRepository.get(requireContext())

        contactId = requireArguments().getString("contactId")
            ?: error("ChatThreadFragment requires a contactId argument")
        val contactName = requireArguments().getString("contactName").orEmpty()
        binding.tvContactName.text = getString(R.string.chatting_with, contactName)
        Log.d(TAG, "onViewCreated contact=$contactId")

        adapter = ChatAdapter { session.user?.id }
        binding.recyclerMessages.layoutManager =
            LinearLayoutManager(requireContext()).apply { stackFromEnd = true }
        binding.recyclerMessages.adapter = adapter

        binding.btnSend.setOnClickListener { sendMessage() }

        startPolling()
    }

    private fun startPolling() {
        viewLifecycleOwner.lifecycleScope.launch {
            while (isAdded) {
                loadMessages()
                delay(5_000L)
            }
        }
    }

    private suspend fun loadMessages() {
        try {
            val result = repo.loadMessages(contactId)
            val list = result.data
            _binding?.let { b ->
                adapter.submitList(list) {
                    if (adapter.itemCount > 0) b.recyclerMessages.scrollToPosition(adapter.itemCount - 1)
                }
            }
        } catch (e: Exception) {
            Log.d(TAG, "loadMessages skipped (${e.javaClass.simpleName})")
        }
    }

    private fun sendMessage() {
        val text = binding.etMessage.text?.toString()?.trim().orEmpty()
        if (text.isEmpty()) return
        binding.etMessage.text?.clear()

        viewLifecycleOwner.lifecycleScope.launch {
            try {
                val result = repo.queueMessage(contactId, text)
                Log.i(TAG, "message queued; outbox remaining=${result.remaining}")
                loadMessages() // shows the new bubble straight away, "Sending…" if it is still queued
            } catch (e: Exception) {
                Log.e(TAG, "queueing the message failed", e)
                Toast.makeText(requireContext(), getString(R.string.send_failed, e.message), Toast.LENGTH_SHORT).show()
            }
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }

    private companion object {
        const val TAG = "ChatThread"
    }
}
