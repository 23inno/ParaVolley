package com.paravolley.mobile.data

import com.paravolley.mobile.model.NotificationItem
import com.paravolley.mobile.model.Player
import com.paravolley.mobile.model.SportsEvent

object FakePlayerRepository {

    val currentPlayer = Player(
        id = 1,
        firstName = "Thabo",
        surname = "Mokoena",
        playerNumber = "#14",
        age = 22,
        position = "Midfielder",
        classification = "Varsity",
        location = "Johannesburg, Gauteng",
        email = "thabo.mokoena@email.com",
        phone = "+27 82 456 7890",
        emergencyContactName = "Nomsa Mokoena",
        emergencyContactRelationship = "Mother",
        emergencyContactPhone = "+27 83 123 4567"
    )

    val events = listOf(
        SportsEvent(
            id = 1,
            title = "Spring Tournament Finals",
            category = "Tournament",
            date = "5 May 2026",
            time = "14:00 - 18:00",
            location = "Central Stadium",
            status = "Upcoming",
            spotsRemaining = 12,
            isPast = false
        ),
        SportsEvent(
            id = 2,
            title = "Weekly Practice Session",
            category = "Training",
            date = "1 May 2026",
            time = "17:30 - 19:30",
            location = "Training Ground A",
            status = "Registered",
            spotsRemaining = null,
            isPast = false,
            isRegistered = true
        ),
        SportsEvent(
            id = 3,
            title = "Team Building Event",
            category = "Social",
            date = "8 May 2026",
            time = "10:00 - 16:00",
            location = "Community Centre",
            status = "Upcoming",
            spotsRemaining = 5,
            isPast = false
        ),
        SportsEvent(
            id = 4,
            title = "Advanced Skills Workshop",
            category = "Training",
            date = "12 May 2026",
            time = "15:00 - 17:00",
            location = "Training Ground B",
            status = "Upcoming",
            spotsRemaining = 20,
            isPast = false
        ),
        SportsEvent(
            id = 5,
            title = "Regional Championship Qualifier",
            category = "Tournament",
            date = "15 May 2026",
            time = "09:00 - 17:00",
            location = "Regional Sports Complex",
            status = "Upcoming",
            spotsRemaining = 8,
            isPast = false
        ),
        SportsEvent(
            id = 6,
            title = "Opening Season Match",
            category = "Tournament",
            date = "15 April 2026",
            time = "15:00 - 17:00",
            location = "Main Stadium",
            status = "Completed",
            spotsRemaining = null,
            isPast = true
        ),
        SportsEvent(
            id = 7,
            title = "Pre-Season Training Camp",
            category = "Training",
            date = "10 April 2026",
            time = "09:00 - 16:00",
            location = "Training Complex",
            status = "Completed",
            spotsRemaining = null,
            isPast = true
        ),
        SportsEvent(
            id = 8,
            title = "Team Welcome Mixer",
            category = "Social",
            date = "5 April 2026",
            time = "18:00 - 21:00",
            location = "Community Hall",
            status = "Completed",
            spotsRemaining = null,
            isPast = true
        )
    )

    val notifications = listOf(
        NotificationItem(
            id = 1,
            title = "Event Reminder",
            message = "Spring Tournament Finals starts in 6 days.",
            timeAgo = "2 hours ago",
            isRead = false
        ),
        NotificationItem(
            id = 2,
            title = "New Message",
            message = "Coach posted an update about practice.",
            timeAgo = "5 hours ago",
            isRead = false
        ),
        NotificationItem(
            id = 3,
            title = "Achievement Unlocked",
            message = "You completed 10 events this season!",
            timeAgo = "1 day ago",
            isRead = true
        ),
        NotificationItem(
            id = 4,
            title = "Registration Confirmed",
            message = "Your Weekly Practice registration was confirmed.",
            timeAgo = "2 days ago",
            isRead = true
        ),
        NotificationItem(
            id = 5,
            title = "Venue Change",
            message = "The Skills Workshop moved to Training Ground C.",
            timeAgo = "2 days ago",
            isRead = true
        ),
        NotificationItem(
            id = 6,
            title = "Season Update",
            message = "New team guidelines have been published.",
            timeAgo = "3 days ago",
            isRead = true
        )
    )
}