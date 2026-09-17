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
import androidx.compose.material.icons.filled.DoneAll
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.PushPin
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
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
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.network.AnnouncementResponse
import com.paravolley.mobile.network.AnnouncementsRepository
import com.paravolley.mobile.ui.theme.AppColors
import java.time.LocalDate
import java.time.format.DateTimeFormatter
import java.util.Locale
import kotlinx.coroutines.launch

private val NotificationGreen = Color(0xFF1A5F3F)
private val NotificationYellow = Color(0xFFFBBF24)
private val NotificationBackground = Color(0xFFF7F9F8)
private val NotificationBorder = Color(0xFFE5E7EB)
private val NotificationText = Color(0xFF111827)
private val NotificationMuted = Color(0xFF6B7280)
private val NotificationUnread = Color(0xFFF0F8F4)

private enum class NotificationFilter {
    ALL,
    UNREAD
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun NotificationsScreen(
    initialAnnouncementId: Int? = null,
    onBack: () -> Unit
) {
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    val repository = remember {
        AnnouncementsRepository(context.applicationContext)
    }

    var announcements by remember {
        mutableStateOf<List<AnnouncementResponse>>(emptyList())
    }
    var selectedAnnouncement by remember {
        mutableStateOf<AnnouncementResponse?>(null)
    }
    var selectedFilter by remember {
        mutableStateOf(NotificationFilter.ALL)
    }
    var isLoading by remember { mutableStateOf(true) }
    var isMarkingAll by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }

    fun openAnnouncement(announcement: AnnouncementResponse) {
        val openedAnnouncement = announcement.copy(isRead = true)

        announcements = announcements.map { item ->
            if (item.id == announcement.id) {
                item.copy(isRead = true)
            } else {
                item
            }
        }
        selectedAnnouncement = openedAnnouncement

        if (!announcement.isRead) {
            scope.launch {
                repository.markAnnouncementRead(announcement.id)
            }
        }
    }

    LaunchedEffect(initialAnnouncementId) {
        isLoading = true
        errorMessage = null

        repository.getAnnouncements()
            .onSuccess { loadedAnnouncements ->
                announcements = loadedAnnouncements

                initialAnnouncementId?.let { targetId ->
                    loadedAnnouncements
                        .firstOrNull { it.id == targetId }
                        ?.let { targetAnnouncement ->
                            openAnnouncement(targetAnnouncement)
                        }
                }
            }
            .onFailure { failure ->
                errorMessage = failure.message
                    ?: "Could not load notifications."
            }

        isLoading = false
    }

    val unreadCount = announcements.count { !it.isRead }
    val filteredAnnouncements = when (selectedFilter) {
        NotificationFilter.ALL -> announcements
        NotificationFilter.UNREAD -> announcements.filter { !it.isRead }
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(NotificationBackground)
            .safeDrawingPadding()
    ) {
        NotificationHeader(
            unreadCount = unreadCount,
            onBack = onBack
        )

        if (!isLoading && errorMessage == null && announcements.isNotEmpty()) {
            NotificationSummaryCard(
                unreadCount = unreadCount,
                totalCount = announcements.size,
                isMarkingAll = isMarkingAll,
                onMarkAllRead = {
                    if (unreadCount > 0 && !isMarkingAll) {
                        val allIds = announcements.map { it.id }
                        announcements = announcements.map {
                            it.copy(isRead = true)
                        }

                        scope.launch {
                            isMarkingAll = true
                            repository.markAllAnnouncementsRead(allIds)
                            isMarkingAll = false
                        }
                    }
                }
            )

            NotificationFilterRow(
                selectedFilter = selectedFilter,
                allCount = announcements.size,
                unreadCount = unreadCount,
                onFilterSelected = { selectedFilter = it }
            )
        }

        when {
            isLoading -> Box(
                modifier = Modifier
                    .fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator(color = NotificationGreen)
            }

            errorMessage != null -> NotificationEmptyState(
                title = "Notifications unavailable",
                message = errorMessage ?: "Could not load notifications."
            )

            announcements.isEmpty() -> NotificationEmptyState(
                title = "You're all caught up",
                message = "New ParaVolley announcements will appear here."
            )

            filteredAnnouncements.isEmpty() -> NotificationEmptyState(
                title = "No unread notifications",
                message = "You have read all of your current announcements."
            )

            else -> LazyColumn(
                modifier = Modifier.weight(1f),
                contentPadding = PaddingValues(
                    start = 16.dp,
                    end = 16.dp,
                    top = 8.dp,
                    bottom = 24.dp
                ),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                items(
                    items = filteredAnnouncements,
                    key = { it.id }
                ) { announcement ->
                    NotificationCard(
                        announcement = announcement,
                        onClick = { openAnnouncement(announcement) }
                    )
                }
            }
        }
    }

    selectedAnnouncement?.let { announcement ->
        NotificationDetailSheet(
            announcement = announcement,
            onDismiss = { selectedAnnouncement = null }
        )
    }
}

@Composable
private fun NotificationHeader(
    unreadCount: Int,
    onBack: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .background(NotificationGreen)
            .padding(horizontal = 8.dp, vertical = 8.dp)
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = onBack) {
                Icon(
                    imageVector = Icons.Filled.ArrowBack,
                    contentDescription = "Back",
                    tint = Color.White
                )
            }

            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "Notifications",
                    color = Color.White,
                    fontWeight = FontWeight.Bold,
                    fontSize = 21.sp
                )
                Text(
                    text = if (unreadCount == 1) {
                        "1 unread message"
                    } else {
                        "$unreadCount unread messages"
                    },
                    color = Color.White.copy(alpha = 0.75f),
                    fontSize = 12.sp
                )
            }

            Box(
                modifier = Modifier
                    .size(42.dp)
                    .background(
                        Color.White.copy(alpha = 0.12f),
                        CircleShape
                    ),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Filled.Notifications,
                    contentDescription = null,
                    tint = NotificationYellow,
                    modifier = Modifier.size(23.dp)
                )
            }

            Spacer(Modifier.width(8.dp))
        }
    }
}

@Composable
private fun NotificationSummaryCard(
    unreadCount: Int,
    totalCount: Int,
    isMarkingAll: Boolean,
    onMarkAllRead: () -> Unit
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 14.dp),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, NotificationBorder),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Box(
                modifier = Modifier
                    .size(44.dp)
                    .background(NotificationUnread, CircleShape),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = unreadCount.toString(),
                    color = NotificationGreen,
                    fontWeight = FontWeight.Bold,
                    fontSize = 18.sp
                )
            }

            Spacer(Modifier.width(12.dp))

            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "Inbox summary",
                    color = NotificationText,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 15.sp
                )
                Text(
                    text = "$unreadCount unread of $totalCount notifications",
                    color = NotificationMuted,
                    fontSize = 12.sp
                )
            }

            if (unreadCount > 0) {
                TextButton(
                    onClick = onMarkAllRead,
                    enabled = !isMarkingAll
                ) {
                    if (isMarkingAll) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(16.dp),
                            color = NotificationGreen,
                            strokeWidth = 2.dp
                        )
                    } else {
                        Icon(
                            imageVector = Icons.Filled.DoneAll,
                            contentDescription = null,
                            tint = NotificationGreen,
                            modifier = Modifier.size(17.dp)
                        )
                        Spacer(Modifier.width(5.dp))
                        Text(
                            text = "Mark all read",
                            color = NotificationGreen,
                            fontSize = 12.sp,
                            fontWeight = FontWeight.SemiBold
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun NotificationFilterRow(
    selectedFilter: NotificationFilter,
    allCount: Int,
    unreadCount: Int,
    onFilterSelected: (NotificationFilter) -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 2.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        NotificationFilterButton(
            modifier = Modifier.weight(1f),
            label = "All",
            count = allCount,
            selected = selectedFilter == NotificationFilter.ALL,
            onClick = { onFilterSelected(NotificationFilter.ALL) }
        )
        NotificationFilterButton(
            modifier = Modifier.weight(1f),
            label = "Unread",
            count = unreadCount,
            selected = selectedFilter == NotificationFilter.UNREAD,
            onClick = { onFilterSelected(NotificationFilter.UNREAD) }
        )
    }
}

@Composable
private fun NotificationFilterButton(
    modifier: Modifier,
    label: String,
    count: Int,
    selected: Boolean,
    onClick: () -> Unit
) {
    Button(
        modifier = modifier,
        onClick = onClick,
        shape = RoundedCornerShape(12.dp),
        colors = ButtonDefaults.buttonColors(
            containerColor = if (selected) NotificationGreen else Color.White,
            contentColor = if (selected) Color.White else NotificationMuted
        ),
        border = if (selected) null else BorderStroke(1.dp, NotificationBorder),
        elevation = ButtonDefaults.buttonElevation(defaultElevation = 0.dp),
        contentPadding = PaddingValues(vertical = 10.dp)
    ) {
        Text(
            text = "$label ($count)",
            fontSize = 13.sp,
            fontWeight = FontWeight.SemiBold
        )
    }
}

@Composable
private fun NotificationCard(
    announcement: AnnouncementResponse,
    onClick: () -> Unit
) {
    val isRead = announcement.isRead

    Card(
        modifier = Modifier.fillMaxWidth(),
        onClick = onClick,
        shape = RoundedCornerShape(15.dp),
        colors = CardDefaults.cardColors(
            containerColor = if (isRead) Color.White else NotificationUnread
        ),
        border = BorderStroke(
            1.dp,
            if (isRead) NotificationBorder else NotificationGreen.copy(alpha = 0.25f)
        ),
        elevation = CardDefaults.cardElevation(
            defaultElevation = if (isRead) 0.dp else 1.dp
        )
    ) {
        Row(
            modifier = Modifier.padding(15.dp),
            verticalAlignment = Alignment.Top
        ) {
            Box(
                modifier = Modifier
                    .size(44.dp)
                    .background(
                        if (isRead) Color(0xFFF3F4F6) else Color.White,
                        CircleShape
                    ),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Filled.Campaign,
                    contentDescription = null,
                    tint = NotificationGreen,
                    modifier = Modifier.size(22.dp)
                )
            }

            Spacer(Modifier.width(12.dp))

            Column(modifier = Modifier.weight(1f)) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.Top
                ) {
                    Text(
                        modifier = Modifier.weight(1f),
                        text = announcement.title,
                        color = NotificationText,
                        fontWeight = if (isRead) {
                            FontWeight.SemiBold
                        } else {
                            FontWeight.Bold
                        },
                        fontSize = 15.sp,
                        maxLines = 2,
                        overflow = TextOverflow.Ellipsis
                    )

                    if (!isRead) {
                        Spacer(Modifier.width(8.dp))
                        Box(
                            modifier = Modifier
                                .padding(top = 5.dp)
                                .size(9.dp)
                                .background(NotificationYellow, CircleShape)
                        )
                    }
                }

                Spacer(Modifier.size(5.dp))

                Text(
                    text = announcement.excerpt.ifBlank { announcement.content },
                    color = NotificationMuted,
                    fontSize = 13.sp,
                    lineHeight = 18.sp,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )

                Spacer(Modifier.size(9.dp))

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Surface(
                        color = NotificationGreen.copy(alpha = 0.08f),
                        shape = RoundedCornerShape(999.dp)
                    ) {
                        Text(
                            modifier = Modifier.padding(
                                horizontal = 9.dp,
                                vertical = 4.dp
                            ),
                            text = announcement.category,
                            color = NotificationGreen,
                            fontWeight = FontWeight.SemiBold,
                            fontSize = 10.sp
                        )
                    }

                    if (announcement.isPinned) {
                        Spacer(Modifier.width(7.dp))
                        Icon(
                            imageVector = Icons.Filled.PushPin,
                            contentDescription = "Pinned",
                            tint = NotificationYellow,
                            modifier = Modifier.size(15.dp)
                        )
                    }

                    Spacer(Modifier.weight(1f))

                    Text(
                        text = formatNotificationDate(announcement.date),
                        color = NotificationMuted,
                        fontSize = 11.sp
                    )
                }
            }
        }
    }
}

@Composable
private fun NotificationEmptyState(
    title: String,
    message: String
) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .padding(32.dp),
        contentAlignment = Alignment.Center
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            Box(
                modifier = Modifier
                    .size(64.dp)
                    .background(NotificationUnread, CircleShape),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Filled.Notifications,
                    contentDescription = null,
                    tint = NotificationGreen,
                    modifier = Modifier.size(30.dp)
                )
            }
            Text(
                text = title,
                color = NotificationText,
                fontWeight = FontWeight.SemiBold,
                fontSize = 17.sp,
                textAlign = TextAlign.Center
            )
            Text(
                text = message,
                color = NotificationMuted,
                fontSize = 13.sp,
                textAlign = TextAlign.Center
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun NotificationDetailSheet(
    announcement: AnnouncementResponse,
    onDismiss: () -> Unit
) {
    ModalBottomSheet(
        onDismissRequest = onDismiss,
        containerColor = Color.White,
        shape = RoundedCornerShape(topStart = 24.dp, topEnd = 24.dp)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(start = 22.dp, end = 22.dp, bottom = 32.dp),
            verticalArrangement = Arrangement.spacedBy(14.dp)
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Box(
                    modifier = Modifier
                        .size(48.dp)
                        .background(NotificationUnread, CircleShape),
                    contentAlignment = Alignment.Center
                ) {
                    Icon(
                        imageVector = Icons.Filled.Campaign,
                        contentDescription = null,
                        tint = NotificationGreen,
                        modifier = Modifier.size(24.dp)
                    )
                }

                Spacer(Modifier.width(12.dp))

                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = announcement.title,
                        color = NotificationText,
                        fontWeight = FontWeight.Bold,
                        fontSize = 20.sp
                    )
                    Text(
                        text = "${announcement.category} • ${formatNotificationDate(announcement.date)}",
                        color = NotificationMuted,
                        fontSize = 12.sp
                    )
                }
            }

            if (announcement.isPinned) {
                Surface(
                    color = AppColors.WarningBackground,
                    shape = RoundedCornerShape(999.dp)
                ) {
                    Row(
                        modifier = Modifier.padding(
                            horizontal = 10.dp,
                            vertical = 5.dp
                        ),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Icon(
                            imageVector = Icons.Filled.PushPin,
                            contentDescription = null,
                            tint = AppColors.WarningText,
                            modifier = Modifier.size(15.dp)
                        )
                        Spacer(Modifier.width(5.dp))
                        Text(
                            text = "Pinned announcement",
                            color = AppColors.WarningText,
                            fontSize = 11.sp,
                            fontWeight = FontWeight.SemiBold
                        )
                    }
                }
            }

            Text(
                text = announcement.content.ifBlank { announcement.excerpt },
                color = NotificationText,
                fontSize = 14.sp,
                lineHeight = 22.sp
            )

            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(
                    imageVector = Icons.Filled.Person,
                    contentDescription = null,
                    tint = NotificationMuted,
                    modifier = Modifier.size(17.dp)
                )
                Spacer(Modifier.width(7.dp))
                Text(
                    text = "Posted by ${announcement.author}",
                    color = NotificationMuted,
                    fontSize = 12.sp
                )
            }
        }
    }
}

private fun formatNotificationDate(raw: String): String {
    val value = raw.trim()
    if (value.isBlank()) return ""

    return try {
        val isoDate = if (value.length >= 10) value.take(10) else value
        LocalDate.parse(isoDate).format(
            DateTimeFormatter.ofPattern(
                "dd MMM yyyy",
                Locale.getDefault()
            )
        )
    } catch (_: Exception) {
        value
    }
}
