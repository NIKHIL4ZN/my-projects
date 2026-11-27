package com.familylinklive.app.ui.contacts

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import coil.load
import coil.transform.CircleCropTransformation
import com.familylinklive.app.R
import com.familylinklive.app.data.models.ContactDto
import com.familylinklive.app.databinding.ItemContactBinding

// ListAdapter + DiffUtil pattern - see the reference comment in
// ui/places/PlacesAdapter.kt, where this pattern is used first.
class ContactsAdapter(
    private val onOpenChat: (ContactDto) -> Unit,
    private val onAccept: (ContactDto) -> Unit,
    private val onRemove: (ContactDto) -> Unit
) : ListAdapter<ContactDto, ContactsAdapter.ContactViewHolder>(DIFF) {

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ContactViewHolder =
        ContactViewHolder(ItemContactBinding.inflate(LayoutInflater.from(parent.context), parent, false))

    override fun onBindViewHolder(holder: ContactViewHolder, position: Int) {
        holder.bind(getItem(position))
    }

    inner class ContactViewHolder(private val binding: ItemContactBinding) :
        RecyclerView.ViewHolder(binding.root) {

        fun bind(contact: ContactDto) {
            val context = binding.root.context
            binding.tvContactName.text = contact.otherUserDisplayName

            /*
            Title: Coil - an image loading library for Android backed by Kotlin Coroutines
            (loading a profile photo stored in Azure Blob Storage, cropped to a circle)
            Author(s): Coil contributors
            Date: 2024
            Version: coil 2.6.0
            Type: Documentation
            Availability: https://coil-kt.github.io/coil/getting_started/
            */
            binding.ivAvatar.load(contact.otherUserPhotoUrl) {
                transformations(CircleCropTransformation())
                placeholder(R.drawable.ic_contacts)
                error(R.drawable.ic_contacts)
                fallback(R.drawable.ic_contacts)
            }

            when {
                contact.status == "ACTIVE" -> {
                    binding.tvContactStatus.text = context.getString(R.string.status_active)
                    binding.incomingActions.visibility = View.GONE
                    binding.root.setOnClickListener { onOpenChat(contact) }
                }
                contact.isIncomingRequest -> {
                    binding.tvContactStatus.text = context.getString(R.string.status_incoming_request)
                    binding.incomingActions.visibility = View.VISIBLE
                    binding.root.setOnClickListener(null)
                    binding.btnAccept.setOnClickListener { onAccept(contact) }
                    binding.btnDecline.setOnClickListener { onRemove(contact) }
                }
                else -> {
                    binding.tvContactStatus.text = context.getString(R.string.status_outgoing_pending)
                    binding.incomingActions.visibility = View.GONE
                    binding.root.setOnClickListener(null)
                }
            }

            binding.btnRemove.setOnClickListener { onRemove(contact) }
        }
    }

    companion object {
        private val DIFF = object : DiffUtil.ItemCallback<ContactDto>() {
            override fun areItemsTheSame(oldItem: ContactDto, newItem: ContactDto) = oldItem.id == newItem.id
            override fun areContentsTheSame(oldItem: ContactDto, newItem: ContactDto) = oldItem == newItem
        }
    }
}
