package com.paravolley.mobile.network

data class PlayerDashboardResponse(
    val player: DashboardPlayer,
    val summary: DashboardSummary,
    val upcomingEvents: List<DashboardEvent>,
    val registeredEvents: List<DashboardRegistration>,
    val recentAnnouncements: List<DashboardAnnouncement>,
    val recentMatches: List<DashboardMatch>
)

data class DashboardPlayer(
    val id: Int,
    val name: String,
    val email: String,
    val position: String,
    val team: String,
    val status: String
)

data class DashboardSummary(
    val upcomingEvents: Int,
    val registeredEvents: Int,
    val totalAttendance: Int,
    val presentAttendance: Int,
    val absentAttendance: Int,
    val attendanceRate: Double
)

data class DashboardEvent(
    val id: Int,
    val title: String,
    val date: String,
    val time: String,
    val location: String,
    val type: String,
    val status: String
)

data class DashboardRegistration(
    val registrationId: Int,
    val eventId: Int,
    val title: String,
    val date: String,
    val time: String,
    val location: String,
    val status: String
)

data class DashboardAnnouncement(
    val id: Int,
    val title: String,
    val excerpt: String,
    val category: String,
    val date: String,
    val isPinned: Boolean
)

data class DashboardMatch(
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