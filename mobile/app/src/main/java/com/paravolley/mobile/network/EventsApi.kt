package com.paravolley.mobile.network

import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.Path

interface EventsApi {

    @GET("api/events")
    suspend fun getEvents(
        @Header("Authorization")
        authorization: String
    ): Response<List<EventResponse>>

    @GET("api/events/{id}")
    suspend fun getEvent(
        @Header("Authorization")
        authorization: String,
        @Path("id")
        eventId: Int
    ): Response<EventResponse>

    @GET("api/player/registrations")
    suspend fun getMyRegistrations(
        @Header("Authorization")
        authorization: String
    ): Response<List<EventRegistrationResponse>>

    @POST("api/events/{eventId}/register")
    suspend fun registerForEvent(
        @Header("Authorization")
        authorization: String,
        @Path("eventId")
        eventId: Int
    ): Response<EventRegistrationResponse>

    @POST(
        "api/events/{eventId}/cancel-registration"
    )
    suspend fun cancelRegistration(
        @Header("Authorization")
        authorization: String,
        @Path("eventId")
        eventId: Int
    ): Response<EventRegistrationResponse>
}