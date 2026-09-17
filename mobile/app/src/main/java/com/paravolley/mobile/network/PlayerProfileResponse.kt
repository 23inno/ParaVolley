package com.paravolley.mobile.network

import com.google.gson.annotations.SerializedName

data class PlayerProfileResponse(
    val id: Int,
    val name: String,
    val position: String,
    val team: String,
    val status: String,
    val age: Int,
    val matches: Int,
    val email: String,
    val phone: String,
    @SerializedName("emergencyContactName")
    private val emergencyContactNameValue: String? = null,
    @SerializedName("emergencyContactPhone")
    private val emergencyContactPhoneValue: String? = null,
    val joinedDate: String? = null,
    val disability: String,
    val hasProfilePhoto: Boolean = false
) {
    val emergencyContactName: String
        get() = emergencyContactNameValue.orEmpty()

    val emergencyContactPhone: String
        get() = emergencyContactPhoneValue.orEmpty()
}

data class UpdatePlayerProfileRequest(
    val age: Int,
    val email: String,
    val phone: String,
    val emergencyContactName: String,
    val emergencyContactPhone: String
)
