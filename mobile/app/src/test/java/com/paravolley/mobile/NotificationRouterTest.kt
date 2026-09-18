package com.paravolley.mobile

import com.paravolley.mobile.navigation.Routes
import com.paravolley.mobile.notifications.NotificationRouter
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class NotificationRouterTest {

    @Test
    fun upcomingMatchNotificationOpensUpcomingMatches() {
        val route = NotificationRouter.destinationForTitle(
            "Upcoming match: ParaVolley Mpumalanga vs Visitors"
        )

        assertEquals(Routes.UPCOMING_MATCHES, route)
        assertTrue(
            NotificationRouter.isSupportedDestination(route)
        )
    }
}
