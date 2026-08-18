package com.paravolley.mobile.network

import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.Header
import retrofit2.http.POST

interface QrAttendanceApi {

    @POST("api/qr-attendance/check-in")
    suspend fun checkIn(
        @Header("Authorization")
        authorization: String,
        @Body
        request: QrCheckInRequest
    ): Response<QrCheckInResponse>
}