package com.paravolley.mobile.notifications

import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Context
import android.os.Build
import android.util.Log
import com.google.firebase.FirebaseApp
import com.google.firebase.FirebaseOptions
import com.google.firebase.messaging.FirebaseMessaging
import com.paravolley.mobile.BuildConfig

object PushNotifications {
    const val CHANNEL_ID = "paravolley_updates"
    const val CHANNEL_NAME = "ParaVolley updates"

    private const val PLAYER_TOPIC = "players"
    private const val TAG = "PVPush"

    fun initialize(context: Context): Boolean {
        if (!BuildConfig.FIREBASE_ENABLED) {
            Log.e(
                TAG,
                "Firebase disabled: one or more Firebase Gradle properties are missing."
            )
            return false
        }

        val appContext = context.applicationContext

        createNotificationChannel(appContext)

        try {
            if (FirebaseApp.getApps(appContext).isEmpty()) {
                val options = FirebaseOptions.Builder()
                    .setApplicationId(
                        BuildConfig.FIREBASE_APPLICATION_ID
                    )
                    .setApiKey(
                        BuildConfig.FIREBASE_API_KEY
                    )
                    .setProjectId(
                        BuildConfig.FIREBASE_PROJECT_ID
                    )
                    .setGcmSenderId(
                        BuildConfig.FIREBASE_SENDER_ID
                    )
                    .build()

                val initialized = FirebaseApp.initializeApp(
                    appContext,
                    options
                )

                if (initialized == null) {
                    Log.e(TAG, "FirebaseApp.initializeApp returned null.")
                    return false
                }
            }

            val messaging = FirebaseMessaging.getInstance()
            messaging.isAutoInitEnabled = true

            messaging.token.addOnCompleteListener { task ->
                if (task.isSuccessful) {
                    Log.i(TAG, "FCM registration token obtained successfully.")
                } else {
                    Log.e(
                        TAG,
                        "FCM registration token request failed.",
                        task.exception
                    )
                }
            }

            Log.i(
                TAG,
                "Firebase initialized for project ${BuildConfig.FIREBASE_PROJECT_ID}."
            )

            return true
        } catch (exception: Exception) {
            Log.e(TAG, "Firebase initialization failed.", exception)
            return false
        }
    }

    fun subscribePlayer(context: Context) {
        if (!initialize(context)) {
            Log.e(TAG, "Player topic subscription skipped because Firebase is unavailable.")
            return
        }

        FirebaseMessaging.getInstance()
            .subscribeToTopic(
                PLAYER_TOPIC
            )
            .addOnCompleteListener { task ->
                if (task.isSuccessful) {
                    Log.i(TAG, "Subscribed successfully to topic '$PLAYER_TOPIC'.")
                } else {
                    Log.e(
                        TAG,
                        "Failed to subscribe to topic '$PLAYER_TOPIC'.",
                        task.exception
                    )
                }
            }
    }

    fun unsubscribePlayer(context: Context) {
        if (!initialize(context)) {
            Log.e(TAG, "Player topic unsubscribe skipped because Firebase is unavailable.")
            return
        }

        FirebaseMessaging.getInstance()
            .unsubscribeFromTopic(
                PLAYER_TOPIC
            )
            .addOnCompleteListener { task ->
                if (task.isSuccessful) {
                    Log.i(TAG, "Unsubscribed successfully from topic '$PLAYER_TOPIC'.")
                } else {
                    Log.e(
                        TAG,
                        "Failed to unsubscribe from topic '$PLAYER_TOPIC'.",
                        task.exception
                    )
                }
            }
    }

    private fun createNotificationChannel(
        context: Context
    ) {
        if (
            Build.VERSION.SDK_INT <
            Build.VERSION_CODES.O
        ) {
            return
        }

        val channel = NotificationChannel(
            CHANNEL_ID,
            CHANNEL_NAME,
            NotificationManager.IMPORTANCE_HIGH
        ).apply {
            description =
                "Important ParaVolley Mpumalanga player updates"
        }

        context.getSystemService(
            NotificationManager::class.java
        ).createNotificationChannel(channel)
    }
}
