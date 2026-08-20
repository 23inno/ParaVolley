package com.paravolley.mobile.network

import com.paravolley.mobile.BuildConfig
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object RetrofitClient {

    private val baseUrl: String =
        BuildConfig.API_BASE_URL.also { value ->
            require(value.endsWith("/")) {
                "API_BASE_URL must end with '/'."
            }
        }

    private val retrofit: Retrofit by lazy {
        Retrofit.Builder()
            .baseUrl(baseUrl)
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
