package com.paravolley.mobile

import android.Manifest
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import com.paravolley.mobile.navigation.ParaVolleyApp
import com.paravolley.mobile.network.RetrofitClient
import com.paravolley.mobile.network.SessionManager
import com.paravolley.mobile.notifications.PushNotifications
import com.paravolley.mobile.ui.theme.ParaVolleyMobileTheme

class MainActivity : ComponentActivity() {

    override fun onCreate(
        savedInstanceState: Bundle?
    ) {
        super.onCreate(savedInstanceState)

        RetrofitClient.initialize(
            applicationContext
        )

        PushNotifications.initialize(
            applicationContext
        )

        if (
            SessionManager(applicationContext)
                .hasValidPlayerSession()
        ) {
            PushNotifications.subscribePlayer(
                applicationContext
            )
        }

        requestNotificationPermissionIfNeeded()

        enableEdgeToEdge()

        setContent {
            ParaVolleyMobileTheme {
                ParaVolleyApp()
            }
        }
    }

    private fun requestNotificationPermissionIfNeeded() {
        if (
            Build.VERSION.SDK_INT >=
            Build.VERSION_CODES.TIRAMISU &&
            ContextCompat.checkSelfPermission(
                this,
                Manifest.permission.POST_NOTIFICATIONS
            ) != PackageManager.PERMISSION_GRANTED
        ) {
            ActivityCompat.requestPermissions(
                this,
                arrayOf(
                    Manifest.permission.POST_NOTIFICATIONS
                ),
                NOTIFICATION_PERMISSION_REQUEST
            )
        }
    }

    companion object {
        private const val NOTIFICATION_PERMISSION_REQUEST = 2001
    }
}
