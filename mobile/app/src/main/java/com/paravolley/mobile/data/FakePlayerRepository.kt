package com.paravolley.mobile.data

import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import com.paravolley.mobile.model.*

object FakePlayerRepository {

    var currentPlayer = mutableStateOf(
        Player(
            id = 1,
            firstName = "Thabo",
            surname = "Mokoena",
            playerNumber = "#14",
            age = 22,
            position = "Midfielder",
            classification = "1.5 Minimal Impairment (Varsity)",
            team = "Team A (Mpumalanga Stars)",
            location = "Mbombela, Mpumalanga",
            email = "thabo.mokoena@paravolley.co.za",
            phone = "+27 82 456 7890",
            emergencyContactName = "Nomsa Mokoena",
            emergencyContactRelationship = "Mother",
            emergencyContactPhone = "+27 83 123 4567",
            status = "Active",
            attendanceRate = 95.0,
            totalMatches = 14
        )
    )

    val events = mutableStateListOf(
        SportsEvent(
            id = 1,
            title = "Provincial Championship Finals",
            category = "Tournament",
            date = "15 May 2026",
            time = "14:00 - 18:00",
            location = "Mbombela Stadium, Nelspruit",
            status = "Upcoming",
            spotsRemaining = 12,
            isPast = false,
            isRegistered = false,
            description = "Annual provincial tournament determining qualifiers for national playoffs."
        ),
        SportsEvent(
            id = 2,
            title = "Weekly Tactical Practice",
            category = "Training",
            date = "18 May 2026",
            time = "16:00 - 18:30",
            location = "Nelspruit Indoor Sports Hall",
            status = "Registered",
            spotsRemaining = null,
            isPast = false,
            isRegistered = true,
            description = "High intensity offensive formation practice and serving drills."
        ),
        SportsEvent(
            id = 3,
            title = "Adaptive Skills & Agility Workshop",
            category = "Workshop",
            date = "22 May 2026",
            time = "10:00 - 14:00",
            location = "Centurion Community Ground",
            status = "Upcoming",
            spotsRemaining = 6,
            isPast = false,
            isRegistered = false,
            description = "Wheelchair maneuverability and seated agility conditioning by certified coaches."
        ),
        SportsEvent(
            id = 4,
            title = "Regional Championship Qualifier",
            category = "Tournament",
            date = "28 May 2026",
            time = "09:00 - 17:00",
            location = "Regional Sports Complex",
            status = "Upcoming",
            spotsRemaining = 8,
            isPast = false,
            isRegistered = false,
            description = "Competitive qualifier matches against visiting district squads."
        ),
        SportsEvent(
            id = 5,
            title = "Season Opener vs Limpopo Rhinos",
            category = "Tournament",
            date = "12 April 2026",
            time = "15:00 - 17:00",
            location = "Main Stadium Arena",
            status = "Completed",
            spotsRemaining = null,
            isPast = true,
            isRegistered = true,
            description = "Victory in 3 straight sets (25-21, 25-18, 25-20)."
        ),
        SportsEvent(
            id = 6,
            title = "Pre-Season Conditioning Camp",
            category = "Training",
            date = "05 April 2026",
            time = "09:00 - 16:00",
            location = "Training Complex Gym",
            status = "Completed",
            spotsRemaining = null,
            isPast = true,
            isRegistered = true,
            description = "Comprehensive physical assessments, baseline testing and team bonding."
        )
    )

    val notifications = mutableStateListOf(
        NotificationItem(
            id = 1,
            title = "Tournament Final Registration",
            message = "Provincial Championship registration is confirmed for Team A.",
            timeAgo = "2 hours ago",
            isRead = false,
            type = "Event"
        ),
        NotificationItem(
            id = 2,
            title = "Venue Confirmation",
            message = "Friday practice session moved to Training Hall B at 16:00.",
            timeAgo = "Yesterday",
            isRead = false,
            type = "Message"
        ),
        NotificationItem(
            id = 3,
            title = "Attendance Target Reached",
            message = "Congratulations! You maintained 95% attendance this training block.",
            timeAgo = "3 days ago",
            isRead = true,
            type = "Achievement"
        )
    )

    val announcements = listOf(
        AnnouncementItem(
            id = 1,
            title = "Provincial Championship Registration Open",
            excerpt = "All players must confirm medical classification before May 10, 2026.",
            category = "Tournament",
            date = "10 May 2026",
            isPinned = true
        ),
        AnnouncementItem(
            id = 2,
            title = "New Training Equipment Delivered",
            excerpt = "Professional Mikasa Paralympic grade balls now available in the equipment room.",
            category = "Equipment",
            date = "08 May 2026",
            isPinned = false
        )
    )

    val recentMatches = listOf(
        MatchFixture(
            id = 1,
            teamA = "Mpumalanga Stars",
            teamB = "Limpopo Rhinos",
            date = "12 April 2026",
            time = "15:00",
            venue = "Main Stadium Arena",
            tournament = "Provincial League",
            status = "Completed",
            scoreA = 3,
            scoreB = 0
        ),
        MatchFixture(
            id = 2,
            teamA = "Mpumalanga Stars",
            teamB = "Gauteng Warriors",
            date = "15 May 2026",
            time = "14:00",
            venue = "Mbombela Stadium",
            tournament = "Provincial Championship",
            status = "Upcoming",
            scoreA = null,
            scoreB = null
        )
    )

    fun toggleRegistration(eventId: Int) {
        val index = events.indexOfFirst { it.id == eventId }
        if (index != -1) {
            val event = events[index]
            val newRegistered = !event.isRegistered
            events[index] = event.copy(
                isRegistered = newRegistered,
                status = if (newRegistered) "Registered" else "Upcoming"
            )
        }
    }

    fun recordCheckIn(code: String): Pair<Boolean, String> {
        val registeredEvent = events.firstOrNull { it.isRegistered && !it.isPast }
        return if (registeredEvent != null) {
            Pair(true, "Attendance verified for '${registeredEvent.title}' at ${registeredEvent.location}!")
        } else {
            Pair(true, "Attendance verified successfully for current session! Points & check-in recorded.")
        }
    }
}
