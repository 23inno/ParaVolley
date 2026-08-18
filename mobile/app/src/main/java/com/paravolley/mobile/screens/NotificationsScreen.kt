package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.network.AnnouncementResponse
import com.paravolley.mobile.network.AnnouncementsRepository
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun NotificationsScreen(
    onBack: () -> Unit
) {
    val context =
        LocalContext.current

    val repository =
        remember {
            AnnouncementsRepository(
                context.applicationContext
            )
        }

    var announcements by remember {
        mutableStateOf<
            List<AnnouncementResponse>
        >(
            emptyList()
        )
    }

    var readAnnouncementIds by remember {
        mutableStateOf<Set<Int>>(
            emptySet()
        )
    }

    var isLoading by remember {
        mutableStateOf(true)
    }

    var errorMessage by remember {
        mutableStateOf<String?>(null)
    }

    LaunchedEffect(Unit) {
        isLoading = true
        errorMessage = null

        repository
            .getAnnouncements()
            .onSuccess { response ->
                announcements = response
                isLoading = false
            }
            .onFailure { exception ->
                errorMessage =
                    exception.message
                        ?: "Could not load announcements."

                isLoading = false
            }
    }

    val unreadCount =
        announcements.count {
            announcement ->
            !readAnnouncementIds.contains(
                announcement.id
            )
        }

    Column(
        modifier =
            Modifier
                .fillMaxSize()
                .background(
                    AppColors.LightBackground
                )
                .safeDrawingPadding()
    ) {
        Column(
            modifier =
                Modifier
                    .fillMaxWidth()
                    .background(
                        AppColors.DarkGreen
                    )
                    .padding(18.dp)
        ) {
            Button(
                onClick =
                    onBack,
                colors =
                    ButtonDefaults.buttonColors(
                        containerColor =
                            AppColors.Yellow,
                        contentColor =
                            AppColors.DarkText
                    )
            ) {
                Text("Back")
            }

            Text(
                modifier =
                    Modifier.padding(
                        top = 14.dp
                    ),
                text =
                    "Announcements",
                color =
                    Color.White,
                fontSize =
                    25.sp,
                fontWeight =
                    FontWeight.Bold
            )

            Text(
                text =
                    "$unreadCount unread on this device",
                color =
                    Color.White
            )
        }

        when {
            isLoading -> {
                Box(
                    modifier =
                        Modifier
                            .fillMaxWidth()
                            .padding(40.dp),
                    contentAlignment =
                        Alignment.Center
                ) {
                    CircularProgressIndicator()
                }
            }

            errorMessage != null -> {
                Box(
                    modifier =
                        Modifier
                            .fillMaxWidth()
                            .padding(24.dp),
                    contentAlignment =
                        Alignment.Center
                ) {
                    Text(
                        text =
                            errorMessage
                                ?: "Could not load announcements.",
                        color =
                            Color.Red
                    )
                }
            }

            announcements.isEmpty() -> {
                Box(
                    modifier =
                        Modifier
                            .fillMaxWidth()
                            .padding(32.dp),
                    contentAlignment =
                        Alignment.Center
                ) {
                    Text(
                        text =
                            "No announcements are available.",
                        color =
                            AppColors.GreyText
                    )
                }
            }

            else -> {
                LazyColumn(
                    modifier =
                        Modifier.weight(1f),
                    contentPadding =
                        PaddingValues(
                            16.dp
                        ),
                    verticalArrangement =
                        Arrangement.spacedBy(
                            10.dp
                        )
                ) {
                    item {
                        Button(
                            modifier =
                                Modifier
                                    .fillMaxWidth(),
                            onClick = {
                                readAnnouncementIds =
                                    announcements
                                        .map {
                                            announcement ->
                                            announcement.id
                                        }
                                        .toSet()
                            },
                            colors =
                                ButtonDefaults
                                    .buttonColors(
                                        containerColor =
                                            AppColors.Yellow,
                                        contentColor =
                                            AppColors.DarkText
                                    )
                        ) {
                            Text(
                                "Mark all as read"
                            )
                        }
                    }

                    items(
                        items =
                            announcements,
                        key = {
                            announcement ->
                            announcement.id
                        }
                    ) { announcement ->

                        val isRead =
                            readAnnouncementIds
                                .contains(
                                    announcement.id
                                )

                        Card(
                            modifier =
                                Modifier.fillMaxWidth(),
                            colors =
                                CardDefaults
                                    .cardColors(
                                        containerColor =
                                            if (isRead) {
                                                Color.White
                                            } else {
                                                AppColors.UnreadBlue
                                            }
                                    )
                        ) {
                            Column(
                                modifier =
                                    Modifier.padding(
                                        16.dp
                                    )
                            ) {
                                Row(
                                    modifier =
                                        Modifier
                                            .fillMaxWidth(),
                                    horizontalArrangement =
                                        Arrangement
                                            .SpaceBetween
                                ) {
                                    Text(
                                        text =
                                            announcement.title,
                                        color =
                                            AppColors.DarkGreen,
                                        fontWeight =
                                            FontWeight.Bold
                                    )

                                    if (!isRead) {
                                        Text(
                                            text =
                                                "NEW",
                                            color =
                                                AppColors.Green,
                                            fontWeight =
                                                FontWeight.Bold
                                        )
                                    }
                                }

                                if (
                                    announcement.isPinned
                                ) {
                                    Text(
                                        modifier =
                                            Modifier.padding(
                                                top = 5.dp
                                            ),
                                        text =
                                            "PINNED",
                                        color =
                                            AppColors.Green,
                                        fontWeight =
                                            FontWeight.Bold
                                    )
                                }

                                Text(
                                    modifier =
                                        Modifier.padding(
                                            top = 6.dp
                                        ),
                                    text =
                                        announcement.excerpt
                                )

                                if (
                                    announcement.content
                                        .isNotBlank()
                                ) {
                                    Text(
                                        modifier =
                                            Modifier.padding(
                                                top = 8.dp
                                            ),
                                        text =
                                            announcement.content
                                    )
                                }

                                Text(
                                    modifier =
                                        Modifier.padding(
                                            top = 8.dp
                                        ),
                                    text =
                                        "Category: ${announcement.category}",
                                    color =
                                        AppColors.GreyText
                                )

                                Text(
                                    modifier =
                                        Modifier.padding(
                                            top = 4.dp
                                        ),
                                    text =
                                        "Author: ${announcement.author}",
                                    color =
                                        AppColors.GreyText
                                )

                                Text(
                                    modifier =
                                        Modifier.padding(
                                            top = 4.dp
                                        ),
                                    text =
                                        "Date: ${announcement.date}",
                                    color =
                                        AppColors.GreyText
                                )

                                Text(
                                    modifier =
                                        Modifier.padding(
                                            top = 4.dp
                                        ),
                                    text =
                                        "Views: ${announcement.views}",
                                    color =
                                        AppColors.GreyText
                                )

                                if (!isRead) {
                                    Button(
                                        modifier =
                                            Modifier
                                                .fillMaxWidth()
                                                .padding(
                                                    top = 10.dp
                                                ),
                                        onClick = {
                                            readAnnouncementIds =
                                                readAnnouncementIds +
                                                    announcement.id
                                        },
                                        colors =
                                            ButtonDefaults
                                                .buttonColors(
                                                    containerColor =
                                                        AppColors.DarkGreen
                                                )
                                    ) {
                                        Text(
                                            "Mark as read"
                                        )
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}