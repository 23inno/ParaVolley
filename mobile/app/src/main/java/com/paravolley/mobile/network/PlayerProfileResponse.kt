package com.paravolley.mobile.network

data class PlayerProfileResponse(
    val id: Int,
    val name: String,
    val position: String,
    val team: String,
    val status: String,
    val age: Int,
    val matches: Int,
    val email: String,
    val phone: String,
    val disability: String
)