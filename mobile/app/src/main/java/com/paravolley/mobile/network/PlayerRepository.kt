package com.paravolley.mobile.network

import android.content.Context
import android.net.Uri
import com.google.gson.Gson
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.toRequestBody

class PlayerRepository(
    context: Context
) {
    private val appContext = context.applicationContext

    private val sessionManager =
        SessionManager(appContext)

    private val gson = Gson()

    suspend fun getProfile(): Result<PlayerProfileResponse> {
        val authorization = authorizationHeader()
            ?: return missingSession()

        return try {
            val response = RetrofitClient.playerApi
                .getProfile(authorization)

            if (response.isSuccessful) {
                response.body()?.let(Result.Companion::success)
                    ?: Result.failure(
                        Exception("The player profile response was empty.")
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
                Exception(
                    "Could not load the player profile.",
                    exception
                )
            )
        }
    }

    suspend fun updateProfile(
        age: Int,
        email: String,
        phone: String
    ): Result<PlayerProfileResponse> {
        val authorization = authorizationHeader()
            ?: return missingSession()

        return try {
            val response = RetrofitClient.playerApi.updateProfile(
                authorization = authorization,
                request = UpdatePlayerProfileRequest(
                    age = age,
                    email = email.trim(),
                    phone = phone.trim()
                )
            )

            if (response.isSuccessful) {
                response.body()?.let(Result.Companion::success)
                    ?: Result.failure(
                        Exception("The updated player profile response was empty.")
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
                Exception(
                    "Could not save the player profile.",
                    exception
                )
            )
        }
    }

    suspend fun getProfilePhoto(): Result<ByteArray?> {
        val authorization = authorizationHeader()
            ?: return missingSession()

        return try {
            val response = RetrofitClient.playerApi
                .getProfilePhoto(authorization)

            when {
                response.isSuccessful -> {
                    Result.success(response.body()?.bytes())
                }

                response.code() == 404 -> Result.success(null)

                else -> Result.failure(
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
                Exception(
                    "Could not load the profile photo.",
                    exception
                )
            )
        }
    }

    suspend fun uploadProfilePhoto(
        uri: Uri
    ): Result<PlayerProfileResponse> {
        val authorization = authorizationHeader()
            ?: return missingSession()

        val contentResolver = appContext.contentResolver
        val mimeType = contentResolver.getType(uri)
            ?.lowercase()
            ?.substringBefore(';')

        val fileName = when (mimeType) {
            "image/jpeg", "image/jpg" -> "profile.jpg"
            "image/png" -> "profile.png"
            "image/webp" -> "profile.webp"
            else -> return Result.failure(
                Exception("Choose a JPG, PNG, or WEBP image.")
            )
        }

        val declaredSize = try {
            contentResolver.openFileDescriptor(uri, "r")
                ?.use { descriptor -> descriptor.statSize }
                ?: -1L
        } catch (_: Exception) {
            -1L
        }

        if (declaredSize > MAX_PROFILE_PHOTO_BYTES) {
            return Result.failure(
                Exception("Profile photos must be smaller than 2 MB.")
            )
        }

        val bytes = try {
            contentResolver.openInputStream(uri)
                ?.use { it.readBytes() }
                ?: return Result.failure(
                    Exception("The selected image could not be opened.")
                )
        } catch (exception: Exception) {
            return Result.failure(
                Exception("The selected image could not be opened.", exception)
            )
        }

        if (bytes.isEmpty()) {
            return Result.failure(
                Exception("The selected image is empty.")
            )
        }

        if (bytes.size > MAX_PROFILE_PHOTO_BYTES) {
            return Result.failure(
                Exception("Profile photos must be smaller than 2 MB.")
            )
        }

        return try {
            val requestBody = bytes.toRequestBody(
                (mimeType ?: "application/octet-stream").toMediaType()
            )
            val photoPart = MultipartBody.Part.createFormData(
                "photo",
                fileName,
                requestBody
            )

            val response = RetrofitClient.playerApi.uploadProfilePhoto(
                authorization = authorization,
                photo = photoPart
            )

            if (response.isSuccessful) {
                response.body()?.let(Result.Companion::success)
                    ?: Result.failure(
                        Exception("The photo upload response was empty.")
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
                Exception("Could not upload the profile photo.", exception)
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
                val error = gson.fromJson(
                    errorJson,
                    ApiError::class.java
                )

                if (!error.message.isNullOrBlank()) {
                    return error.message
                }
            } catch (_: Exception) {
                // Use the fallback message below.
            }
        }

        return when (statusCode) {
            400 -> "The profile information was invalid."
            401 -> "Your login session is no longer valid."
            403 -> "You do not have permission to update this profile."
            404 -> "The player profile could not be found."
            409 -> "That email address is already in use."
            413 -> "Profile photos must be smaller than 2 MB."
            429 -> "Too many requests. Please try again shortly."
            500 -> "The ParaVolley server encountered an error."
            else -> "Profile request failed. Server returned $statusCode."
        }
    }

    private data class ApiError(
        val message: String?
    )

    companion object {
        private const val MAX_PROFILE_PHOTO_BYTES = 2 * 1024 * 1024
    }
}
