package com.paravolley.mobile.model

data class Player(
    val id: Int,
    val firstName: String,
    val surname: String,
    val playerNumber: String,
    val age: Int,
    val position: String,
    val classification: String,
    val location: String,
    val email: String,
    val phone: String,
    val emergencyContactName: String,
    val emergencyContactRelationship: String,
    val emergencyContactPhone: String
) {
    val fullName: String
        get() = "$firstName $surname"
}

data class SportsEvent(
    val id: Int,
    val title: String,
    val category: String,
    val date: String,
    val time: String,
    val location: String,
    val status: String,
    val spotsRemaining: Int?,
    val isPast: Boolean,
    val isRegistered: Boolean = false
)

data class NotificationItem(
    val id: Int,
    val title: String,
    val message: String,
    val timeAgo: String,
    val isRead: Boolean
)