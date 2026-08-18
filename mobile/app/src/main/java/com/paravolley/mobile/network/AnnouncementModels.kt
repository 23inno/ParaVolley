package com.paravolley.mobile.network

data class AnnouncementResponse(
    val id: Int,
    val title: String,
    val excerpt: String,
    val content: String,
    val author: String,
    val date: String,
    val category: String,
    val isPinned: Boolean,
    val views: Int
)