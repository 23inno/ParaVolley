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
    val emergencyContactName: String? = null,
    val emergencyContactPhone: String? = null,
    val joinedDate: String? = null,
    val disability: String,
    val hasProfilePhoto: Boolean = false
)

data class UpdatePlayerProfileRequest(
    val age: Int,
    val email: String,
    val phone: String,
    val emergencyContactName: String,
    val emergencyContactPhone: String
)
