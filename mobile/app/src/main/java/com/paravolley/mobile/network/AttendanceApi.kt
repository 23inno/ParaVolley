package com.paravolley.mobile.network

import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header

interface AttendanceApi {

    @GET("api/player/attendance")
    suspend fun getMyAttendance(
        @Header("Authorization")
        authorization: String
    ): Response<List<AttendanceResponse>>
}