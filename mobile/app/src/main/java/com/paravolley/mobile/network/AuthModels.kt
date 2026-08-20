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
    val name: String,
    val position: String,
    val team: String,
    val age: Int,
    val email: String,
    val phone: String,
    val disability: String,
    val password: String
)

data class RegisterPlayerResponse(
    val message: String,
    val playerId: Int
)
