package com.paravolley.mobile.network

import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header

interface PlayerApi {

    @GET("api/player/me")
    suspend fun getProfile(
        @Header("Authorization")
        authorization: String
    ): Response<PlayerProfileResponse>
}