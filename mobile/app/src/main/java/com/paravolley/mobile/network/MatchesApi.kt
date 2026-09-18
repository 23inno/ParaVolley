package com.paravolley.mobile.network

import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.Path
import retrofit2.http.Query

interface MatchesApi {

    @GET("api/matches")
    suspend fun getMatches(
        @Header("Authorization")
        authorization: String,
        @Query("page")
        page: Int,
        @Query("pageSize")
        pageSize: Int
    ): Response<List<MatchResponse>>

    @GET("api/matches/upcoming")
    suspend fun getUpcomingMatches(
        @Header("Authorization")
        authorization: String,
        @Query("page")
        page: Int,
        @Query("pageSize")
        pageSize: Int
    ): Response<List<MatchResponse>>

    @GET("api/matches/{id}")
    suspend fun getMatch(
        @Header("Authorization")
        authorization: String,
        @Path("id")
        matchId: Int
    ): Response<MatchResponse>
}
