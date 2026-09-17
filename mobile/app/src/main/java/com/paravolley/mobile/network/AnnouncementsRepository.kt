package com.paravolley.mobile.network

import android.content.Context
import com.google.gson.Gson

class AnnouncementsRepository(
    context: Context
) {
    private val appContext = context.applicationContext
    private val sessionManager = SessionManager(appContext)
    private val gson = Gson()
    private val readPreferences = appContext.getSharedPreferences(
        "announcement_read_state",
        Context.MODE_PRIVATE
    )

    suspend fun getAnnouncements(): Result<List<AnnouncementResponse>> {
        val authorization = authorizationHeader()
            ?: return missingSession()

        return try {
            val response = RetrofitClient.announcementsApi
                .getAnnouncements(authorization)

            if (response.isSuccessful) {
                val locallyReadIds = locallyReadAnnouncementIds()
                val announcements = (response.body() ?: emptyList()).map { announcement ->
                    if (announcement.isRead || announcement.id in locallyReadIds) {
                        announcement.copy(isRead = true)
                    } else {
                        announcement
                    }
                }
                Result.success(announcements)
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
                response.body()?.let { announcement ->
                    val isLocallyRead = announcement.id in locallyReadAnnouncementIds()
                    Result.success(
                        if (announcement.isRead || isLocallyRead) {
                            announcement.copy(isRead = true)
                        } else {
                            announcement
                        }
                    )
                } ?: Result.failure(
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
        rememberAnnouncementRead(announcementId)

        val authorization = authorizationHeader()
            ?: return Result.success(Unit)

        return try {
            val response = RetrofitClient.announcementsApi
                .markAnnouncementRead(
                    authorization = authorization,
                    announcementId = announcementId
                )

            if (response.isSuccessful || response.code() == 404) {
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
        } catch (_: Exception) {
            // The local read state is already saved, so the notification remains read
            // even if the server is temporarily unavailable.
            Result.success(Unit)
        }
    }

    suspend fun markAllAnnouncementsRead(
        announcementIds: Collection<Int>
    ): Result<Unit> {
        rememberAnnouncementsRead(announcementIds)

        val authorization = authorizationHeader()
            ?: return Result.success(Unit)

        return try {
            val response = RetrofitClient.announcementsApi
                .markAllAnnouncementsRead(authorization)

            if (response.isSuccessful || response.code() == 404) {
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
        } catch (_: Exception) {
            Result.success(Unit)
        }
    }

    private fun locallyReadAnnouncementIds(): Set<Int> {
        return readPreferences
            .getStringSet(READ_IDS_KEY, emptySet())
            .orEmpty()
            .mapNotNull(String::toIntOrNull)
            .toSet()
    }

    private fun rememberAnnouncementRead(announcementId: Int) {
        rememberAnnouncementsRead(listOf(announcementId))
    }

    private fun rememberAnnouncementsRead(announcementIds: Collection<Int>) {
        val ids = readPreferences
            .getStringSet(READ_IDS_KEY, emptySet())
            .orEmpty()
            .toMutableSet()

        announcementIds.forEach { ids.add(it.toString()) }

        readPreferences.edit()
            .putStringSet(READ_IDS_KEY, ids)
            .apply()
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

    companion object {
        private const val READ_IDS_KEY = "read_announcement_ids"
    }
}
