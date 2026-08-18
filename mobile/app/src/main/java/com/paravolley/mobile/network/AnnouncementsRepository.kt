package com.paravolley.mobile.network

import android.content.Context
import com.google.gson.Gson

class AnnouncementsRepository(
    context: Context
) {
    private val sessionManager =
        SessionManager(
            context.applicationContext
        )

    private val gson =
        Gson()

    suspend fun getAnnouncements():
            Result<List<AnnouncementResponse>> {

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
                    .announcementsApi
                    .getAnnouncements(
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
                            statusCode =
                                response.code(),
                            errorJson =
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
                    "Could not load announcements.",
                    exception
                )
            )
        }
    }

    suspend fun getAnnouncement(
        announcementId: Int
    ): Result<AnnouncementResponse> {

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
                    .announcementsApi
                    .getAnnouncement(
                        authorization =
                            "Bearer $token",
                        announcementId =
                            announcementId
                    )

            if (response.isSuccessful) {
                val body =
                    response.body()

                if (body != null) {
                    Result.success(body)
                } else {
                    Result.failure(
                        Exception(
                            "The announcement response was empty."
                        )
                    )
                }
            } else {
                Result.failure(
                    Exception(
                        getErrorMessage(
                            statusCode =
                                response.code(),
                            errorJson =
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
                    "Could not load the announcement.",
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
                // Use fallback below.
            }
        }

        return when (statusCode) {
            400 ->
                "The announcement request was invalid."

            401 ->
                "Your login session is no longer valid."

            403 ->
                "You do not have permission to view announcements."

            404 ->
                "The announcement could not be found."

            500 ->
                "The ParaVolley server encountered an error."

            else ->
                "Announcement request failed. Server returned $statusCode."
        }
    }

    private data class ApiError(
        val message: String?
    )
}