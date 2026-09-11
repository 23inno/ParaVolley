package com.paravolley.mobile.notifications

import android.content.Context
import com.google.firebase.FirebaseApp
import com.google.firebase.FirebaseOptions
import com.google.firebase.messaging.FirebaseMessaging
import com.paravolley.mobile.BuildConfig

object PushNotifications {
    const val CHANNEL_ID = "paravolley_updates"
    const val CHANNEL_NAME = "ParaVolley updates"

    private const val PLAYER_TOPIC = "players"

    fun initialize(context: Context): Boolean {
        if (!BuildConfig.FIREBASE_ENABLED) {
            return false
        }

        val appContext = context.applicationContext

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

            FirebaseApp.initializeApp(
                appContext,
                options
            )
        }

        return FirebaseApp.getApps(appContext)
            .isNotEmpty()
    }

    fun subscribePlayer(context: Context) {
        if (!initialize(context)) {
            return
        }

        FirebaseMessaging.getInstance()
            .subscribeToTopic(
                PLAYER_TOPIC
            )
    }

    fun unsubscribePlayer(context: Context) {
        if (!initialize(context)) {
            return
        }

        FirebaseMessaging.getInstance()
            .unsubscribeFromTopic(
                PLAYER_TOPIC
            )
    }
}
