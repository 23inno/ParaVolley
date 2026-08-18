package com.paravolley.mobile.network

import android.content.Context
import com.google.gson.Gson

class PlayerRepository(
    context: Context
) {
    private val sessionManager =
        SessionManager(
            context.applicationContext
        )

    private val gson = Gson()

    suspend fun getProfile():
            Result<PlayerProfileResponse> {

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
                RetrofitClient.playerApi
                    .getProfile(
                        authorization =
                            "Bearer $token"
                    )

            if (response.isSuccessful) {
                val body =
                    response.body()

                if (body != null) {
                    Result.success(body)
                } else {
                    Result.failure(
                        Exception(
                            "The player profile response was empty."
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
                    "Could not load the player profile.",
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
                // Use fallback message below.
            }
        }

        return when (statusCode) {
            400 ->
                "The profile request was invalid."

            401 ->
                "Your login session is no longer valid."

            403 ->
                "You do not have permission to view this profile."

            404 ->
                "The player profile could not be found."

            500 ->
                "The ParaVolley server encountered an error."

            else ->
                "Profile request failed. Server returned $statusCode."
        }
    }

    private data class ApiError(
        val message: String?
    )
}