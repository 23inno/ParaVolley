package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
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
import androidx.compose.material.icons.filled.PushPin
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
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

@OptIn(ExperimentalMaterial3Api::class)
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

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(AppColors.LightBackground)
            .safeDrawingPadding()
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(Color.White)
                .padding(horizontal = 8.dp, vertical = 9.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = onBack) {
                Icon(
                    imageVector = Icons.Filled.ArrowBack,
                    contentDescription = "Back",
                    tint = AppColors.DarkText
                )
            }
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "Notifications",
                    color = AppColors.DarkText,
                    fontWeight = FontWeight.Bold,
                    fontSize = 20.sp
                )
                Text(
                    text = "$unreadCount unread",
                    color = AppColors.GreyText,
                    fontSize = 12.sp
                )
            }
            if (unreadCount > 0) {
                TextButton(
                    onClick = { readAnnouncementIds = announcements.map { it.id }.toSet() }
                ) {
                    Text("Mark all read", color = AppColors.Green, fontSize = 12.sp)
                }
            }
        }

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
                Text(errorMessage ?: "Could not load announcements.", color = AppColors.Error)
            }

            announcements.isEmpty() -> Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(32.dp),
                contentAlignment = Alignment.Center
            ) {
                Text("No announcements are available.", color = AppColors.GreyText)
            }

            else -> LazyColumn(
                modifier = Modifier.weight(1f),
                contentPadding = PaddingValues(16.dp),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                items(announcements, key = { it.id }) { announcement ->
                    val isRead = announcement.id in readAnnouncementIds
                    AnnouncementNotificationCard(
                        announcement = announcement,
                        isRead = isRead,
                        onClick = {
                            readAnnouncementIds = readAnnouncementIds + announcement.id
                            selectedAnnouncement = announcement
                        }
                    )
                }
            }
        }
    }

    selectedAnnouncement?.let { announcement ->
        ModalBottomSheet(
            onDismissRequest = { selectedAnnouncement = null },
            containerColor = Color.White,
            shape = RoundedCornerShape(topStart = 22.dp, topEnd = 22.dp)
        ) {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(start = 22.dp, end = 22.dp, bottom = 30.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Box(
                        modifier = Modifier
                            .size(44.dp)
                            .background(AppColors.LightGreen, CircleShape),
                        contentAlignment = Alignment.Center
                    ) {
                        Icon(Icons.Filled.Campaign, contentDescription = null, tint = AppColors.Green)
                    }
                    Spacer(Modifier.width(12.dp))
                    Column(modifier = Modifier.weight(1f)) {
                        Text(
                            text = announcement.title,
                            color = AppColors.DarkText,
                            fontWeight = FontWeight.Bold,
                            fontSize = 20.sp
                        )
                        Text(
                            text = "${announcement.category} • ${announcement.date}",
                            color = AppColors.GreyText,
                            fontSize = 12.sp
                        )
                    }
                }

                if (announcement.isPinned) {
                    Surface(color = AppColors.WarningBackground, shape = RoundedCornerShape(999.dp)) {
                        Row(
                            modifier = Modifier.padding(horizontal = 10.dp, vertical = 5.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(
                                Icons.Filled.PushPin,
                                contentDescription = null,
                                tint = AppColors.WarningText,
                                modifier = Modifier.size(15.dp)
                            )
                            Spacer(Modifier.width(5.dp))
                            Text("Pinned announcement", color = AppColors.WarningText, fontSize = 11.sp, fontWeight = FontWeight.SemiBold)
                        }
                    }
                }

                Text(
                    text = announcement.content.ifBlank { announcement.excerpt },
                    color = AppColors.DarkText,
                    fontSize = 14.sp,
                    lineHeight = 21.sp
                )
                Text(
                    text = "Posted by ${announcement.author}",
                    color = AppColors.GreyText,
                    fontSize = 12.sp
                )
            }
        }
    }
}

@Composable
private fun AnnouncementNotificationCard(
    announcement: AnnouncementResponse,
    isRead: Boolean,
    onClick: () -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        onClick = onClick,
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(
            containerColor = if (isRead) Color.White else AppColors.LightGreen.copy(alpha = 0.7f)
        ),
        border = androidx.compose.foundation.BorderStroke(
            1.dp,
            if (isRead) AppColors.Border else AppColors.Green.copy(alpha = 0.22f)
        ),
        elevation = CardDefaults.cardElevation(defaultElevation = if (isRead) 0.dp else 1.dp)
    ) {
        Row(
            modifier = Modifier.padding(14.dp),
            verticalAlignment = Alignment.Top
        ) {
            Box(
                modifier = Modifier
                    .size(42.dp)
                    .background(AppColors.LightGreen, CircleShape),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Filled.Campaign,
                    contentDescription = null,
                    tint = AppColors.Green,
                    modifier = Modifier.size(21.dp)
                )
            }
            Spacer(Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        modifier = Modifier.weight(1f),
                        text = announcement.title,
                        color = AppColors.DarkText,
                        fontWeight = if (isRead) FontWeight.SemiBold else FontWeight.Bold,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis
                    )
                    Text(
                        text = announcement.date,
                        color = AppColors.GreyText,
                        fontSize = 10.sp
                    )
                }
                Spacer(Modifier.size(4.dp))
                Text(
                    text = announcement.excerpt,
                    color = AppColors.GreyText,
                    fontSize = 13.sp,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )
                Row(
                    modifier = Modifier.padding(top = 7.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = announcement.category,
                        color = AppColors.Green,
                        fontWeight = FontWeight.SemiBold,
                        fontSize = 11.sp
                    )
                    if (!isRead) {
                        Spacer(Modifier.width(7.dp))
                        Box(
                            modifier = Modifier
                                .size(7.dp)
                                .background(AppColors.Yellow, CircleShape)
                        )
                    }
                }
            }
        }
    }
}
