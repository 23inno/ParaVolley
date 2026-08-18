package com.paravolley.mobile.network

data class QrCheckInRequest(
    val token: String
)

data class QrCheckInResponse(
    val id: Int,
    val playerId: Int,
    val playerName: String,
    val eventId: Int,
    val eventTitle: String,
    val eventDate: String,
    val eventTime: String,
    val eventLocation: String,
    val attendanceDate: String,
    val status: String
)