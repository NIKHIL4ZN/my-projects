package com.familylinklive.app.ui.activity

import android.text.format.DateUtils
import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.familylinklive.app.R
import com.familylinklive.app.databinding.ItemActivityBinding

// ListAdapter + DiffUtil pattern - see the reference comment in
// ui/places/PlacesAdapter.kt, where this pattern is used first.
class ActivityAdapter(
    private val onClick: (ActivityItem) -> Unit
) : ListAdapter<ActivityItem, ActivityAdapter.ViewHolder>(DIFF) {

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int) =
        ViewHolder(ItemActivityBinding.inflate(LayoutInflater.from(parent.context), parent, false))

    override fun onBindViewHolder(holder: ViewHolder, position: Int) = holder.bind(getItem(position))

    inner class ViewHolder(private val binding: ItemActivityBinding) : RecyclerView.ViewHolder(binding.root) {
        fun bind(item: ActivityItem) {
            val context = binding.root.context
            val place = item.placeName.orEmpty()
            binding.tvActivityText.text = when (item.type) {
                "GEOFENCE_ARRIVED" -> context.getString(R.string.activity_arrived, item.actorName, place)
                "GEOFENCE_LEFT" -> context.getString(R.string.activity_left, item.actorName, place)
                "SOS" -> context.getString(R.string.activity_sos, item.actorName)
                "INVITE_RECEIVED" -> context.getString(R.string.activity_invite, item.actorName)
                "INVITE_ACCEPTED" -> context.getString(R.string.activity_accepted, item.actorName)
                else -> item.actorName
            }
            binding.tvActivityTime.text = DateUtils.getRelativeTimeSpanString(
                item.createdAt, System.currentTimeMillis(), DateUtils.MINUTE_IN_MILLIS
            )
            binding.root.setOnClickListener { onClick(item) }
        }
    }

    companion object {
        private val DIFF = object : DiffUtil.ItemCallback<ActivityItem>() {
            override fun areItemsTheSame(oldItem: ActivityItem, newItem: ActivityItem) = oldItem.id == newItem.id
            override fun areContentsTheSame(oldItem: ActivityItem, newItem: ActivityItem) = oldItem == newItem
        }
    }
}
