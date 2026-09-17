package com.paravolley.mobile.network

import android.content.Context
import com.google.gson.Gson

class AnnouncementsRepository(
    context: Context
) {
    private val sessionManager = SessionManager(context.applicationContext)
    private val gson = Gson()

    suspend fun getAnnouncements(): Result<List<AnnouncementResponse>> {
        val authorization = authorizationHeader()
            ?: return missingSession()

        return try {
            val response = RetrofitClient.announcementsApi
                .getAnnouncements(authorization)

            if (response.isSuccessful) {
                Result.success(response.body() ?: emptyList())
            } else {
                Result.failure(
                    Exception(
                        getErrorMessage(
                            response.code(),
                            response.errorBody()?.string()
                        )
                    )
                )
            }
        } catch (exception: Exception) {
            Result.failure(
                Exception("Could not load announcements.", exception)
            )
        }
    }

    suspend fun getAnnouncement(
        announcementId: Int
    ): Result<AnnouncementResponse> {
        val authorization = authorizationHeader()
            ?: return missingSession()

        return try {
            val response = RetrofitClient.announcementsApi
                .getAnnouncement(
                    authorization = authorization,
                    announcementId = announcementId
                )

            if (response.isSuccessful) {
                response.body()?.let(Result.Companion::success)
                    ?: Result.failure(
                        Exception("The announcement response was empty.")
                    )
            } else {
                Result.failure(
                    Exception(
                        getErrorMessage(
                            response.code(),
                            response.errorBody()?.string()
                        )
                    )
                )
            }
        } catch (exception: Exception) {
            Result.failure(
                Exception("Could not load the announcement.", exception)
            )
        }
    }

    suspend fun markAnnouncementRead(
        announcementId: Int
    ): Result<Unit> {
        val authorization = authorizationHeader()
            ?: return missingSession()

        return try {
            val response = RetrofitClient.announcementsApi
                .markAnnouncementRead(
                    authorization = authorization,
                    announcementId = announcementId
                )

            if (response.isSuccessful) {
                Result.success(Unit)
            } else {
                Result.failure(
                    Exception(
                        getErrorMessage(
                            response.code(),
                            response.errorBody()?.string()
                        )
                    )
                )
            }
        } catch (exception: Exception) {
            Result.failure(
                Exception("Could not mark the notification as read.", exception)
            )
        }
    }

    suspend fun markAllAnnouncementsRead(): Result<Unit> {
        val authorization = authorizationHeader()
            ?: return missingSession()

        return try {
            val response = RetrofitClient.announcementsApi
                .markAllAnnouncementsRead(authorization)

            if (response.isSuccessful) {
                Result.success(Unit)
            } else {
                Result.failure(
                    Exception(
                        getErrorMessage(
                            response.code(),
                            response.errorBody()?.string()
                        )
                    )
                )
            }
        } catch (exception: Exception) {
            Result.failure(
                Exception("Could not mark all notifications as read.", exception)
            )
        }
    }

    private fun authorizationHeader(): String? {
        val token = sessionManager.getToken()
        return if (token.isNullOrBlank()) null else "Bearer $token"
    }

    private fun <T> missingSession(): Result<T> {
        return Result.failure(
            Exception("Your login session could not be found.")
        )
    }

    private fun getErrorMessage(
        statusCode: Int,
        errorJson: String?
    ): String {
        if (!errorJson.isNullOrBlank()) {
            try {
                val error = gson.fromJson(errorJson, ApiError::class.java)
                if (!error.message.isNullOrBlank()) {
                    return error.message
                }
            } catch (_: Exception) {
                // Use the fallback message below.
            }
        }

        return when (statusCode) {
            400 -> "The announcement request was invalid."
            401 -> "Your login session is no longer valid."
            403 -> "You do not have permission to view announcements."
            404 -> "The announcement could not be found."
            429 -> "Too many requests. Please try again shortly."
            500 -> "The ParaVolley server encountered an error."
            else -> "Announcement request failed. Server returned $statusCode."
        }
    }

    private data class ApiError(
        val message: String?
    )
}
