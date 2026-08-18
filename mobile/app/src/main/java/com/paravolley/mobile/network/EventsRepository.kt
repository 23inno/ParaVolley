package com.paravolley.mobile.network

import android.content.Context
import com.google.gson.Gson

class EventsRepository(
    context: Context
) {
    private val sessionManager =
        SessionManager(
            context.applicationContext
        )

    private val gson =
        Gson()

    suspend fun getEvents():
            Result<List<EventResponse>> {

        val token =
            sessionManager.getToken()

        if (token.isNullOrBlank()) {
            return Result.failure(
                Exception(
                    "Your login session could not be found."
                )
            )
        }

        return try {
            val response =
                RetrofitClient
                    .eventsApi
                    .getEvents(
                        authorization =
                            "Bearer $token"
                    )

            if (response.isSuccessful) {
                Result.success(
                    response.body()
                        ?: emptyList()
                )
            } else {
                Result.failure(
                    Exception(
                        getErrorMessage(
                            response.code(),
                            response
                                .errorBody()
                                ?.string()
                        )
                    )
                )
            }
        } catch (exception: Exception) {
            Result.failure(
                Exception(
                    "Could not load events.",
                    exception
                )
            )
        }
    }

    suspend fun getMyRegistrations():
            Result<List<EventRegistrationResponse>> {

        val token =
            sessionManager.getToken()

        if (token.isNullOrBlank()) {
            return Result.failure(
                Exception(
                    "Your login session could not be found."
                )
            )
        }

        return try {
            val response =
                RetrofitClient
                    .eventsApi
                    .getMyRegistrations(
                        authorization =
                            "Bearer $token"
                    )

            if (response.isSuccessful) {
                Result.success(
                    response.body()
                        ?: emptyList()
                )
            } else {
                Result.failure(
                    Exception(
                        getErrorMessage(
                            response.code(),
                            response
                                .errorBody()
                                ?.string()
                        )
                    )
                )
            }
        } catch (exception: Exception) {
            Result.failure(
                Exception(
                    "Could not load event registrations.",
                    exception
                )
            )
        }
    }

    suspend fun registerForEvent(
        eventId: Int
    ): Result<EventRegistrationResponse> {

        val token =
            sessionManager.getToken()

        if (token.isNullOrBlank()) {
            return Result.failure(
                Exception(
                    "Your login session could not be found."
                )
            )
        }

        return try {
            val response =
                RetrofitClient
                    .eventsApi
                    .registerForEvent(
                        authorization =
                            "Bearer $token",
                        eventId =
                            eventId
                    )

            if (response.isSuccessful) {
                val body =
                    response.body()

                if (body != null) {
                    Result.success(body)
                } else {
                    Result.failure(
                        Exception(
                            "The registration response was empty."
                        )
                    )
                }
            } else {
                Result.failure(
                    Exception(
                        getErrorMessage(
                            response.code(),
                            response
                                .errorBody()
                                ?.string()
                        )
                    )
                )
            }
        } catch (exception: Exception) {
            Result.failure(
                Exception(
                    "Could not register for the event.",
                    exception
                )
            )
        }
    }

    suspend fun cancelRegistration(
        eventId: Int
    ): Result<EventRegistrationResponse> {

        val token =
            sessionManager.getToken()

        if (token.isNullOrBlank()) {
            return Result.failure(
                Exception(
                    "Your login session could not be found."
                )
            )
        }

        return try {
            val response =
                RetrofitClient
                    .eventsApi
                    .cancelRegistration(
                        authorization =
                            "Bearer $token",
                        eventId =
                            eventId
                    )

            if (response.isSuccessful) {
                val body =
                    response.body()

                if (body != null) {
                    Result.success(body)
                } else {
                    Result.failure(
                        Exception(
                            "The cancellation response was empty."
                        )
                    )
                }
            } else {
                Result.failure(
                    Exception(
                        getErrorMessage(
                            response.code(),
                            response
                                .errorBody()
                                ?.string()
                        )
                    )
                )
            }
        } catch (exception: Exception) {
            Result.failure(
                Exception(
                    "Could not cancel the event registration.",
                    exception
                )
            )
        }
    }

    private fun getErrorMessage(
        statusCode: Int,
        errorJson: String?
    ): String {

        if (!errorJson.isNullOrBlank()) {
            try {
                val error =
                    gson.fromJson(
                        errorJson,
                        ApiError::class.java
                    )

                if (
                    !error.message
                        .isNullOrBlank()
                ) {
                    return error.message
                }
            } catch (_: Exception) {
                // Use fallback message.
            }
        }

        return when (statusCode) {
            400 ->
                "The event request was invalid."

            401 ->
                "Your login session is no longer valid."

            403 ->
                "You do not have permission to perform this action."

            404 ->
                "The event could not be found."

            409 ->
                "The requested event action could not be completed."

            500 ->
                "The ParaVolley server encountered an error."

            else ->
                "Event request failed. Server returned $statusCode."
        }
    }

    private data class ApiError(
        val message: String?
    )
}