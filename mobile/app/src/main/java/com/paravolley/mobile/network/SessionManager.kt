package com.paravolley.mobile.network

import android.content.Context

class SessionManager(
    context: Context
) {
    private val preferences =
        context.getSharedPreferences(
            "paravolley_session",
            Context.MODE_PRIVATE
        )

    fun saveLogin(
        loginResponse: LoginResponse
    ) {
        preferences.edit()
            .putString(
                KEY_TOKEN,
                loginResponse.token
            )
            .putString(
                KEY_EXPIRES_AT,
                loginResponse.expiresAt
            )
            .putInt(
                KEY_USER_ID,
                loginResponse.user.id
            )
            .putString(
                KEY_EMAIL,
                loginResponse.user.email
            )
            .putString(
                KEY_ROLE,
                loginResponse.user.role
            )
            .putInt(
                KEY_PLAYER_ID,
                loginResponse.user.playerId
                    ?: NO_PLAYER_ID
            )
            .putString(
                KEY_PLAYER_NAME,
                loginResponse.user.playerName
            )
            .apply()
    }

    fun getToken(): String? {
        return preferences.getString(
            KEY_TOKEN,
            null
        )
    }

    fun getRole(): String? {
        return preferences.getString(
            KEY_ROLE,
            null
        )
    }

    fun getPlayerId(): Int? {
        val playerId =
            preferences.getInt(
                KEY_PLAYER_ID,
                NO_PLAYER_ID
            )

        return if (
            playerId == NO_PLAYER_ID
        ) {
            null
        } else {
            playerId
        }
    }

    fun getPlayerName(): String? {
        return preferences.getString(
            KEY_PLAYER_NAME,
            null
        )
    }

    fun isLoggedIn(): Boolean {
        return !getToken().isNullOrBlank()
    }

    fun clearSession() {
        preferences.edit()
            .clear()
            .apply()
    }

    companion object {
        private const val KEY_TOKEN =
            "token"

        private const val KEY_EXPIRES_AT =
            "expires_at"

        private const val KEY_USER_ID =
            "user_id"

        private const val KEY_EMAIL =
            "email"

        private const val KEY_ROLE =
            "role"

        private const val KEY_PLAYER_ID =
            "player_id"

        private const val KEY_PLAYER_NAME =
            "player_name"

        private const val NO_PLAYER_ID =
            -1
    }
}