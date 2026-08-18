package com.paravolley.mobile.network

import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object RetrofitClient {

    private const val BASE_URL =
        "http://10.0.2.2:5080/"

    private val retrofit: Retrofit by lazy {
        Retrofit.Builder()
            .baseUrl(BASE_URL)
            .addConverterFactory(
                GsonConverterFactory.create()
            )
            .build()
    }

    val authApi: AuthApi by lazy {
        retrofit.create(
            AuthApi::class.java
        )
    }

    val playerApi: PlayerApi by lazy {
        retrofit.create(
            PlayerApi::class.java
        )
    }

    val dashboardApi: DashboardApi by lazy {
        retrofit.create(
            DashboardApi::class.java
        )
    }

    val eventsApi: EventsApi by lazy {
        retrofit.create(
            EventsApi::class.java
        )
    }

    val attendanceApi: AttendanceApi by lazy {
        retrofit.create(
            AttendanceApi::class.java
        )
    }

    val announcementsApi: AnnouncementsApi by lazy {
        retrofit.create(
            AnnouncementsApi::class.java
        )
    }

    val qrAttendanceApi: QrAttendanceApi by lazy {
        retrofit.create(
            QrAttendanceApi::class.java
        )
    }
}