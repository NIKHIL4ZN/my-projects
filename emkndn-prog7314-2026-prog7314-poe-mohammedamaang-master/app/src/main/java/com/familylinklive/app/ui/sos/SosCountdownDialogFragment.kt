package com.familylinklive.app.ui.sos

import android.app.Dialog
import android.os.Bundle
import android.os.CountDownTimer
import android.util.Log
import androidx.core.os.bundleOf
import androidx.fragment.app.DialogFragment
import com.familylinklive.app.databinding.DialogSosCountdownBinding
import com.google.android.material.dialog.MaterialAlertDialogBuilder

/**
 * User-Defined Feature 5 - Emergency SOS, step 1: the 5-second cancellable countdown.
 *
 * The SOS button needs a press-and-hold, and then this countdown gives one more chance to back
 * out (Part 1 REQ-24), so a pocket-press can't alert the whole family. When the timer reaches
 * zero the dialog reports back to MapFragment, which gets a GPS fix and sends the alert.
 */
class SosCountdownDialogFragment : DialogFragment() {

    private var binding: DialogSosCountdownBinding? = null
    private var timer: CountDownTimer? = null

    /*
    Title: CountDownTimer - schedule a countdown until a time in the future with regular notifications on intervals along the way
    Author(s): Android Developers
    Date: 2024
    Version: API level 1
    Type: Documentation
    Availability: https://developer.android.com/reference/android/os/CountDownTimer
    */
    override fun onCreateDialog(savedInstanceState: Bundle?): Dialog {
        val b = DialogSosCountdownBinding.inflate(layoutInflater)
        binding = b
        isCancelable = false

        b.btnCancelSos.setOnClickListener {
            Log.i(TAG, "SOS cancelled during countdown")
            dismiss()
        }

        timer = object : CountDownTimer(COUNTDOWN_MS, 1000L) {
            override fun onTick(millisUntilFinished: Long) {
                b.tvCountdown.text = ((millisUntilFinished + 999) / 1000).toString()
            }

            override fun onFinish() {
                Log.i(TAG, "countdown finished - confirming SOS")
                parentFragmentManager.setFragmentResult(RESULT_KEY, bundleOf())
                dismissAllowingStateLoss()
            }
        }.start()

        return MaterialAlertDialogBuilder(requireContext())
            .setView(b.root)
            .setCancelable(false)
            .create()
    }

    override fun onDestroyView() {
        timer?.cancel()
        binding = null
        super.onDestroyView()
    }

    override fun onDismiss(dialog: android.content.DialogInterface) {
        timer?.cancel()
        super.onDismiss(dialog)
    }

    companion object {
        const val RESULT_KEY = "sos_confirmed"
        private const val COUNTDOWN_MS = 5_000L
        private const val TAG = "SosCountdown"
    }
}
