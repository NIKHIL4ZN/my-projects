package com.familylinklive.app.ui.contacts

import android.app.AlertDialog
import android.app.Dialog
import android.os.Bundle
import android.util.Log
import android.view.View
import android.widget.Toast
import androidx.fragment.app.DialogFragment
import androidx.lifecycle.lifecycleScope
import com.familylinklive.app.R
import com.familylinklive.app.data.ApiClient
import com.familylinklive.app.data.models.InviteRequest
import com.familylinklive.app.databinding.DialogInviteContactBinding
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import kotlinx.coroutines.launch
import org.json.JSONObject

/**
 * Invite a contact by email (POST /contacts/invite). Shows a specific,
 * readable message for the two expected failure cases - unknown email
 * (404) and duplicate request (409) - rather than a generic error toast.
 */
class InviteContactDialogFragment : DialogFragment() {

    private var _binding: DialogInviteContactBinding? = null
    private val binding get() = _binding!!

    override fun onCreateDialog(savedInstanceState: Bundle?): Dialog {
        _binding = DialogInviteContactBinding.inflate(layoutInflater)

        // MaterialAlertDialogBuilder + custom positive-button listener
        // pattern - see the reference comment in
        // ui/places/AddEditPlaceDialogFragment.kt, where it's used first.
        val dialog = MaterialAlertDialogBuilder(requireContext())
            .setTitle(R.string.invite_contact)
            .setView(binding.root)
            .setPositiveButton(R.string.send, null)
            .setNegativeButton(R.string.cancel, null)
            .create()

        dialog.setOnShowListener {
            dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener { sendInvite() }
        }
        return dialog
    }

    private fun sendInvite() {
        val email = binding.etEmail.text?.toString()?.trim().orEmpty()
        if (email.isEmpty()) {
            showInlineError(getString(R.string.email_address))
            return
        }

        lifecycleScope.launch {
            try {
                val api = ApiClient.getService(requireContext())
                val response = api.inviteContact(InviteRequest(email))

                Log.i(TAG, "invite answered HTTP ${response.code()}")
                when (response.code()) {
                    201 -> {
                        Toast.makeText(requireContext(), R.string.invite_sent, Toast.LENGTH_SHORT).show()
                        parentFragmentManager.setFragmentResult("contact_changed", Bundle())
                        dismiss()
                    }
                    404 -> showInlineError(getString(R.string.invite_user_not_found))
                    409 -> showInlineError(getString(R.string.invite_already_exists))
                    else -> showInlineError(errorMessageFrom(response.errorBody()?.string()))
                }
            } catch (e: Exception) {
                Log.w(TAG, "invite failed (${e.javaClass.simpleName})")
                showInlineError(getString(R.string.invite_failed, e.message))
            }
        }
    }

    private fun errorMessageFrom(rawBody: String?): String {
        if (rawBody.isNullOrBlank()) return getString(R.string.invite_failed, "unknown error")
        return runCatching { JSONObject(rawBody).optString("error") }
            .getOrNull()
            ?.takeIf { it.isNotBlank() }
            ?: getString(R.string.invite_failed, rawBody)
    }

    private fun showInlineError(message: String) {
        binding.tvInviteError.text = message
        binding.tvInviteError.visibility = View.VISIBLE
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }

    private companion object {
        const val TAG = "InviteContact"
    }
}
