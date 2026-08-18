package com.paravolley.mobile.network

import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header

interface DashboardApi {

    @GET("api/player/dashboard")
    suspend fun getDashboard(
        @Header("Authorization")
        authorization: String
    ): Response<PlayerDashboardResponse>
}