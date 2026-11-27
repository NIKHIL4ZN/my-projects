package com.familylinklive.app.ui.chat

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.familylinklive.app.data.models.MessageDto
import com.familylinklive.app.databinding.ItemMessageReceivedBinding
import com.familylinklive.app.databinding.ItemMessageSentBinding

// ListAdapter + DiffUtil pattern - see the reference comment in
// ui/places/PlacesAdapter.kt, where this pattern is used first.
class ChatAdapter(private val currentUserId: () -> String?) :
    ListAdapter<MessageDto, RecyclerView.ViewHolder>(DIFF) {

    override fun getItemViewType(position: Int): Int =
        if (getItem(position).senderId == currentUserId()) TYPE_SENT else TYPE_RECEIVED

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): RecyclerView.ViewHolder =
        if (viewType == TYPE_SENT) {
            SentViewHolder(ItemMessageSentBinding.inflate(LayoutInflater.from(parent.context), parent, false))
        } else {
            ReceivedViewHolder(ItemMessageReceivedBinding.inflate(LayoutInflater.from(parent.context), parent, false))
        }

    override fun onBindViewHolder(holder: RecyclerView.ViewHolder, position: Int) {
        val message = getItem(position)
        when (holder) {
            is SentViewHolder -> {
                holder.binding.tvBody.text = message.body
                // Part 3: a message still in the offline outbox is shown as "Sending…" until the server has it.
                holder.binding.tvStatus.visibility = if (message.pending) View.VISIBLE else View.GONE
            }
            is ReceivedViewHolder -> {
                holder.binding.tvSender.text = message.senderName
                holder.binding.tvBody.text = message.body
            }
        }
    }

    class SentViewHolder(val binding: ItemMessageSentBinding) : RecyclerView.ViewHolder(binding.root)
    class ReceivedViewHolder(val binding: ItemMessageReceivedBinding) : RecyclerView.ViewHolder(binding.root)

    companion object {
        private const val TYPE_SENT = 1
        private const val TYPE_RECEIVED = 2
        private val DIFF = object : DiffUtil.ItemCallback<MessageDto>() {
            // A queued message keeps its local id until the server copy replaces it, so compare by
            // clientMessageId when there is one - the bubble then updates in place instead of flickering.
            override fun areItemsTheSame(oldItem: MessageDto, newItem: MessageDto) =
                (oldItem.clientMessageId != null && oldItem.clientMessageId == newItem.clientMessageId) ||
                    oldItem.id == newItem.id

            override fun areContentsTheSame(oldItem: MessageDto, newItem: MessageDto) = oldItem == newItem
        }
    }
}
