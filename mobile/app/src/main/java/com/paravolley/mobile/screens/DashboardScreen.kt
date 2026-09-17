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
import androidx.compose.material.icons.filled.Campaign
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.ChevronRight
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
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
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
        CircularProgressIndicator(color = AppColors.Green)
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
                containerColor = AppColors.Yellow,
                contentColor = AppColors.DarkText
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
    LazyColumn(
        modifier = Modifier
            .fillMaxSize()
            .padding(innerPadding),
        contentPadding = PaddingValues(bottom = 24.dp)
    ) {
        item {
            DashboardHeader(
                dashboard = dashboard,
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
                    items(dashboard.upcomingEvents.take(5), key = { it.id }) { event ->
                        DashboardEventCard(
                            event = event,
                            onOpenEvents = { onNavigate(Routes.EVENTS) }
                        )
                    }
                }
            }
        }

        item {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = 24.dp)
                    .background(Color(0xFFF9FAFB))
                    .padding(horizontal = 24.dp, vertical = 24.dp)
            ) {
                Text(
                    text = "Quick Actions",
                    color = AppColors.DarkText,
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
                        iconBackground = AppColors.Yellow,
                        borderColor = AppColors.Yellow,
                        onClick = { onNavigate(Routes.SCANNER) }
                    )
                    QuickActionCard(
                        modifier = Modifier.weight(1f),
                        title = "View Events",
                        icon = Icons.Filled.CalendarMonth,
                        iconBackground = AppColors.Green,
                        borderColor = AppColors.Green,
                        iconTint = Color.White,
                        onClick = { onNavigate(Routes.EVENTS) }
                    )
                }
            }
        }

        item {
            SectionHeader(
                title = "Notifications",
                action = "View All",
                onAction = onOpenNotifications
            )
        }

        if (dashboard.recentAnnouncements.isEmpty()) {
            item { EmptyCard("No announcements are available.") }
        } else {
            items(dashboard.recentAnnouncements.take(3), key = { it.id }) { announcement ->
                AnnouncementCard(announcement)
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
                title = "Recent Matches",
                action = "View Results",
                onAction = { onNavigate(Routes.RESULTS) }
            )
        }

        if (dashboard.recentMatches.isEmpty()) {
            item { EmptyCard("No recent match information is available.") }
        } else {
            items(dashboard.recentMatches.take(3), key = { it.id }) { match ->
                MatchCard(match)
            }
        }
    }
}

@Composable
private fun DashboardHeader(
    dashboard: PlayerDashboardResponse,
    onOpenNotifications: () -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .background(AppColors.Green)
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
                    .clip(RoundedCornerShape(8.dp))
            )
            Spacer(Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "Welcome,",
                    color = Color.White.copy(alpha = 0.8f),
                    fontSize = 13.sp
                )
                Text(
                    text = dashboard.player.name,
                    color = Color.White,
                    fontSize = 20.sp,
                    fontWeight = FontWeight.SemiBold,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
            }
        }

        Spacer(Modifier.width(10.dp))

        Box {
            Surface(
                color = Color.White.copy(alpha = 0.2f),
                shape = CircleShape
            ) {
                IconButton(onClick = onOpenNotifications) {
                    Icon(
                        imageVector = Icons.Filled.Notifications,
                        contentDescription = "Notifications",
                        tint = Color.White,
                        modifier = Modifier.size(21.dp)
                    )
                }
            }

            if (dashboard.recentAnnouncements.isNotEmpty()) {
                Box(
                    modifier = Modifier
                        .align(Alignment.TopEnd)
                        .size(11.dp)
                        .background(AppColors.Yellow, CircleShape)
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
            .padding(start = 24.dp, end = 14.dp, top = 24.dp, bottom = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Text(
            text = title,
            color = AppColors.DarkText,
            fontWeight = FontWeight.SemiBold,
            fontSize = 18.sp
        )
        if (action.isNotBlank()) {
            TextButton(onClick = onAction) {
                Text(
                    text = action,
                    color = AppColors.Green,
                    fontWeight = FontWeight.Medium,
                    fontSize = 13.sp
                )
                Icon(
                    imageVector = Icons.Filled.ChevronRight,
                    contentDescription = null,
                    tint = AppColors.Green,
                    modifier = Modifier.size(17.dp)
                )
            }
        }
    }
}

@Composable
private fun DashboardEventCard(
    event: DashboardEvent,
    onOpenEvents: () -> Unit
) {
    Card(
        modifier = Modifier.width(280.dp),
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, Color(0xFFF3F4F6)),
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.Top
            ) {
                Text(
                    modifier = Modifier.weight(1f),
                    text = event.title,
                    color = AppColors.DarkText,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 16.sp,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )
                Spacer(Modifier.width(8.dp))
                Pill(text = event.type)
            }

            InfoRow(Icons.Filled.CalendarMonth, event.date)
            InfoRow(Icons.Filled.Schedule, event.time)
            InfoRow(Icons.Filled.LocationOn, event.location)

            Button(
                modifier = Modifier.fillMaxWidth(),
                onClick = onOpenEvents,
                colors = ButtonDefaults.buttonColors(
                    containerColor = AppColors.Yellow,
                    contentColor = AppColors.DarkText
                ),
                shape = RoundedCornerShape(9.dp)
            ) {
                Text(
                    text = "View Details",
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 13.sp
                )
            }
        }
    }
}

@Composable
private fun QuickActionCard(
    modifier: Modifier,
    title: String,
    icon: ImageVector,
    iconBackground: Color,
    borderColor: Color,
    iconTint: Color = AppColors.DarkText,
    onClick: () -> Unit
) {
    Card(
        modifier = modifier,
        onClick = onClick,
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(2.dp, borderColor),
        elevation = CardDefaults.cardElevation(defaultElevation = 3.dp)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(vertical = 22.dp, horizontal = 12.dp),
            horizontalAlignment = Alignment.CenterHorizontally
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
                    modifier = Modifier.size(24.dp)
                )
            }
            Spacer(Modifier.height(12.dp))
            Text(
                text = title,
                color = AppColors.DarkText,
                fontWeight = FontWeight.SemiBold,
                fontSize = 14.sp
            )
        }
    }
}

@Composable
private fun SummaryGrid(dashboard: PlayerDashboardResponse) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 24.dp),
        verticalArrangement = Arrangement.spacedBy(10.dp)
    ) {
        Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
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
        Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
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
        border = BorderStroke(1.dp, AppColors.Border),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
    ) {
        Row(
            modifier = Modifier.padding(14.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Box(
                modifier = Modifier
                    .size(36.dp)
                    .background(AppColors.LightGreen, CircleShape),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = icon,
                    contentDescription = null,
                    tint = AppColors.Green,
                    modifier = Modifier.size(19.dp)
                )
            }
            Spacer(Modifier.width(10.dp))
            Column {
                Text(
                    text = value,
                    color = AppColors.DarkText,
                    fontWeight = FontWeight.Bold,
                    fontSize = 16.sp
                )
                Text(
                    text = label,
                    color = AppColors.GreyText,
                    fontSize = 11.sp
                )
            }
        }
    }
}

@Composable
private fun AnnouncementCard(announcement: DashboardAnnouncement) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 24.dp, vertical = 5.dp),
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, Color(0xFFF3F4F6)),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
    ) {
        Row(
            modifier = Modifier.padding(14.dp),
            verticalAlignment = Alignment.Top
        ) {
            Box(
                modifier = Modifier
                    .size(40.dp)
                    .background(AppColors.LightGreen, CircleShape),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Filled.Notifications,
                    contentDescription = null,
                    tint = AppColors.Green,
                    modifier = Modifier.size(20.dp)
                )
            }
            Spacer(Modifier.width(12.dp))
            Column(modifier = Modifier.weight(1f)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        modifier = Modifier.weight(1f),
                        text = announcement.title,
                        color = AppColors.DarkText,
                        fontWeight = FontWeight.SemiBold,
                        fontSize = 13.sp,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis
                    )
                    Text(
                        text = announcement.date,
                        color = AppColors.GreyText,
                        fontSize = 10.sp
                    )
                }
                Spacer(Modifier.height(4.dp))
                Text(
                    text = announcement.excerpt,
                    color = AppColors.GreyText,
                    fontSize = 13.sp,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )
                if (announcement.isPinned) {
                    Spacer(Modifier.height(6.dp))
                    Text(
                        text = "PINNED",
                        color = AppColors.Green,
                        fontWeight = FontWeight.Bold,
                        fontSize = 10.sp
                    )
                }
            }
        }
    }
}

@Composable
private fun MatchCard(match: DashboardMatch) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 24.dp, vertical = 5.dp),
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, AppColors.Border)
    ) {
        Column(
            modifier = Modifier.padding(15.dp),
            verticalArrangement = Arrangement.spacedBy(6.dp)
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(
                    imageVector = Icons.Filled.EmojiEvents,
                    contentDescription = null,
                    tint = AppColors.Green,
                    modifier = Modifier.size(20.dp)
                )
                Spacer(Modifier.width(8.dp))
                Text(
                    text = match.tournament,
                    color = AppColors.Green,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 12.sp
                )
            }
            Text(
                text = "${match.teamA}  ${match.scoreA ?: "-"}  •  ${match.scoreB ?: "-"}  ${match.teamB}",
                color = AppColors.DarkText,
                fontWeight = FontWeight.Bold,
                fontSize = 16.sp
            )
            Text(
                text = "${match.date} • ${match.time}",
                color = AppColors.GreyText,
                fontSize = 12.sp
            )
            Text(
                text = match.venue,
                color = AppColors.GreyText,
                fontSize = 12.sp
            )
        }
    }
}

@Composable
private fun InfoRow(icon: ImageVector, value: String) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = AppColors.Green,
            modifier = Modifier.size(15.dp)
        )
        Spacer(Modifier.width(7.dp))
        Text(
            text = value,
            color = AppColors.GreyText,
            fontSize = 13.sp,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis
        )
    }
}

@Composable
private fun Pill(text: String) {
    Surface(
        color = AppColors.LightGreen,
        contentColor = AppColors.Green,
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
        border = BorderStroke(1.dp, AppColors.Border)
    ) {
        Text(
            modifier = Modifier.padding(18.dp),
            text = text,
            color = AppColors.GreyText
        )
    }
}
