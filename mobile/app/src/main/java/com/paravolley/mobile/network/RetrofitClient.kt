package com.paravolley.mobile.network

import android.content.Context
import com.paravolley.mobile.BuildConfig
import okhttp3.OkHttpClient
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object RetrofitClient {

    private lateinit var sessionManager: SessionManager

    fun initialize(context: Context) {
        if (!::sessionManager.isInitialized) {
            sessionManager = SessionManager(
                context.applicationContext
            )
        }
    }

    private val baseUrl: String =
        BuildConfig.API_BASE_URL.also { value ->
            require(value.endsWith("/")) {
                "API_BASE_URL must end with '/'."
            }
        }

    private val retrofit: Retrofit by lazy {
        check(::sessionManager.isInitialized) {
            "RetrofitClient must be initialized before use."
        }

        val httpClient = OkHttpClient.Builder()
            .addInterceptor { chain ->
                val originalRequest = chain.request()
                val requestBuilder =
                    originalRequest.newBuilder()

                if (
                    originalRequest.header(
                        "Authorization"
                    ).isNullOrBlank()
                ) {
                    sessionManager
                        .getAuthorizationHeader()
                        ?.let { authorization ->
                            requestBuilder.header(
                                "Authorization",
                                authorization
                            )
                        }
                }

                val response = chain.proceed(
                    requestBuilder.build()
                )

                if (
                    response.code == 401 &&
                    !originalRequest.url
                        .encodedPath
                        .endsWith("/api/auth/login")
                ) {
                    sessionManager.clearSession()
                    SessionEvents
                        .notifySessionExpired()
                }

                response
            }
            .build()

        Retrofit.Builder()
            .baseUrl(baseUrl)
            .client(httpClient)
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
