package com.paravolley.mobile.network

data class EventResponse(
    val id: Int,
    val title: String,
    val date: String,
    val time: String,
    val location: String,
    val type: String,
    val participants: Int,
    val status: String,
    val description: String
)

data class EventRegistrationResponse(
    val id: Int,
    val eventId: Int,
    val eventTitle: String,
    val eventDate: String,
    val eventTime: String,
    val eventLocation: String,
    val eventType: String,
    val eventStatus: String,
    val registrationStatus: String,
    val registeredAtUtc: String
)