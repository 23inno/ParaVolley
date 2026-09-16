package com.paravolley.mobile.model

data class Player(
    val id: Int,
    val firstName: String,
    val surname: String,
    val playerNumber: String,
    val age: Int,
    val position: String,
    val classification: String,
    val team: String,
    val location: String,
    val email: String,
    val phone: String,
    val emergencyContactName: String,
    val emergencyContactRelationship: String,
    val emergencyContactPhone: String,
    val status: String = "Active",
    val attendanceRate: Double = 94.5,
    val totalMatches: Int = 18
) {
    val fullName: String
        get() = "$firstName $surname"
}

data class SportsEvent(
    val id: Int,
    val title: String,
    val category: String, // "Tournament", "Training", "Workshop", "Social"
    val date: String,
    val time: String,
    val location: String,
    val status: String,   // "Upcoming", "Registered", "Completed", "Cancelled"
    val spotsRemaining: Int?,
    val isPast: Boolean,
    val isRegistered: Boolean = false,
    val description: String = ""
)

data class NotificationItem(
    val id: Int,
    val title: String,
    val message: String,
    val timeAgo: String,
    val isRead: Boolean,
    val type: String = "General" // "Event", "Message", "Achievement", "System"
)

data class AnnouncementItem(
    val id: Int,
    val title: String,
    val excerpt: String,
    val category: String,
    val date: String,
    val isPinned: Boolean
)

data class MatchFixture(
    val id: Int,
    val teamA: String,
    val teamB: String,
    val date: String,
    val time: String,
    val venue: String,
    val tournament: String,
    val status: String,
    val scoreA: Int?,
    val scoreB: Int?
)
