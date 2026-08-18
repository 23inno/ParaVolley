package com.paravolley.mobile.network

import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.Path

interface AnnouncementsApi {

    @GET("api/announcements")
    suspend fun getAnnouncements(
        @Header("Authorization")
        authorization: String
    ): Response<List<AnnouncementResponse>>

    @GET("api/announcements/{id}")
    suspend fun getAnnouncement(
        @Header("Authorization")
        authorization: String,
        @Path("id")
        announcementId: Int
    ): Response<AnnouncementResponse>
}