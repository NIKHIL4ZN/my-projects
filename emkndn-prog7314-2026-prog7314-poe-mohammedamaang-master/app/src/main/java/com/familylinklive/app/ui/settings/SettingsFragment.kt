package com.familylinklive.app.ui.settings

import android.Manifest
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.activity.result.PickVisualMediaRequest
import androidx.activity.result.contract.ActivityResultContracts
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import coil.load
import coil.transform.CircleCropTransformation
import com.familylinklive.app.R
import com.familylinklive.app.data.ApiClient
import com.familylinklive.app.data.SessionManager
import com.familylinklive.app.data.models.SettingsRequest
import com.familylinklive.app.data.models.UserDto
import com.familylinklive.app.databinding.FragmentSettingsBinding
import com.familylinklive.app.location.LocationSharingService
import com.familylinklive.app.util.AppScope
import com.familylinklive.app.util.BiometricHelper
import com.familylinklive.app.util.ImageUtils
import com.familylinklive.app.util.LocaleCodes
import com.familylinklive.app.util.LocaleHelper
import com.familylinklive.app.util.SignOut
import com.familylinklive.app.util.hasFineLocationPermission
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.RequestBody.Companion.toRequestBody

/**
 * Settings menu (REQ-5 .. REQ-9). Every setting here is real and persisted:
 *  - Language (English / Afrikaans / isiZulu) - switches the whole app instantly (Part 3);
 *  - Biometric lock - fingerprint / face to open the app (Part 3);
 *  - Share my location - the privacy switch, synced to the server;
 *  - Live location sharing - a foreground service that keeps sharing with the app closed (Part 3);
 *  - Profile photo - uploaded to Azure Blob Storage through the API (Part 3);
 *  - Sign out - clears Firebase, Google, the push token, the offline database and the session.
 */
class SettingsFragment : Fragment() {

    private var _binding: FragmentSettingsBinding? = null
    private val binding get() = _binding!!
    private lateinit var session: SessionManager

    /*
    Title: Photo picker - ActivityResultContracts.PickVisualMedia
    Author(s): Android Developers
    Date: 2024
    Version: androidx.activity 1.9.1
    Type: Documentation
    Availability: https://developer.android.com/training/data-storage/shared/photopicker
    */
    private val pickPhoto = registerForActivityResult(ActivityResultContracts.PickVisualMedia()) { uri ->
        if (uri != null) uploadPhoto(uri)
    }

    private val backgroundPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions()
    ) { results ->
        val granted = results[Manifest.permission.ACCESS_FINE_LOCATION] == true ||
            (context?.let { hasFineLocationPermission(it) } == true)
        Log.i(TAG, "background-sharing permissions granted=$granted")
        if (granted) {
            enableBackgroundSharing()
        } else {
            _binding?.switchBackgroundSharing?.isChecked = false
            context?.let { Toast.makeText(it, R.string.location_permission_required, Toast.LENGTH_SHORT).show() }
        }
    }

    override fun onCreateView(
        inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?
    ): View {
        _binding = FragmentSettingsBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        Log.d(TAG, "onViewCreated")
        session = SessionManager.getInstance(requireContext())

        bindProfile(session.user)
        binding.tvLanguageValue.text = LocaleHelper.displayName(LocaleHelper.currentCode())
        binding.switchBiometric.isChecked = session.biometricEnabled
        binding.switchShareLocation.isChecked = session.shareLocationEnabled
        binding.switchBackgroundSharing.isChecked = session.backgroundSharing

        binding.btnChangePhoto.setOnClickListener {
            pickPhoto.launch(PickVisualMediaRequest(ActivityResultContracts.PickVisualMedia.ImageOnly))
        }
        binding.rowLanguage.setOnClickListener { showLanguageDialog() }
        binding.switchBiometric.setOnClickListener { onBiometricToggled() }
        binding.switchShareLocation.setOnClickListener { onShareLocationToggled() }
        binding.switchBackgroundSharing.setOnClickListener { onBackgroundSharingToggled() }
        binding.btnSignOut.setOnClickListener { signOut() }
        binding.btnDeleteAccount.setOnClickListener { confirmDeleteAccount() }
    }

    private fun bindProfile(user: UserDto?) {
        val b = _binding ?: return
        b.tvName.text = user?.displayName.orEmpty()
        b.tvEmail.text = user?.email.orEmpty()
        b.ivAvatar.load(user?.photoUrl) {
            transformations(CircleCropTransformation())
            placeholder(R.drawable.ic_contacts)
            error(R.drawable.ic_contacts)
            fallback(R.drawable.ic_contacts)
        }
    }

    // ---------------------------------------------------------------- language

    private fun showLanguageDialog() {
        val appContext = requireContext().applicationContext
        val codes = LocaleCodes.supported
        val names = codes.map { LocaleHelper.displayName(it) }.toTypedArray()
        MaterialAlertDialogBuilder(requireContext())
            .setTitle(R.string.choose_language)
            .setSingleChoiceItems(names, codes.indexOf(LocaleHelper.currentCode())) { dialog, which ->
                val code = codes[which]
                dialog.dismiss()
                Log.i(TAG, "language changed to $code")
                // Tell the server first-class (it writes push notifications in this language), then
                // switch the UI; AppCompat recreates the activity so the new language shows at once.
                AppScope.scope.launch {
                    runCatching {
                        ApiClient.getService(appContext).updateSettings(SettingsRequest(language = LocaleCodes.toServer(code)))
                    }
                }
                LocaleHelper.apply(code)
            }
            .setNegativeButton(R.string.cancel, null)
            .show()
    }

    // ---------------------------------------------------------------- biometric lock

    private fun onBiometricToggled() {
        val wantOn = binding.switchBiometric.isChecked
        if (!wantOn) {
            session.biometricEnabled = false
            syncBiometric(false)
            return
        }
        if (!BiometricHelper.canAuthenticate(requireContext())) {
            binding.switchBiometric.isChecked = false
            Toast.makeText(requireContext(), R.string.biometric_unavailable, Toast.LENGTH_LONG).show()
            return
        }
        // Prove it works once before turning the lock on, so nobody can lock themselves out.
        BiometricHelper.authenticate(
            activity = requireActivity(),
            title = getString(R.string.biometric_prompt_title),
            subtitle = getString(R.string.biometric_prompt_subtitle),
            negativeText = getString(R.string.biometric_cancel),
            onSuccess = {
                session.biometricEnabled = true
                syncBiometric(true)
                context?.let { Toast.makeText(it, R.string.biometric_enabled_toast, Toast.LENGTH_SHORT).show() }
            },
            onFailure = { _, _ -> _binding?.switchBiometric?.isChecked = false }
        )
    }

    private fun syncBiometric(enabled: Boolean) {
        val appContext = requireContext().applicationContext
        AppScope.scope.launch {
            runCatching { ApiClient.getService(appContext).updateSettings(SettingsRequest(biometricEnabled = enabled)) }
        }
    }

    // ---------------------------------------------------------------- location privacy / sharing

    private fun onShareLocationToggled() {
        val enabled = binding.switchShareLocation.isChecked
        session.shareLocationEnabled = enabled
        Log.i(TAG, "share my location -> $enabled")
        val appContext = requireContext().applicationContext
        AppScope.scope.launch {
            runCatching { ApiClient.getService(appContext).updateSettings(SettingsRequest(shareLocation = enabled)) }
        }
    }

    private fun onBackgroundSharingToggled() {
        if (!binding.switchBackgroundSharing.isChecked) {
            session.backgroundSharing = false
            LocationSharingService.stop(requireContext())
            Log.i(TAG, "live location sharing stopped")
            return
        }
        if (hasFineLocationPermission(requireContext())) {
            enableBackgroundSharing()
        } else {
            val permissions = mutableListOf(Manifest.permission.ACCESS_FINE_LOCATION)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) permissions.add(Manifest.permission.POST_NOTIFICATIONS)
            backgroundPermissionLauncher.launch(permissions.toTypedArray())
        }
    }

    private fun enableBackgroundSharing() {
        val ctx = context ?: return
        session.backgroundSharing = true
        LocationSharingService.start(ctx)
        Log.i(TAG, "live location sharing started")
    }

    // ---------------------------------------------------------------- profile photo (Azure Blob Storage)

    private fun uploadPhoto(uri: Uri) {
        val appContext = requireContext().applicationContext
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                val bytes = withContext(Dispatchers.Default) { ImageUtils.toJpegBytes(appContext, uri) }
                if (bytes == null) {
                    Toast.makeText(appContext, getString(R.string.photo_failed, getString(R.string.something_went_wrong)), Toast.LENGTH_LONG).show()
                    return@launch
                }
                Log.i(TAG, "uploading profile photo (${bytes.size} bytes)")
                val updated = ApiClient.getService(appContext).uploadPhoto(bytes.toRequestBody("image/jpeg".toMediaType()))
                session.user = updated
                bindProfile(updated)
                Toast.makeText(appContext, R.string.photo_updated, Toast.LENGTH_SHORT).show()
            } catch (e: Exception) {
                Log.e(TAG, "photo upload failed", e)
                Toast.makeText(appContext, getString(R.string.photo_failed, e.message), Toast.LENGTH_LONG).show()
            }
        }
    }

    // ---------------------------------------------------------------- sign out

    private fun signOut() {
        val activity = requireActivity()
        val appContext = activity.applicationContext
        viewLifecycleOwner.lifecycleScope.launch {
            SignOut.perform(appContext)
            SignOut.openLogin(appContext)
            activity.finish()
        }
    }

    // ---------------------------------------------------------------- delete account (Google Play requirement)

    private fun confirmDeleteAccount() {
        MaterialAlertDialogBuilder(requireContext())
            .setTitle(R.string.delete_account_title)
            .setMessage(R.string.delete_account_message)
            .setPositiveButton(R.string.delete_account_confirm) { _, _ -> deleteAccount() }
            .setNegativeButton(R.string.cancel, null)
            .show()
    }

    private fun deleteAccount() {
        val activity = requireActivity()
        val appContext = activity.applicationContext
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                val response = ApiClient.getService(appContext).deleteAccount()
                if (!response.isSuccessful) {
                    Toast.makeText(appContext, appContext.getString(R.string.account_delete_failed, response.code().toString()), Toast.LENGTH_LONG).show()
                    return@launch
                }
                Log.i(TAG, "account deleted on the server - signing out locally")
                SignOut.perform(appContext, accountDeleted = true)
                Toast.makeText(appContext, R.string.account_deleted, Toast.LENGTH_LONG).show()
                SignOut.openLogin(appContext)
                activity.finish()
            } catch (e: java.io.IOException) {
                Toast.makeText(appContext, R.string.account_delete_offline, Toast.LENGTH_LONG).show()
            } catch (e: Exception) {
                Log.e(TAG, "account deletion failed", e)
                Toast.makeText(appContext, appContext.getString(R.string.account_delete_failed, e.message), Toast.LENGTH_LONG).show()
            }
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }

    private companion object {
        const val TAG = "SettingsFragment"
    }
}
