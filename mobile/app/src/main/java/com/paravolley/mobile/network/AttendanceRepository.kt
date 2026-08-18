package com.paravolley.mobile.network

import android.content.Context
import com.google.gson.Gson

class AttendanceRepository(
    context: Context
) {
    private val sessionManager =
        SessionManager(
            context.applicationContext
        )

    private val gson =
        Gson()

    suspend fun getMyAttendance():
            Result<List<AttendanceResponse>> {

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
                    .attendanceApi
                    .getMyAttendance(
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
                    "Could not load attendance history.",
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
                "The attendance request was invalid."

            401 ->
                "Your login session is no longer valid."

            403 ->
                "You do not have permission to view attendance."

            404 ->
                "The player attendance record could not be found."

            500 ->
                "The ParaVolley server encountered an error."

            else ->
                "Attendance request failed. Server returned $statusCode."
        }
    }

    private data class ApiError(
        val message: String?
    )
}