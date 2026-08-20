package com.paravolley.mobile.network

import com.google.gson.Gson
import com.google.gson.JsonParser

class AuthRepository {

    private val gson = Gson()

    suspend fun login(
        email: String,
        password: String
    ): Result<LoginResponse> {

        return try {
            val response =
                RetrofitClient.authApi.login(
                    LoginRequest(
                        email = email.trim(),
                        password = password
                    )
                )

            if (response.isSuccessful) {
                val body = response.body()

                if (body != null) {
                    Result.success(body)
                } else {
                    Result.failure(
                        Exception(
                            "The login response was empty."
                        )
                    )
                }
            } else {
                val errorMessage =
                    getErrorMessage(
                        response.code(),
                        response.errorBody()?.string()
                    )

                Result.failure(
                    Exception(errorMessage)
                )
            }
        } catch (exception: Exception) {
            Result.failure(
                Exception(
                    "Could not connect to the ParaVolley server.",
                    exception
                )
            )
        }
    }

    suspend fun registerPlayer(
        request: RegisterPlayerRequest
    ): Result<RegisterPlayerResponse> {
        return try {
            val response =
                RetrofitClient.authApi
                    .registerPlayer(request)

            if (response.isSuccessful) {
                response.body()?.let {
                    Result.success(it)
                } ?: Result.failure(
                    Exception(
                        "The registration response was empty."
                    )
                )
            } else {
                Result.failure(
                    Exception(
                        getRegistrationErrorMessage(
                            response.code(),
                            response.errorBody()?.string()
                        )
                    )
                )
            }
        } catch (exception: Exception) {
            Result.failure(
                Exception(
                    "Could not connect to the ParaVolley server.",
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
                val apiError =
                    gson.fromJson(
                        errorJson,
                        ApiError::class.java
                    )

                if (
                    !apiError.message
                        .isNullOrBlank()
                ) {
                    return apiError.message
                }
            } catch (_: Exception) {
                // Fall back to the status-code message.
            }
        }

        return when (statusCode) {
            400 ->
                "The login request was invalid."

            401 ->
                "Invalid email or password."

            403 ->
                "This account is not allowed to use the player app."

            404 ->
                "The login service could not be found."

            500 ->
                "The ParaVolley server encountered an error."

            else ->
                "Login failed. Server returned $statusCode."
        }
    }

    private fun getRegistrationErrorMessage(
        statusCode: Int,
        errorJson: String?
    ): String {
        if (!errorJson.isNullOrBlank()) {
            try {
                val root = JsonParser
                    .parseString(errorJson)
                    .asJsonObject

                root.get("message")?.let {
                    return it.asString
                }

                root.getAsJsonObject("errors")
                    ?.entrySet()
                    ?.firstOrNull()
                    ?.value
                    ?.asJsonArray
                    ?.firstOrNull()
                    ?.let {
                        return it.asString
                    }
            } catch (_: Exception) {
                // Fall back to a safe status-code message.
            }
        }

        return when (statusCode) {
            400 -> "Check the registration details and try again."
            409 -> "An account already exists with this email address."
            404 -> "The registration service could not be found."
            500 -> "The ParaVolley server encountered an error."
            else -> "Registration failed. Server returned $statusCode."
        }
    }

    private data class ApiError(
        val message: String?
    )
}
