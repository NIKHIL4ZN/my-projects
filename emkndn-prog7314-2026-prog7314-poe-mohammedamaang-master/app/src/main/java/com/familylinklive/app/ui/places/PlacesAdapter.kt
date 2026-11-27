package com.familylinklive.app.ui.places

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.familylinklive.app.R
import com.familylinklive.app.data.models.PlaceDto
import com.familylinklive.app.databinding.ItemPlaceBinding

/*
Title: Create dynamic lists with RecyclerView - ListAdapter and DiffUtil for
efficient list updates
Author(s): Android Developers
Date: 2023
Version: androidx.recyclerview 1.3.2
Type: Documentation
Availability: https://developer.android.com/develop/ui/views/layout/recyclerview
This ListAdapter + DiffUtil pattern is reused identically in
ContactsAdapter.kt, ChatAdapter.kt and ActivityAdapter.kt.
*/
class PlacesAdapter(
    private val onEdit: (PlaceDto) -> Unit,
    private val onDelete: (PlaceDto) -> Unit
) : ListAdapter<PlaceDto, PlacesAdapter.PlaceViewHolder>(DIFF) {

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): PlaceViewHolder =
        PlaceViewHolder(ItemPlaceBinding.inflate(LayoutInflater.from(parent.context), parent, false))

    override fun onBindViewHolder(holder: PlaceViewHolder, position: Int) {
        holder.bind(getItem(position))
    }

    inner class PlaceViewHolder(private val binding: ItemPlaceBinding) :
        RecyclerView.ViewHolder(binding.root) {

        fun bind(place: PlaceDto) {
            val context = binding.root.context
            binding.tvPlaceName.text = place.name
            val radius = context.getString(R.string.place_radius_detail, place.radiusM)
            binding.tvPlaceDetail.text =
                if (place.autoMessage.isNullOrBlank()) radius
                else "$radius · ${context.getString(R.string.place_auto_message_set)}"
            binding.root.setOnClickListener { onEdit(place) }
            binding.btnDelete.setOnClickListener { onDelete(place) }
        }
    }

    companion object {
        private val DIFF = object : DiffUtil.ItemCallback<PlaceDto>() {
            override fun areItemsTheSame(oldItem: PlaceDto, newItem: PlaceDto) = oldItem.id == newItem.id
            override fun areContentsTheSame(oldItem: PlaceDto, newItem: PlaceDto) = oldItem == newItem
        }
    }
}
