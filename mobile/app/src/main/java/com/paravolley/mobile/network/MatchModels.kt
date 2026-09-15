package com.paravolley.mobile.network

data class MatchResponse(
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
