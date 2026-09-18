package com.paravolley.mobile.network

import android.content.Context
import com.google.gson.Gson

class MatchesRepository(
    context: Context
) {
    private val sessionManager =
        SessionManager(
            context.applicationContext
        )

    private val gson = Gson()

    suspend fun getMatches():
            Result<List<MatchResponse>> {

        val token = sessionManager.getToken()

        if (token.isNullOrBlank()) {
            return Result.failure(
                Exception(
                    "Your login session could not be found."
                )
            )
        }

        return try {
            val matches = mutableListOf<MatchResponse>()
            var page = 1

            while (true) {
                val response =
                    RetrofitClient
                        .matchesApi
                        .getMatches(
                            authorization = "Bearer $token",
                            page = page,
                            pageSize = PAGE_SIZE
                        )

                if (!response.isSuccessful) {
                    return Result.failure(
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

                val pageItems =
                    response.body()
                        ?: emptyList()

                matches.addAll(pageItems)

                if (pageItems.size < PAGE_SIZE) {
                    break
                }

                page++
            }

            Result.success(matches)
        } catch (exception: Exception) {
            Result.failure(
                Exception(
                    "Could not load match results.",
                    exception
                )
            )
        }
    }

    suspend fun getUpcomingMatches():
            Result<List<MatchResponse>> {

        val token = sessionManager.getToken()

        if (token.isNullOrBlank()) {
            return Result.failure(
                Exception(
                    "Your login session could not be found."
                )
            )
        }

        return try {
            val matches = mutableListOf<MatchResponse>()
            var page = 1

            while (true) {
                val response =
                    RetrofitClient
                        .matchesApi
                        .getUpcomingMatches(
                            authorization = "Bearer $token",
                            page = page,
                            pageSize = PAGE_SIZE
                        )

                if (!response.isSuccessful) {
                    return Result.failure(
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

                val pageItems =
                    response.body()
                        ?: emptyList()

                matches.addAll(pageItems)

                if (pageItems.size < PAGE_SIZE) {
                    break
                }

                page++
            }

            Result.success(matches)
        } catch (exception: Exception) {
            Result.failure(
                Exception(
                    "Could not load upcoming matches.",
                    exception
                )
            )
        }
    }

    suspend fun getMatch(
        matchId: Int
    ): Result<MatchResponse> {

        val token = sessionManager.getToken()

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
                    .matchesApi
                    .getMatch(
                        authorization = "Bearer $token",
                        matchId = matchId
                    )

            if (response.isSuccessful) {
                val body = response.body()

                if (body != null) {
                    Result.success(body)
                } else {
                    Result.failure(
                        Exception(
                            "The match response was empty."
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
                    "Could not load the match.",
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

                if (!error.message.isNullOrBlank()) {
                    return error.message
                }
            } catch (_: Exception) {
                // Use fallback message.
            }
        }

        return when (statusCode) {
            400 ->
                "The match request was invalid."

            401 ->
                "Your login session is no longer valid."

            403 ->
                "You do not have permission to view matches."

            404 ->
                "The match could not be found."

            500 ->
                "The ParaVolley server encountered an error."

            else ->
                "Match request failed. Server returned $statusCode."
        }
    }

    private data class ApiError(
        val message: String?
    )

    private companion object {
        const val PAGE_SIZE = 200
    }
}
