package com.paravolley.mobile.network

data class LoginRequest(
    val email: String,
    val password: String
)

data class LoginResponse(
    val token: String,
    val expiresAt: String,
    val user: AppUserResponse
)

data class AppUserResponse(
    val id: Int,
    val email: String,
    val role: String,
    val playerId: Int?,
    val playerName: String?
)

data class RegisterPlayerRequest(
    val fullName: String,
    val email: String,
    val phone: String,
    val dateOfBirth: String,
    val province: String?,
    val town: String?,
    val experienceLevel: String?,
    val preferredPosition: String?,
    val classification: String,
    val emergencyContactName: String?,
    val emergencyContactPhone: String?,
    val medicalNotes: String?,
    val consent: Boolean
)

data class RegisterPlayerResponse(
    val message: String,
    val applicationId: Int
)
