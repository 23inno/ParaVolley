package com.paravolley.mobile.screens

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Campaign
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.ChevronRight
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.PushPin
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Surface
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
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.network.AnnouncementResponse
import com.paravolley.mobile.network.AnnouncementsRepository
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun NotificationsScreen(
    onBack: () -> Unit
) {
    val context = LocalContext.current
    val repository = remember { AnnouncementsRepository(context.applicationContext) }

    var announcements by remember { mutableStateOf<List<AnnouncementResponse>>(emptyList()) }
    var readAnnouncementIds by remember { mutableStateOf<Set<Int>>(emptySet()) }
    var selectedAnnouncement by remember { mutableStateOf<AnnouncementResponse?>(null) }
    var isLoading by remember { mutableStateOf(true) }
    var errorMessage by remember { mutableStateOf<String?>(null) }

    LaunchedEffect(Unit) {
        isLoading = true
        errorMessage = null
        repository.getAnnouncements()
            .onSuccess {
                announcements = it
                isLoading = false
            }
            .onFailure {
                errorMessage = it.message ?: "Could not load announcements."
                isLoading = false
            }
    }

    val unreadCount = announcements.count { it.id !in readAnnouncementIds }

    selectedAnnouncement?.let { announcement ->
        AnnouncementDetail(
            announcement = announcement,
            onBack = { selectedAnnouncement = null }
        )
        return
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.White)
            .safeDrawingPadding()
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 6.dp, vertical = 8.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                IconButton(onClick = onBack) {
                    Icon(
                        imageVector = Icons.Filled.ArrowBack,
                        contentDescription = "Back",
                        tint = AppColors.DarkText
                    )
                }
                Text(
                    text = "Notifications",
                    color = AppColors.Green,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 20.sp
                )
            }

            if (unreadCount > 0) {
                Surface(
                    color = AppColors.Yellow,
                    shape = RoundedCornerShape(999.dp)
                ) {
                    Text(
                        modifier = Modifier.padding(horizontal = 12.dp, vertical = 5.dp),
                        text = "$unreadCount new",
                        color = AppColors.DarkText,
                        fontWeight = FontWeight.Medium,
                        fontSize = 12.sp
                    )
                }
            }
        }

        HorizontalDivider(color = AppColors.Border)

        when {
            isLoading -> Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator(color = AppColors.Green)
            }

            errorMessage != null -> Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(24.dp),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = errorMessage ?: "Could not load announcements.",
                    color = AppColors.Error
                )
            }

            announcements.isEmpty() -> EmptyNotificationsState()

            else -> {
                LazyColumn(
                    modifier = Modifier.weight(1f),
                    contentPadding = PaddingValues(vertical = 4.dp)
                ) {
                    items(announcements, key = { it.id }) { announcement ->
                        val isRead = announcement.id in readAnnouncementIds
                        AnnouncementNotificationRow(
                            announcement = announcement,
                            isRead = isRead,
                            onClick = {
                                readAnnouncementIds = readAnnouncementIds + announcement.id
                                selectedAnnouncement = announcement
                            }
                        )
                        HorizontalDivider(
                            modifier = Modifier.padding(start = 68.dp),
                            color = Color(0xFFF3F4F6)
                        )
                    }
                }

                if (unreadCount > 0) {
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .background(Color.White)
                    ) {
                        HorizontalDivider(color = AppColors.Border)
                        Button(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(horizontal = 24.dp, vertical = 14.dp)
                                .height(48.dp),
                            onClick = {
                                readAnnouncementIds = announcements.map { it.id }.toSet()
                            },
                            colors = ButtonDefaults.buttonColors(
                                containerColor = AppColors.Green,
                                contentColor = Color.White
                            ),
                            shape = RoundedCornerShape(10.dp)
                        ) {
                            Icon(
                                imageVector = Icons.Filled.Check,
                                contentDescription = null,
                                modifier = Modifier.size(18.dp)
                            )
                            Spacer(Modifier.width(7.dp))
                            Text(
                                text = "Mark all as read",
                                fontWeight = FontWeight.SemiBold,
                                fontSize = 13.sp
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun AnnouncementNotificationRow(
    announcement: AnnouncementResponse,
    isRead: Boolean,
    onClick: () -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        onClick = onClick,
        shape = RoundedCornerShape(0.dp),
        colors = CardDefaults.cardColors(
            containerColor = if (isRead) Color.White else AppColors.LightGreen.copy(alpha = 0.45f)
        ),
        elevation = CardDefaults.cardElevation(defaultElevation = 0.dp)
    ) {
        Row(
            modifier = Modifier.padding(horizontal = 24.dp, vertical = 15.dp),
            verticalAlignment = Alignment.Top
        ) {
            Box(
                modifier = Modifier
                    .size(40.dp)
                    .background(AppColors.LightGreen, CircleShape),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Filled.Campaign,
                    contentDescription = null,
                    tint = AppColors.Green,
                    modifier = Modifier.size(20.dp)
                )
            }

            Spacer(Modifier.width(12.dp))

            Column(modifier = Modifier.weight(1f)) {
                Row(verticalAlignment = Alignment.Top) {
                    Text(
                        modifier = Modifier.weight(1f),
                        text = announcement.title,
                        color = AppColors.DarkText,
                        fontWeight = if (isRead) FontWeight.Medium else FontWeight.Bold,
                        fontSize = 14.sp,
                        maxLines = 2,
                        overflow = TextOverflow.Ellipsis
                    )
                    if (!isRead) {
                        Spacer(Modifier.width(8.dp))
                        Box(
                            modifier = Modifier
                                .padding(top = 5.dp)
                                .size(8.dp)
                                .background(AppColors.Yellow, CircleShape)
                        )
                    }
                }

                Spacer(Modifier.height(4.dp))

                Text(
                    text = announcement.excerpt,
                    color = AppColors.GreyText,
                    fontSize = 13.sp,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )

                Spacer(Modifier.height(5.dp))

                Text(
                    text = announcement.date,
                    color = Color(0xFF9CA3AF),
                    fontSize = 11.sp
                )
            }

            Spacer(Modifier.width(7.dp))

            Icon(
                imageVector = Icons.Filled.ChevronRight,
                contentDescription = null,
                tint = Color(0xFFD1D5DB),
                modifier = Modifier.size(18.dp)
            )
        }
    }
}

@Composable
private fun AnnouncementDetail(
    announcement: AnnouncementResponse,
    onBack: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.White)
            .safeDrawingPadding()
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 6.dp, vertical = 8.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = onBack) {
                Icon(
                    imageVector = Icons.Filled.ArrowBack,
                    contentDescription = "Back",
                    tint = AppColors.DarkText
                )
            }
            Text(
                text = "Notification",
                color = AppColors.Green,
                fontWeight = FontWeight.SemiBold,
                fontSize = 20.sp
            )
        }

        HorizontalDivider(color = AppColors.Border)

        LazyColumn(
            modifier = Modifier.weight(1f),
            contentPadding = PaddingValues(horizontal = 24.dp, vertical = 24.dp),
            verticalArrangement = Arrangement.spacedBy(18.dp)
        ) {
            item {
                Surface(
                    color = AppColors.LightGreen,
                    shape = RoundedCornerShape(999.dp)
                ) {
                    Text(
                        modifier = Modifier.padding(horizontal = 11.dp, vertical = 5.dp),
                        text = announcement.category.ifBlank { "Announcement" },
                        color = AppColors.Green,
                        fontWeight = FontWeight.SemiBold,
                        fontSize = 11.sp
                    )
                }
            }

            item {
                Row(verticalAlignment = Alignment.Top) {
                    Box(
                        modifier = Modifier
                            .size(48.dp)
                            .background(AppColors.LightGreen, RoundedCornerShape(12.dp)),
                        contentAlignment = Alignment.Center
                    ) {
                        Icon(
                            imageVector = Icons.Filled.Campaign,
                            contentDescription = null,
                            tint = AppColors.Green,
                            modifier = Modifier.size(23.dp)
                        )
                    }

                    Spacer(Modifier.width(14.dp))

                    Column(modifier = Modifier.weight(1f)) {
                        Text(
                            text = announcement.title,
                            color = AppColors.DarkText,
                            fontWeight = FontWeight.SemiBold,
                            fontSize = 19.sp,
                            lineHeight = 25.sp
                        )
                        Spacer(Modifier.height(4.dp))
                        Text(
                            text = announcement.date,
                            color = AppColors.GreyText,
                            fontSize = 11.sp
                        )
                    }
                }
            }

            if (announcement.isPinned) {
                item {
                    Surface(
                        color = AppColors.WarningBackground,
                        shape = RoundedCornerShape(10.dp)
                    ) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(12.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(
                                imageVector = Icons.Filled.PushPin,
                                contentDescription = null,
                                tint = AppColors.WarningText,
                                modifier = Modifier.size(18.dp)
                            )
                            Spacer(Modifier.width(8.dp))
                            Text(
                                text = "Pinned announcement",
                                color = AppColors.WarningText,
                                fontWeight = FontWeight.SemiBold,
                                fontSize = 12.sp
                            )
                        }
                    }
                }
            }

            item {
                Surface(
                    color = Color(0xFFF9FAFB),
                    shape = RoundedCornerShape(12.dp)
                ) {
                    Text(
                        modifier = Modifier.padding(16.dp),
                        text = announcement.excerpt,
                        color = Color(0xFF374151),
                        fontSize = 14.sp,
                        lineHeight = 21.sp
                    )
                }
            }

            item {
                Text(
                    text = announcement.content.ifBlank { announcement.excerpt },
                    color = AppColors.GreyText,
                    fontSize = 14.sp,
                    lineHeight = 22.sp
                )
            }

            item {
                Text(
                    text = "Posted by ${announcement.author}",
                    color = Color(0xFF9CA3AF),
                    fontSize = 11.sp
                )
            }
        }

        Button(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 24.dp, vertical = 16.dp)
                .height(48.dp),
            onClick = onBack,
            colors = ButtonDefaults.buttonColors(
                containerColor = AppColors.Green,
                contentColor = Color.White
            ),
            shape = RoundedCornerShape(10.dp)
        ) {
            Text(
                text = "Back to Notifications",
                fontWeight = FontWeight.SemiBold,
                fontSize = 13.sp
            )
        }
    }
}

@Composable
private fun EmptyNotificationsState() {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(32.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Box(
            modifier = Modifier
                .size(64.dp)
                .background(Color(0xFFF3F4F6), CircleShape),
            contentAlignment = Alignment.Center
        ) {
            Icon(
                imageVector = Icons.Filled.Notifications,
                contentDescription = null,
                tint = Color(0xFF9CA3AF),
                modifier = Modifier.size(30.dp)
            )
        }
        Spacer(Modifier.height(14.dp))
        Text(
            text = "No notifications",
            color = AppColors.DarkText,
            fontWeight = FontWeight.SemiBold,
            fontSize = 18.sp
        )
        Spacer(Modifier.height(6.dp))
        Text(
            text = "You are all caught up. New ParaVolley updates will appear here.",
            color = AppColors.GreyText,
            fontSize = 13.sp,
            lineHeight = 19.sp
        )
    }
}
