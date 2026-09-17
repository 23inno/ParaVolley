package com.paravolley.mobile.network

import android.content.Context
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.graphics.ImageDecoder
import android.net.Uri
import android.os.Build
import com.google.gson.Gson
import java.io.ByteArrayOutputStream
import kotlin.math.roundToInt
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
        phone: String,
        emergencyContactName: String,
        emergencyContactPhone: String
    ): Result<PlayerProfileResponse> {
        val authorization = authorizationHeader()
            ?: return missingSession()

        return try {
            val response = RetrofitClient.playerApi.updateProfile(
                authorization = authorization,
                request = UpdatePlayerProfileRequest(
                    age = age,
                    email = email.trim(),
                    phone = phone.trim(),
                    emergencyContactName = emergencyContactName.trim(),
                    emergencyContactPhone = emergencyContactPhone.trim()
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

        val normalizedPhoto = normalizeProfilePhoto(uri)
            .getOrElse { failure ->
                return Result.failure(failure)
            }

        return try {
            val requestBody = normalizedPhoto.toRequestBody(
                "image/jpeg".toMediaType()
            )
            val photoPart = MultipartBody.Part.createFormData(
                "photo",
                "profile.jpg",
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

    private fun normalizeProfilePhoto(uri: Uri): Result<ByteArray> {
        val contentResolver = appContext.contentResolver

        val declaredSize = try {
            contentResolver.openFileDescriptor(uri, "r")
                ?.use { descriptor -> descriptor.statSize }
                ?: -1L
        } catch (_: Exception) {
            -1L
        }

        if (declaredSize > MAX_SOURCE_PHOTO_BYTES) {
            return Result.failure(
                Exception("Choose an image smaller than 12 MB.")
            )
        }

        val bitmap = try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
                val source = ImageDecoder.createSource(contentResolver, uri)
                ImageDecoder.decodeBitmap(source) { decoder, info, _ ->
                    decoder.allocator = ImageDecoder.ALLOCATOR_SOFTWARE

                    val sourceWidth = info.size.width
                    val sourceHeight = info.size.height
                    val largestSide = maxOf(sourceWidth, sourceHeight)

                    if (largestSide > PROFILE_PHOTO_MAX_DIMENSION) {
                        val scale = PROFILE_PHOTO_MAX_DIMENSION.toFloat() /
                            largestSide.toFloat()
                        decoder.setTargetSize(
                            (sourceWidth * scale).roundToInt().coerceAtLeast(1),
                            (sourceHeight * scale).roundToInt().coerceAtLeast(1)
                        )
                    }
                }
            } else {
                contentResolver.openInputStream(uri)
                    ?.use(BitmapFactory::decodeStream)
            }
        } catch (exception: Exception) {
            return Result.failure(
                Exception("The selected image could not be opened.", exception)
            )
        } ?: return Result.failure(
            Exception("The selected image could not be opened.")
        )

        val scaledBitmap = if (
            bitmap.width > PROFILE_PHOTO_MAX_DIMENSION ||
            bitmap.height > PROFILE_PHOTO_MAX_DIMENSION
        ) {
            val largestSide = maxOf(bitmap.width, bitmap.height)
            val scale = PROFILE_PHOTO_MAX_DIMENSION.toFloat() /
                largestSide.toFloat()
            Bitmap.createScaledBitmap(
                bitmap,
                (bitmap.width * scale).roundToInt().coerceAtLeast(1),
                (bitmap.height * scale).roundToInt().coerceAtLeast(1),
                true
            )
        } else {
            bitmap
        }

        var quality = 90
        var bytes: ByteArray

        do {
            val stream = ByteArrayOutputStream()
            scaledBitmap.compress(
                Bitmap.CompressFormat.JPEG,
                quality,
                stream
            )
            bytes = stream.toByteArray()
            quality -= 10
        } while (
            bytes.size > MAX_PROFILE_PHOTO_BYTES &&
            quality >= 50
        )

        if (bytes.isEmpty() || bytes.size > MAX_PROFILE_PHOTO_BYTES) {
            return Result.failure(
                Exception("The selected image could not be reduced below 2 MB.")
            )
        }

        return Result.success(bytes)
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
        private const val MAX_SOURCE_PHOTO_BYTES = 12 * 1024 * 1024
        private const val PROFILE_PHOTO_MAX_DIMENSION = 1024
    }
}
