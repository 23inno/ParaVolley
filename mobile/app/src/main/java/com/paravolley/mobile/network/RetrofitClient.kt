package com.paravolley.mobile.network

import android.content.Context
import android.util.Log
import com.paravolley.mobile.BuildConfig
import java.io.IOException
import java.util.concurrent.TimeUnit
import okhttp3.OkHttpClient
import okhttp3.Response
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object RetrofitClient {

    private const val NETWORK_LOG_TAG = "PVNetwork"
    private const val MAX_GET_ATTEMPTS = 3

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
            .retryOnConnectionFailure(true)
            .connectTimeout(20, TimeUnit.SECONDS)
            .readTimeout(30, TimeUnit.SECONDS)
            .writeTimeout(30, TimeUnit.SECONDS)
            .callTimeout(45, TimeUnit.SECONDS)
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
            .addInterceptor { chain ->
                val request = chain.request()

                if (!request.method.equals("GET", ignoreCase = true)) {
                    return@addInterceptor chain.proceed(request)
                }

                var lastException: IOException? = null

                for (attempt in 1..MAX_GET_ATTEMPTS) {
                    try {
                        val response = chain.proceed(request)

                        if (
                            !shouldRetryResponse(response) ||
                            attempt == MAX_GET_ATTEMPTS
                        ) {
                            return@addInterceptor response
                        }

                        if (BuildConfig.DEBUG) {
                            Log.w(
                                NETWORK_LOG_TAG,
                                "GET ${request.url.encodedPath} returned ${response.code}; retrying ($attempt/$MAX_GET_ATTEMPTS)."
                            )
                        }

                        response.close()
                        waitBeforeRetry(attempt)
                    } catch (exception: IOException) {
                        lastException = exception

                        if (BuildConfig.DEBUG) {
                            Log.w(
                                NETWORK_LOG_TAG,
                                "GET ${request.url.encodedPath} failed; retrying ($attempt/$MAX_GET_ATTEMPTS).",
                                exception
                            )
                        }

                        if (attempt == MAX_GET_ATTEMPTS) {
                            throw exception
                        }

                        waitBeforeRetry(attempt)
                    }
                }

                throw lastException
                    ?: IOException("The network request could not be completed.")
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

    private fun shouldRetryResponse(response: Response): Boolean {
        return response.code in setOf(
            408,
            502,
            503,
            504
        )
    }

    private fun waitBeforeRetry(attempt: Int) {
        try {
            Thread.sleep(500L * attempt)
        } catch (exception: InterruptedException) {
            Thread.currentThread().interrupt()
            throw IOException(
                "The network retry was interrupted.",
                exception
            )
        }
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

    val matchesApi: MatchesApi by lazy {
        retrofit.create(
            MatchesApi::class.java
        )
    }

    val qrAttendanceApi: QrAttendanceApi by lazy {
        retrofit.create(
            QrAttendanceApi::class.java
        )
    }
}
