package com.paravolley.mobile

import android.Manifest
import android.content.Intent
import android.graphics.Color as AndroidColor
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.SystemBarStyle
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import com.paravolley.mobile.navigation.ParaVolleyApp
import com.paravolley.mobile.network.RetrofitClient
import com.paravolley.mobile.network.SessionManager
import com.paravolley.mobile.notifications.NotificationRouter
import com.paravolley.mobile.notifications.PushNotifications
import com.paravolley.mobile.screens.LaunchAnimationScreen
import com.paravolley.mobile.ui.theme.ParaVolleyMobileTheme

class MainActivity : ComponentActivity() {

    private val notificationRoute =
        mutableStateOf<String?>(null)

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

        handleNotificationIntent(intent)
        requestNotificationPermissionIfNeeded()

        enableEdgeToEdge(
            statusBarStyle = SystemBarStyle.light(
                AndroidColor.TRANSPARENT,
                AndroidColor.TRANSPARENT
            ),
            navigationBarStyle = SystemBarStyle.light(
                AndroidColor.TRANSPARENT,
                AndroidColor.TRANSPARENT
            )
        )

        setContent {
            ParaVolleyMobileTheme {
                var showLaunchAnimation by remember {
                    mutableStateOf(savedInstanceState == null)
                }

                if (showLaunchAnimation) {
                    LaunchAnimationScreen(
                        onFinished = {
                            showLaunchAnimation = false
                        }
                    )
                } else {
                    ParaVolleyApp(
                        notificationRoute =
                            notificationRoute.value,
                        onNotificationRouteConsumed = {
                            notificationRoute.value = null
                        }
                    )
                }
            }
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        handleNotificationIntent(intent)
    }

    private fun handleNotificationIntent(
        intent: Intent?
    ) {
        val explicitDestination =
            intent?.getStringExtra(
                NotificationRouter.EXTRA_DESTINATION
            )

        val dataTitle =
            intent?.getStringExtra(
                NotificationRouter.EXTRA_TITLE
            )

        val destination =
            when {
                NotificationRouter
                    .isSupportedDestination(
                        explicitDestination
                    ) -> explicitDestination

                !dataTitle.isNullOrBlank() ->
                    NotificationRouter
                        .destinationForTitle(
                            dataTitle
                        )

                else -> null
            }

        notificationRoute.value = destination
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
