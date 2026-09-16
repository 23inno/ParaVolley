package com.paravolley.mobile.notifications

import com.paravolley.mobile.navigation.Routes

object NotificationRouter {
    const val EXTRA_DESTINATION = "notificationDestination"
    const val EXTRA_TITLE = "title"

    fun destinationForTitle(title: String?): String {
        val normalized = title?.trim().orEmpty()

        return when {
            normalized.startsWith(
                "Match result:",
                ignoreCase = true
            ) -> Routes.RESULTS

            normalized.startsWith(
                "Event reminder:",
                ignoreCase = true
            ) -> Routes.EVENTS

            normalized.startsWith(
                "New announcement:",
                ignoreCase = true
            ) -> Routes.NOTIFICATIONS

            else -> Routes.DASHBOARD
        }
    }

    fun isSupportedDestination(route: String?): Boolean =
        route == Routes.DASHBOARD ||
            route == Routes.EVENTS ||
            route == Routes.RESULTS ||
            route == Routes.NOTIFICATIONS ||
            route == Routes.PROFILE
}
