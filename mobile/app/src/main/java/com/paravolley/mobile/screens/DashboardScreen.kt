package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
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
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.components.AppBottomBar
import com.paravolley.mobile.navigation.Routes
import com.paravolley.mobile.network.DashboardAnnouncement
import com.paravolley.mobile.network.DashboardEvent
import com.paravolley.mobile.network.DashboardMatch
import com.paravolley.mobile.network.DashboardRepository
import com.paravolley.mobile.network.PlayerDashboardResponse
import com.paravolley.mobile.network.SessionManager
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun DashboardScreen(
    onNavigate: (String) -> Unit,
    onOpenNotifications: () -> Unit
) {
    val context =
        LocalContext.current

    val dashboardRepository =
        remember {
            DashboardRepository(
                context.applicationContext
            )
        }

    val sessionManager =
        remember {
            SessionManager(
                context.applicationContext
            )
        }

    var dashboard by remember {
        mutableStateOf<PlayerDashboardResponse?>(
            null
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

        dashboardRepository
            .getDashboard()
            .onSuccess { response ->
                dashboard = response
                isLoading = false
            }
            .onFailure { exception ->
                errorMessage =
                    exception.message
                        ?: "Could not load dashboard."

                isLoading = false
            }
    }

    Scaffold(
        containerColor =
            AppColors.LightBackground,
        bottomBar = {
            AppBottomBar(
                selectedRoute =
                    Routes.DASHBOARD,
                onNavigate =
                    onNavigate
            )
        }
    ) { innerPadding ->

        when {
            isLoading -> {
                Box(
                    modifier =
                        Modifier
                            .padding(
                                innerPadding
                            )
                            .fillMaxWidth()
                            .padding(40.dp),
                    contentAlignment =
                        Alignment.Center
                ) {
                    CircularProgressIndicator()
                }
            }

            errorMessage != null -> {
                Column(
                    modifier =
                        Modifier
                            .padding(
                                innerPadding
                            )
                            .fillMaxWidth()
                            .padding(24.dp),
                    horizontalAlignment =
                        Alignment.CenterHorizontally,
                    verticalArrangement =
                        Arrangement.spacedBy(
                            16.dp
                        )
                ) {
                    Text(
                        text =
                            errorMessage
                                ?: "Could not load dashboard.",
                        color =
                            Color.Red
                    )

                    OutlinedButton(
                        onClick = {
                            sessionManager
                                .clearSession()
                        }
                    ) {
                        Text(
                            "Session Error"
                        )
                    }
                }
            }

            dashboard != null -> {
                DashboardContent(
                    dashboard =
                        dashboard!!,
                    innerPadding =
                        innerPadding,
                    onNavigate =
                        onNavigate,
                    onOpenNotifications =
                        onOpenNotifications
                )
            }
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
    val playerInitials = dashboard.player.name
        .trim()
        .split(Regex("\\s+"))
        .filter(String::isNotBlank)
        .take(2)
        .mapNotNull { it.firstOrNull()?.uppercase() }
        .joinToString("")
        .ifBlank { "PV" }

    LazyColumn(
        modifier =
            Modifier.padding(
                innerPadding
            ),
        contentPadding =
            PaddingValues(
                bottom = 24.dp
            )
    ) {
        item {
            Column(
                modifier =
                    Modifier
                        .fillMaxWidth()
                        .background(
                            AppColors.DarkGreen
                        )
                        .padding(22.dp)
            ) {
                Row(
                    modifier =
                        Modifier.fillMaxWidth(),
                    horizontalArrangement =
                        Arrangement.SpaceBetween,
                    verticalAlignment =
                        Alignment.CenterVertically
                ) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Box(
                            modifier = Modifier
                                .size(48.dp)
                                .background(AppColors.Yellow, CircleShape),
                            contentAlignment = Alignment.Center
                        ) {
                            Text(
                                text = playerInitials,
                                color = AppColors.DarkText,
                                fontWeight = FontWeight.Bold
                            )
                        }

                        Spacer(modifier = Modifier.width(12.dp))

                        Column {
                            Text(
                                text = "Welcome back,",
                                color = Color.White.copy(alpha = 0.82f)
                            )

                            Text(
                                text = dashboard.player.name,
                                color = Color.White,
                                fontSize = 23.sp,
                                fontWeight = FontWeight.Bold
                            )

                            Text(
                                text = "${dashboard.player.position} • ${dashboard.player.team}",
                                color = Color.White.copy(alpha = 0.9f)
                            )
                        }
                    }

                    Button(
                        onClick =
                            onOpenNotifications,
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
                            "Announcements"
                        )
                    }
                }
            }
        }

        item {
            DashboardHeading(
                title =
                    "Your Summary",
                buttonText =
                    "Profile",
                onButtonClick = {
                    onNavigate(
                        Routes.PROFILE
                    )
                }
            )
        }

        item {
            SummarySection(
                dashboard =
                    dashboard
            )
        }

        item {
            DashboardHeading(
                title =
                    "Upcoming Events",
                buttonText =
                    "View all",
                onButtonClick = {
                    onNavigate(
                        Routes.EVENTS
                    )
                }
            )
        }

        item {
            if (
                dashboard
                    .upcomingEvents
                    .isEmpty()
            ) {
                EmptyMessage(
                    text =
                        "No upcoming events are available."
                )
            } else {
                LazyRow(
                    contentPadding =
                        PaddingValues(
                            horizontal =
                                16.dp
                        ),
                    horizontalArrangement =
                        Arrangement.spacedBy(
                            12.dp
                        )
                ) {
                    items(
                        dashboard
                            .upcomingEvents
                            .take(3)
                    ) { event ->

                        DashboardEventCard(
                            modifier =
                                Modifier
                                    .fillParentMaxWidth(
                                        0.88f
                                    ),
                            event =
                                event,
                            onOpenEvents = {
                                onNavigate(
                                    Routes.EVENTS
                                )
                            }
                        )
                    }
                }
            }
        }

        item {
            Text(
                modifier =
                    Modifier.padding(
                        start = 16.dp,
                        top = 24.dp,
                        bottom = 12.dp
                    ),
                text =
                    "Quick Actions",
                color =
                    AppColors.DarkGreen,
                fontWeight =
                    FontWeight.Bold,
                fontSize =
                    21.sp
            )
        }

        item {
            Row(
                modifier =
                    Modifier
                        .fillMaxWidth()
                        .padding(
                            horizontal =
                                16.dp
                        ),
                horizontalArrangement =
                    Arrangement.spacedBy(
                        12.dp
                    )
            ) {
                Button(
                    modifier =
                        Modifier
                            .weight(1f)
                            .height(82.dp),
                    onClick = {
                        onNavigate(
                            Routes.SCANNER
                        )
                    },
                    colors =
                        ButtonDefaults
                            .buttonColors(
                                containerColor =
                                    AppColors.Yellow,
                                contentColor =
                                    AppColors.DarkText
                            ),
                    shape = RoundedCornerShape(16.dp)
                ) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        Text("Scan QR", fontWeight = FontWeight.Bold)
                        Text("Attendance check-in", fontSize = 11.sp)
                    }
                }

                Button(
                    modifier =
                        Modifier
                            .weight(1f)
                            .height(82.dp),
                    onClick = {
                        onNavigate(
                            Routes.EVENTS
                        )
                    },
                    colors =
                        ButtonDefaults
                            .buttonColors(
                                containerColor =
                                    AppColors.DarkGreen
                            ),
                    shape = RoundedCornerShape(16.dp)
                ) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        Text("View Events", fontWeight = FontWeight.Bold)
                        Text("Schedule and register", fontSize = 11.sp)
                    }
                }
            }
        }

        item {
            DashboardHeading(
                title =
                    "Recent Announcements",
                buttonText =
                    "View all",
                onButtonClick =
                    onOpenNotifications
            )
        }

        if (
            dashboard
                .recentAnnouncements
                .isEmpty()
        ) {
            item {
                EmptyMessage(
                    text =
                        "No announcements are available."
                )
            }
        } else {
            items(
                dashboard
                    .recentAnnouncements
                    .take(3)
            ) { announcement ->
                AnnouncementCard(
                    announcement =
                        announcement
                )
            }
        }

        item {
            DashboardHeading(
                title =
                    "Recent Matches",
                buttonText =
                    "",
                onButtonClick = {}
            )
        }

        if (
            dashboard
                .recentMatches
                .isEmpty()
        ) {
            item {
                EmptyMessage(
                    text =
                        "No match information is available."
                )
            }
        } else {
            items(
                dashboard
                    .recentMatches
                    .take(3)
            ) { match ->
                MatchCard(
                    match =
                        match
                )
            }
        }
    }
}

@Composable
private fun SummarySection(
    dashboard: PlayerDashboardResponse
) {
    Column(
        modifier =
            Modifier
                .fillMaxWidth()
                .padding(
                    horizontal =
                        16.dp
                ),
        verticalArrangement =
            Arrangement.spacedBy(
                10.dp
            )
    ) {
        SummaryCard(
            label =
                "Upcoming Events",
            value =
                dashboard
                    .summary
                    .upcomingEvents
                    .toString()
        )

        SummaryCard(
            label =
                "Registered Events",
            value =
                dashboard
                    .summary
                    .registeredEvents
                    .toString()
        )

        SummaryCard(
            label =
                "Attendance",
            value =
                "${dashboard.summary.presentAttendance}/${dashboard.summary.totalAttendance}"
        )

        SummaryCard(
            label =
                "Attendance Rate",
            value =
                "${dashboard.summary.attendanceRate}%"
        )
    }
}

@Composable
private fun SummaryCard(
    label: String,
    value: String
) {
    Card(
        modifier =
            Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(14.dp),
        colors =
            CardDefaults.cardColors(
                containerColor =
                    Color.White
            )
    ) {
        Row(
            modifier =
                Modifier
                    .fillMaxWidth()
                    .padding(16.dp),
            horizontalArrangement =
                Arrangement.SpaceBetween
        ) {
            Text(
                text = label,
                color =
                    AppColors.GreyText
            )

            Text(
                text = value,
                color =
                    AppColors.DarkGreen,
                fontWeight =
                    FontWeight.Bold
            )
        }
    }
}

@Composable
private fun DashboardEventCard(
    modifier: Modifier = Modifier,
    event: DashboardEvent,
    onOpenEvents: () -> Unit
) {
    Card(
        modifier =
            modifier,
        shape = RoundedCornerShape(16.dp),
        colors =
            CardDefaults.cardColors(
                containerColor =
                    Color.White
            )
    ) {
        Column(
            modifier =
                Modifier.padding(
                    16.dp
                ),
            verticalArrangement =
                Arrangement.spacedBy(
                    6.dp
                )
        ) {
            Text(
                text =
                    event.title,
                color =
                    AppColors.DarkGreen,
                fontWeight =
                    FontWeight.Bold,
                fontSize =
                    18.sp
            )

            Text(
                text =
                    "${event.date} • ${event.time}"
            )

            Text(
                text =
                    event.location
            )

            Text(
                text =
                    "${event.type} • ${event.status}",
                color =
                    AppColors.GreyText
            )

            Button(
                onClick =
                    onOpenEvents,
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
                    "View Details"
                )
            }
        }
    }
}

@Composable
private fun AnnouncementCard(
    announcement: DashboardAnnouncement
) {
    Card(
        modifier =
            Modifier
                .fillMaxWidth()
                .padding(
                    horizontal =
                        16.dp,
                    vertical =
                        5.dp
                ),
        shape = RoundedCornerShape(14.dp),
        colors =
            CardDefaults.cardColors(
                containerColor =
                    Color.White
            )
    ) {
        Column(
            modifier =
                Modifier.padding(
                    16.dp
                )
        ) {
            Text(
                text =
                    if (
                        announcement.isPinned
                    ) {
                        "Pinned • ${announcement.title}"
                    } else {
                        announcement.title
                    },
                color =
                    AppColors.DarkGreen,
                fontWeight =
                    FontWeight.Bold
            )

            Spacer(
                modifier =
                    Modifier.height(
                        4.dp
                    )
            )

            Text(
                text =
                    announcement.excerpt
            )

            Spacer(
                modifier =
                    Modifier.height(
                        5.dp
                    )
            )

            Text(
                text =
                    "${announcement.category} • ${announcement.date}",
                color =
                    AppColors.GreyText
            )
        }
    }
}

@Composable
private fun MatchCard(
    match: DashboardMatch
) {
    Card(
        modifier =
            Modifier
                .fillMaxWidth()
                .padding(
                    horizontal =
                        16.dp,
                    vertical =
                        5.dp
                ),
        shape = RoundedCornerShape(14.dp),
        colors =
            CardDefaults.cardColors(
                containerColor =
                    Color.White
            )
    ) {
        Column(
            modifier =
                Modifier.padding(
                    16.dp
                ),
            verticalArrangement =
                Arrangement.spacedBy(
                    5.dp
                )
        ) {
            Text(
                text =
                    "${match.teamA} vs ${match.teamB}",
                color =
                    AppColors.DarkGreen,
                fontWeight =
                    FontWeight.Bold
            )

            if (
                match.scoreA != null &&
                match.scoreB != null
            ) {
                Text(
                    text =
                        "${match.scoreA} - ${match.scoreB}",
                    fontWeight =
                        FontWeight.Bold
                )
            }

            Text(
                text =
                    "${match.date} • ${match.time}"
            )

            Text(
                text =
                    match.venue
            )

            Text(
                text =
                    "${match.tournament} • ${match.status}",
                color =
                    AppColors.GreyText
            )
        }
    }
}

@Composable
private fun EmptyMessage(
    text: String
) {
    Text(
        modifier =
            Modifier.padding(
                horizontal =
                    16.dp,
                vertical =
                    8.dp
            ),
        text = text,
        color =
            AppColors.GreyText
    )
}

@Composable
private fun DashboardHeading(
    title: String,
    buttonText: String,
    onButtonClick: () -> Unit
) {
    Row(
        modifier =
            Modifier
                .fillMaxWidth()
                .padding(
                    start =
                        16.dp,
                    end =
                        8.dp,
                    top =
                        20.dp,
                    bottom =
                        8.dp
                ),
        horizontalArrangement =
            Arrangement.SpaceBetween,
        verticalAlignment =
            Alignment.CenterVertically
    ) {
        Text(
            text = title,
            color =
                AppColors.DarkGreen,
            fontWeight =
                FontWeight.Bold,
            fontSize =
                21.sp
        )

        if (
            buttonText.isNotBlank()
        ) {
            TextButton(
                onClick =
                    onButtonClick
            ) {
                Text(
                    buttonText
                )
            }
        }
    }
}
