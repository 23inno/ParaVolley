package com.paravolley.mobile.screens

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Image
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
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.ChevronRight
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.EmojiEvents
import androidx.compose.material.icons.filled.Event
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.QrCodeScanner
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.R
import com.paravolley.mobile.components.AppBottomBar
import com.paravolley.mobile.navigation.Routes
import com.paravolley.mobile.network.DashboardAnnouncement
import com.paravolley.mobile.network.DashboardEvent
import com.paravolley.mobile.network.DashboardMatch
import com.paravolley.mobile.network.DashboardRepository
import com.paravolley.mobile.network.PlayerDashboardResponse
import com.paravolley.mobile.ui.theme.AppColors

private val DashboardGreen = Color(0xFF1A5F3F)
private val DashboardYellow = Color(0xFFFBBF24)
private val DashboardText = Color(0xFF111827)
private val DashboardMuted = Color(0xFF6B7280)
private val DashboardSectionBackground = Color(0xFFF9FAFB)
private val DashboardBorder = Color(0xFFF1F3F5)

@Composable
fun DashboardScreen(
    onNavigate: (String) -> Unit,
    onOpenNotifications: () -> Unit
) {
    val context = LocalContext.current
    val repository = remember { DashboardRepository(context.applicationContext) }
    var dashboard by remember { mutableStateOf<PlayerDashboardResponse?>(null) }
    var isLoading by remember { mutableStateOf(true) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    var reloadKey by remember { mutableIntStateOf(0) }

    LaunchedEffect(reloadKey) {
        isLoading = true
        errorMessage = null

        repository.getDashboard()
            .onSuccess {
                dashboard = it
                isLoading = false
            }
            .onFailure {
                errorMessage = it.message ?: "Could not load dashboard."
                isLoading = false
            }
    }

    Scaffold(
        containerColor = Color.White,
        bottomBar = {
            AppBottomBar(
                selectedRoute = Routes.DASHBOARD,
                onNavigate = onNavigate
            )
        }
    ) { innerPadding ->
        when {
            isLoading -> LoadingState(innerPadding)
            dashboard != null -> DashboardContent(
                dashboard = dashboard!!,
                innerPadding = innerPadding,
                onNavigate = onNavigate,
                onOpenNotifications = onOpenNotifications
            )
            else -> ErrorState(
                innerPadding = innerPadding,
                message = errorMessage ?: "Could not load dashboard.",
                onRetry = { reloadKey++ }
            )
        }
    }
}

@Composable
private fun LoadingState(innerPadding: PaddingValues) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .padding(innerPadding),
        contentAlignment = Alignment.Center
    ) {
        CircularProgressIndicator(color = DashboardGreen)
    }
}

@Composable
private fun ErrorState(
    innerPadding: PaddingValues,
    message: String,
    onRetry: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(innerPadding)
            .padding(24.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(text = message, color = AppColors.Error)
        Spacer(Modifier.height(12.dp))
        Button(
            onClick = onRetry,
            colors = ButtonDefaults.buttonColors(
                containerColor = DashboardYellow,
                contentColor = DashboardText
            ),
            shape = RoundedCornerShape(10.dp)
        ) {
            Text("Try Again", fontWeight = FontWeight.SemiBold)
        }
    }
}

@Composable
private fun DashboardContent(
    dashboard: PlayerDashboardResponse,
    innerPadding: PaddingValues,
    onNavigate: (String) -> Unit,
    onOpenNotifications: () -> Unit
) {
    var selectedEvent by remember { mutableStateOf<DashboardEvent?>(null) }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .padding(innerPadding)
            .background(Color.White)
    ) {
        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            contentPadding = PaddingValues(bottom = 28.dp)
        ) {
            item {
                DashboardHeader(
                    playerName = dashboard.player.name,
                    onOpenNotifications = onOpenNotifications
                )
            }

            item {
                SectionHeader(
                    title = "Upcoming Events",
                    action = "View All",
                    onAction = { onNavigate(Routes.EVENTS) }
                )
            }

            item {
                if (dashboard.upcomingEvents.isEmpty()) {
                    EmptyCard("No upcoming events are available.")
                } else {
                    LazyRow(
                        contentPadding = PaddingValues(horizontal = 24.dp),
                        horizontalArrangement = Arrangement.spacedBy(16.dp)
                    ) {
                        items(
                            items = dashboard.upcomingEvents.take(5),
                            key = { "event-${it.id}" }
                        ) { event ->
                            DashboardEventCard(
                                event = event,
                                onViewDetails = { selectedEvent = event }
                            )
                        }
                    }
                }
            }

            item {
                QuickActionsSection(
                    onScan = { onNavigate(Routes.SCANNER) },
                    onEvents = { onNavigate(Routes.EVENTS) }
                )
            }

            item {
                SectionHeader(
                    title = "Notifications",
                    action = "View All",
                    onAction = onOpenNotifications
                )
            }

            if (dashboard.recentAnnouncements.isEmpty()) {
                item { EmptyCard("No notifications are available.") }
            } else {
                items(
                    items = dashboard.recentAnnouncements.take(3),
                    key = { "announcement-${it.id}" }
                ) { announcement ->
                    NotificationPreviewCard(announcement)
                }
            }

            item {
                SectionHeader(
                    title = "Your Summary",
                    action = "Profile",
                    onAction = { onNavigate(Routes.PROFILE) }
                )
            }

            item {
                SummaryGrid(dashboard)
            }

            item {
                SectionHeader(
                    title = "Recent Results",
                    action = "View All",
                    onAction = { onNavigate(Routes.RESULTS) }
                )
            }

            if (dashboard.recentMatches.isEmpty()) {
                item { EmptyCard("No recent match results are available.") }
            } else {
                items(
                    items = dashboard.recentMatches.take(3),
                    key = { "match-${it.id}" }
                ) { match ->
                    MatchCard(match)
                }
            }
        }

        selectedEvent?.let { event ->
            EventDetailsSheet(
                event = event,
                onDismiss = { selectedEvent = null },
                onOpenEvents = {
                    selectedEvent = null
                    onNavigate(Routes.EVENTS)
                }
            )
        }
    }
}

@Composable
private fun DashboardHeader(
    playerName: String,
    onOpenNotifications: () -> Unit
) {
    val firstName = playerName.trim().substringBefore(" ").ifBlank { playerName }

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .background(DashboardGreen)
            .padding(horizontal = 24.dp, vertical = 24.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Row(
            modifier = Modifier.weight(1f),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Image(
                painter = painterResource(R.drawable.paravolley_mpumalanga_logo),
                contentDescription = "ParaVolley Mpumalanga logo",
                modifier = Modifier
                    .size(48.dp)
                    .clip(RoundedCornerShape(9.dp))
            )

            Spacer(Modifier.width(12.dp))

            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "Welcome,",
                    color = Color.White.copy(alpha = 0.80f),
                    fontSize = 14.sp
                )
                Text(
                    text = firstName,
                    color = Color.White,
                    fontSize = 20.sp,
                    fontWeight = FontWeight.SemiBold,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
            }
        }

        Spacer(Modifier.width(12.dp))

        Surface(
            color = Color.White.copy(alpha = 0.18f),
            shape = CircleShape
        ) {
            IconButton(
                modifier = Modifier.size(40.dp),
                onClick = onOpenNotifications
            ) {
                Icon(
                    imageVector = Icons.Filled.Notifications,
                    contentDescription = "Notifications",
                    tint = Color.White,
                    modifier = Modifier.size(21.dp)
                )
            }
        }
    }
}

@Composable
private fun SectionHeader(
    title: String,
    action: String,
    onAction: () -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(start = 24.dp, end = 14.dp, top = 24.dp, bottom = 14.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Text(
            text = title,
            color = DashboardText,
            fontWeight = FontWeight.SemiBold,
            fontSize = 18.sp
        )

        if (action.isNotBlank()) {
            TextButton(onClick = onAction) {
                Text(
                    text = action,
                    color = DashboardGreen,
                    fontSize = 13.sp,
                    fontWeight = FontWeight.Medium
                )
                Spacer(Modifier.width(2.dp))
                Icon(
                    imageVector = Icons.Filled.ChevronRight,
                    contentDescription = null,
                    tint = DashboardGreen,
                    modifier = Modifier.size(17.dp)
                )
            }
        }
    }
}

@Composable
private fun DashboardEventCard(
    event: DashboardEvent,
    onViewDetails: () -> Unit
) {
    Card(
        modifier = Modifier.width(280.dp),
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, DashboardBorder),
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
    ) {
        Column(
            modifier = Modifier.padding(16.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.Top
            ) {
                Text(
                    modifier = Modifier.weight(1f),
                    text = event.title,
                    color = DashboardText,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 16.sp,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )

                Spacer(Modifier.width(8.dp))
                CategoryPill(event.type)
            }

            Spacer(Modifier.height(12.dp))
            EventInfoRow(Icons.Filled.Event, event.date)
            Spacer(Modifier.height(6.dp))
            EventInfoRow(Icons.Filled.Schedule, event.time)
            Spacer(Modifier.height(6.dp))
            EventInfoRow(Icons.Filled.LocationOn, event.location)
            Spacer(Modifier.height(16.dp))

            Button(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(42.dp),
                onClick = onViewDetails,
                colors = ButtonDefaults.buttonColors(
                    containerColor = DashboardYellow,
                    contentColor = DashboardText
                ),
                shape = RoundedCornerShape(8.dp)
            ) {
                Text(
                    text = "View Details",
                    fontSize = 13.sp,
                    fontWeight = FontWeight.SemiBold
                )
            }
        }
    }
}

@Composable
private fun QuickActionsSection(
    onScan: () -> Unit,
    onEvents: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(top = 26.dp)
            .background(DashboardSectionBackground)
            .padding(horizontal = 24.dp, vertical = 24.dp)
    ) {
        Text(
            text = "Quick Actions",
            color = DashboardText,
            fontWeight = FontWeight.SemiBold,
            fontSize = 18.sp
        )

        Spacer(Modifier.height(16.dp))

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            QuickActionCard(
                modifier = Modifier.weight(1f),
                title = "Scan QR",
                icon = Icons.Filled.QrCodeScanner,
                iconBackground = DashboardYellow,
                iconTint = DashboardText,
                borderColor = DashboardYellow,
                onClick = onScan
            )

            QuickActionCard(
                modifier = Modifier.weight(1f),
                title = "View Events",
                icon = Icons.Filled.CalendarMonth,
                iconBackground = DashboardGreen,
                iconTint = Color.White,
                borderColor = DashboardGreen,
                onClick = onEvents
            )
        }
    }
}

@Composable
private fun QuickActionCard(
    modifier: Modifier,
    title: String,
    icon: ImageVector,
    iconBackground: Color,
    iconTint: Color,
    borderColor: Color,
    onClick: () -> Unit
) {
    Card(
        modifier = modifier.height(132.dp),
        onClick = onClick,
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(2.dp, borderColor),
        elevation = CardDefaults.cardElevation(defaultElevation = 3.dp)
    ) {
        Column(
            modifier = Modifier.fillMaxSize(),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            Box(
                modifier = Modifier
                    .size(48.dp)
                    .background(iconBackground, CircleShape),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = icon,
                    contentDescription = null,
                    tint = iconTint,
                    modifier = Modifier.size(25.dp)
                )
            }

            Spacer(Modifier.height(12.dp))

            Text(
                text = title,
                color = DashboardText,
                fontWeight = FontWeight.Medium,
                fontSize = 14.sp
            )
        }
    }
}

@Composable
private fun NotificationPreviewCard(
    announcement: DashboardAnnouncement
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 24.dp, vertical = 6.dp),
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, DashboardBorder),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.Top
        ) {
            Box(
                modifier = Modifier
                    .size(40.dp)
                    .background(DashboardGreen.copy(alpha = 0.09f), CircleShape),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Filled.Notifications,
                    contentDescription = null,
                    tint = DashboardGreen,
                    modifier = Modifier.size(20.dp)
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
                        color = DashboardText,
                        fontWeight = FontWeight.Medium,
                        fontSize = 14.sp,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis
                    )

                    Spacer(Modifier.width(8.dp))

                    Text(
                        text = announcement.date,
                        color = DashboardMuted,
                        fontSize = 11.sp
                    )
                }

                Spacer(Modifier.height(4.dp))

                Text(
                    text = announcement.excerpt,
                    color = DashboardMuted,
                    fontSize = 13.sp,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )
            }
        }
    }
}

@Composable
private fun SummaryGrid(dashboard: PlayerDashboardResponse) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 24.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            SummaryTile(
                modifier = Modifier.weight(1f),
                icon = Icons.Filled.Event,
                value = dashboard.summary.upcomingEvents.toString(),
                label = "Upcoming"
            )
            SummaryTile(
                modifier = Modifier.weight(1f),
                icon = Icons.Filled.CheckCircle,
                value = dashboard.summary.registeredEvents.toString(),
                label = "Registered"
            )
        }

        Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            SummaryTile(
                modifier = Modifier.weight(1f),
                icon = Icons.Filled.Person,
                value = "${dashboard.summary.presentAttendance}/${dashboard.summary.totalAttendance}",
                label = "Attendance"
            )
            SummaryTile(
                modifier = Modifier.weight(1f),
                icon = Icons.Filled.EmojiEvents,
                value = String.format("%.0f%%", dashboard.summary.attendanceRate),
                label = "Attendance Rate"
            )
        }
    }
}

@Composable
private fun SummaryTile(
    modifier: Modifier,
    icon: ImageVector,
    value: String,
    label: String
) {
    Card(
        modifier = modifier,
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, DashboardBorder),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
    ) {
        Row(
            modifier = Modifier.padding(14.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Box(
                modifier = Modifier
                    .size(36.dp)
                    .background(DashboardGreen.copy(alpha = 0.09f), CircleShape),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = icon,
                    contentDescription = null,
                    tint = DashboardGreen,
                    modifier = Modifier.size(19.dp)
                )
            }

            Spacer(Modifier.width(10.dp))

            Column {
                Text(
                    text = value,
                    color = DashboardText,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 16.sp
                )
                Text(
                    text = label,
                    color = DashboardMuted,
                    fontSize = 11.sp
                )
            }
        }
    }
}

@Composable
private fun MatchCard(match: DashboardMatch) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 24.dp, vertical = 6.dp),
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, DashboardBorder),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(6.dp)
        ) {
            Text(
                text = match.tournament,
                color = DashboardGreen,
                fontWeight = FontWeight.Medium,
                fontSize = 12.sp
            )
            Text(
                text = "${match.teamA}  ${match.scoreA ?: "-"}  •  ${match.scoreB ?: "-"}  ${match.teamB}",
                color = DashboardText,
                fontWeight = FontWeight.SemiBold,
                fontSize = 16.sp
            )
            Text(
                text = "${match.date} • ${match.time}",
                color = DashboardMuted,
                fontSize = 12.sp
            )
            Text(
                text = match.venue,
                color = DashboardMuted,
                fontSize = 12.sp,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )
        }
    }
}

@Composable
private fun EventInfoRow(
    icon: ImageVector,
    value: String
) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = DashboardGreen,
            modifier = Modifier.size(15.dp)
        )
        Spacer(Modifier.width(7.dp))
        Text(
            text = value,
            color = DashboardMuted,
            fontSize = 13.sp,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis
        )
    }
}

@Composable
private fun CategoryPill(text: String) {
    Surface(
        color = DashboardGreen.copy(alpha = 0.09f),
        contentColor = DashboardGreen,
        shape = RoundedCornerShape(999.dp)
    ) {
        Text(
            text = text,
            modifier = Modifier.padding(horizontal = 9.dp, vertical = 4.dp),
            fontWeight = FontWeight.Medium,
            fontSize = 10.sp
        )
    }
}

@Composable
private fun EmptyCard(text: String) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 24.dp),
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, DashboardBorder)
    ) {
        Text(
            modifier = Modifier.padding(18.dp),
            text = text,
            color = DashboardMuted,
            fontSize = 13.sp
        )
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun EventDetailsSheet(
    event: DashboardEvent,
    onDismiss: () -> Unit,
    onOpenEvents: () -> Unit
) {
    val sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)

    ModalBottomSheet(
        onDismissRequest = onDismiss,
        sheetState = sheetState,
        containerColor = Color.White,
        shape = RoundedCornerShape(topStart = 20.dp, topEnd = 20.dp),
        dragHandle = null
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 24.dp)
                .padding(bottom = 28.dp)
        ) {
            Box(
                modifier = Modifier
                    .padding(top = 10.dp, bottom = 12.dp)
                    .width(40.dp)
                    .height(4.dp)
                    .background(Color(0xFFD1D5DB), RoundedCornerShape(999.dp))
                    .align(Alignment.CenterHorizontally)
            )

            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.Top
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    CategoryPill(event.type)
                    Spacer(Modifier.height(8.dp))
                    Text(
                        text = event.title,
                        color = DashboardText,
                        fontWeight = FontWeight.SemiBold,
                        fontSize = 20.sp
                    )
                }

                IconButton(onClick = onDismiss) {
                    Box(
                        modifier = Modifier
                            .size(32.dp)
                            .background(Color(0xFFF3F4F6), CircleShape),
                        contentAlignment = Alignment.Center
                    ) {
                        Icon(
                            imageVector = Icons.Filled.Close,
                            contentDescription = "Close",
                            tint = DashboardMuted,
                            modifier = Modifier.size(18.dp)
                        )
                    }
                }
            }

            Spacer(Modifier.height(14.dp))

            Text(
                text = "Open the Events screen to view the complete event information and manage your registration.",
                color = DashboardMuted,
                fontSize = 13.sp
            )

            Spacer(Modifier.height(18.dp))

            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(
                        color = DashboardSectionBackground,
                        shape = RoundedCornerShape(12.dp)
                    )
                    .padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(14.dp)
            ) {
                SheetDetailRow(
                    icon = Icons.Filled.Event,
                    label = "Date",
                    value = event.date
                )
                SheetDetailRow(
                    icon = Icons.Filled.Schedule,
                    label = "Time",
                    value = event.time
                )
                SheetDetailRow(
                    icon = Icons.Filled.LocationOn,
                    label = "Venue",
                    value = event.location
                )
            }

            Spacer(Modifier.height(20.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                Button(
                    modifier = Modifier
                        .weight(1f)
                        .height(48.dp),
                    onClick = onDismiss,
                    colors = ButtonDefaults.buttonColors(
                        containerColor = Color.White,
                        contentColor = DashboardMuted
                    ),
                    border = BorderStroke(2.dp, Color(0xFFE5E7EB)),
                    shape = RoundedCornerShape(11.dp)
                ) {
                    Text("Close", fontWeight = FontWeight.Medium)
                }

                Button(
                    modifier = Modifier
                        .weight(1f)
                        .height(48.dp),
                    onClick = onOpenEvents,
                    colors = ButtonDefaults.buttonColors(
                        containerColor = DashboardYellow,
                        contentColor = DashboardText
                    ),
                    shape = RoundedCornerShape(11.dp)
                ) {
                    Text("Open Event", fontWeight = FontWeight.SemiBold)
                }
            }
        }
    }
}

@Composable
private fun SheetDetailRow(
    icon: ImageVector,
    label: String,
    value: String
) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Box(
            modifier = Modifier
                .size(36.dp)
                .background(DashboardGreen.copy(alpha = 0.09f), RoundedCornerShape(9.dp)),
            contentAlignment = Alignment.Center
        ) {
            Icon(
                imageVector = icon,
                contentDescription = null,
                tint = DashboardGreen,
                modifier = Modifier.size(18.dp)
            )
        }

        Spacer(Modifier.width(12.dp))

        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = label,
                color = DashboardMuted,
                fontSize = 11.sp
            )
            Text(
                text = value,
                color = DashboardText,
                fontSize = 13.sp,
                fontWeight = FontWeight.Medium
            )
        }
    }
}
