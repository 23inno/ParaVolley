package com.paravolley.mobile.network

import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST

interface AuthApi {

    @POST("api/auth/login")
    suspend fun login(
        @Body request: LoginRequest
    ): Response<LoginResponse>

    @POST("api/auth/register/player")
    suspend fun registerPlayer(
        @Body request: RegisterPlayerRequest
    ): Response<RegisterPlayerResponse>
}
