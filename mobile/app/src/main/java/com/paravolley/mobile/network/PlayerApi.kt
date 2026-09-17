package com.paravolley.mobile.network

import okhttp3.MultipartBody
import okhttp3.ResponseBody
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.Multipart
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Part

interface PlayerApi {

    @GET("api/player/me")
    suspend fun getProfile(
        @Header("Authorization")
        authorization: String
    ): Response<PlayerProfileResponse>

    @PUT("api/player/me")
    suspend fun updateProfile(
        @Header("Authorization")
        authorization: String,
        @Body request: UpdatePlayerProfileRequest
    ): Response<PlayerProfileResponse>

    @GET("api/player/me/photo")
    suspend fun getProfilePhoto(
        @Header("Authorization")
        authorization: String
    ): Response<ResponseBody>

    @Multipart
    @POST("api/player/me/photo")
    suspend fun uploadProfilePhoto(
        @Header("Authorization")
        authorization: String,
        @Part photo: MultipartBody.Part
    ): Response<PlayerProfileResponse>
}
