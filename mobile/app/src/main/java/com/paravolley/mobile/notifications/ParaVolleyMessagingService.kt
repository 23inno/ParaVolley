package com.paravolley.mobile.notifications

import android.Manifest
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import androidx.core.content.ContextCompat
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import com.paravolley.mobile.MainActivity
import com.paravolley.mobile.R
import com.paravolley.mobile.network.SessionManager

class ParaVolleyMessagingService :
    FirebaseMessagingService() {

    override fun onNewToken(token: String) {
        super.onNewToken(token)

        if (
            SessionManager(applicationContext)
                .hasValidPlayerSession()
        ) {
            PushNotifications.subscribePlayer(
                applicationContext
            )
        }
    }

    override fun onMessageReceived(
        remoteMessage: RemoteMessage
    ) {
        super.onMessageReceived(remoteMessage)

        val title =
            remoteMessage.notification?.title
                ?: remoteMessage.data["title"]
                ?: "ParaVolley Mpumalanga"

        val body =
            remoteMessage.notification?.body
                ?: remoteMessage.data["body"]
                ?: "You have a new ParaVolley update."

        showNotification(
            title = title,
            body = body
        )
    }

    private fun showNotification(
        title: String,
        body: String
    ) {
        createNotificationChannel()

        if (
            Build.VERSION.SDK_INT >=
            Build.VERSION_CODES.TIRAMISU &&
            ContextCompat.checkSelfPermission(
                this,
                Manifest.permission.POST_NOTIFICATIONS
            ) != PackageManager.PERMISSION_GRANTED
        ) {
            return
        }

        val openAppIntent = Intent(
            this,
            MainActivity::class.java
        ).apply {
            flags =
                Intent.FLAG_ACTIVITY_CLEAR_TOP or
                    Intent.FLAG_ACTIVITY_SINGLE_TOP
        }

        val pendingIntent = PendingIntent.getActivity(
            this,
            0,
            openAppIntent,
            PendingIntent.FLAG_UPDATE_CURRENT or
                PendingIntent.FLAG_IMMUTABLE
        )

        val notification = NotificationCompat.Builder(
            this,
            PushNotifications.CHANNEL_ID
        )
            .setSmallIcon(R.mipmap.ic_launcher)
            .setContentTitle(title)
            .setContentText(body)
            .setStyle(
                NotificationCompat.BigTextStyle()
                    .bigText(body)
            )
            .setPriority(
                NotificationCompat.PRIORITY_HIGH
            )
            .setAutoCancel(true)
            .setContentIntent(pendingIntent)
            .build()

        NotificationManagerCompat.from(this)
            .notify(
                System.currentTimeMillis()
                    .toInt(),
                notification
            )
    }

    private fun createNotificationChannel() {
        if (
            Build.VERSION.SDK_INT <
            Build.VERSION_CODES.O
        ) {
            return
        }

        val channel = NotificationChannel(
            PushNotifications.CHANNEL_ID,
            PushNotifications.CHANNEL_NAME,
            NotificationManager.IMPORTANCE_HIGH
        ).apply {
            description =
                "Important ParaVolley Mpumalanga player updates"
        }

        getSystemService(
            NotificationManager::class.java
        ).createNotificationChannel(channel)
    }
}
