package com.paravolley.mobile.network

import android.content.Context
import com.google.gson.Gson

class QrAttendanceRepository(
    context: Context
) {
    private val sessionManager =
        SessionManager(
            context.applicationContext
        )

    private val gson =
        Gson()

    suspend fun checkIn(
        token: String
    ): Result<QrCheckInResponse> {

        val cleanToken =
            token.trim()

        if (cleanToken.isBlank()) {
            return Result.failure(
                Exception(
                    "Enter or scan a QR attendance code."
                )
            )
        }

        val accessToken =
            sessionManager.getToken()

        if (accessToken.isNullOrBlank()) {
            return Result.failure(
                Exception(
                    "Your login session could not be found."
                )
            )
        }

        return try {
            val response =
                RetrofitClient
                    .qrAttendanceApi
                    .checkIn(
                        authorization =
                            "Bearer $accessToken",
                        request =
                            QrCheckInRequest(
                                token =
                                    cleanToken
                            )
                    )

            if (response.isSuccessful) {
                val body =
                    response.body()

                if (body != null) {
                    Result.success(body)
                } else {
                    Result.failure(
                        Exception(
                            "The check-in response was empty."
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
                    "Could not complete QR check-in.",
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
                "The QR attendance code is invalid."

            401 ->
                "Your login session is no longer valid."

            403 ->
                "You do not have permission to check in."

            404 ->
                "The player or event could not be found."

            409 ->
                "The QR check-in could not be completed."

            500 ->
                "The ParaVolley server encountered an error."

            else ->
                "QR check-in failed. Server returned $statusCode."
        }
    }

    private data class ApiError(
        val message: String?
    )
}