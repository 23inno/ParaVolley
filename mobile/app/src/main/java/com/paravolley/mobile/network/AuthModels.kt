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
