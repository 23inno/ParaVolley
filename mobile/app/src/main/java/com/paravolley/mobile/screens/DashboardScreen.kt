package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.components.*
import com.paravolley.mobile.data.FakePlayerRepository
import com.paravolley.mobile.navigation.Routes
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun DashboardScreen(
    onNavigate: (String) -> Unit,
    onOpenNotifications: () -> Unit
) {
    val player = FakePlayerRepository.currentPlayer.value
    val upcomingEvents = FakePlayerRepository.events.filter { !it.isPast }
    val announcements = FakePlayerRepository.announcements
    val matches = FakePlayerRepository.recentMatches

    Scaffold(
        containerColor = AppColors.LightBackground,
        bottomBar = {
            AppBottomBar(
                selectedRoute = Routes.DASHBOARD,
                onNavigate = onNavigate
            )
        }
    ) { innerPadding ->
        LazyColumn(
            modifier = Modifier.padding(innerPadding),
            contentPadding = PaddingValues(bottom = 24.dp)
        ) {
            // Header: Athlete Greeting & Notification Bell
            item {
                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .background(AppColors.DarkGreen)
                        .padding(horizontal = 20.dp, vertical = 24.dp)
                ) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Row(
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.spacedBy(14.dp)
                        ) {
                            Box(
                                modifier = Modifier
                                    .size(52.dp)
                                    .clip(CircleShape)
                                    .background(AppColors.Yellow),
                                contentAlignment = Alignment.Center
                            ) {
                                Text(
                                    text = "${player.firstName.first()}${player.surname.first()}",
                                    color = AppColors.DarkText,
                                    fontWeight = FontWeight.ExtraBold,
                                    fontSize = 18.sp
                                )
                            }

                            Column {
                                Text(
                                    text = "Welcome back,",
                                    color = Color.White.copy(alpha = 0.85f),
                                    fontSize = 12.sp
                                )
                                Text(
                                    text = player.fullName,
                                    color = Color.White,
                                    fontSize = 20.sp,
                                    fontWeight = FontWeight.Bold
                                )
                                Text(
                                    text = "${player.team} • ${player.playerNumber}",
                                    color = AppColors.Yellow,
                                    fontSize = 11.sp,
                                    fontWeight = FontWeight.Medium
                                )
                            }
                        }

                        // Notifications Badge Button
                        Box(
                            modifier = Modifier
                                .size(42.dp)
                                .clip(CircleShape)
                                .background(Color.White.copy(alpha = 0.15f))
                                .clickable(onClick = onOpenNotifications),
                            contentAlignment = Alignment.Center
                        ) {
                            Text(text = "🔔", fontSize = 18.sp)
                            Box(
                                modifier = Modifier
                                    .align(Alignment.TopEnd)
                                    .offset(x = 2.dp, y = (-2).dp)
                                    .size(16.dp)
                                    .clip(CircleShape)
                                    .background(AppColors.Yellow),
                                contentAlignment = Alignment.Center
                            ) {
                                Text(
                                    text = "2",
                                    color = AppColors.DarkText,
                                    fontSize = 9.sp,
                                    fontWeight = FontWeight.Black
                                )
                            }
                        }
                    }
                }
            }

            // Summary Statistics KPI Row
            item {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp, vertical = 16.dp),
                    horizontalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    StatCard(
                        modifier = Modifier.weight(1f),
                        title = "Attendance",
                        value = "${player.attendanceRate.toInt()}%",
                        iconChar = "📈",
                        accentColor = AppColors.Green
                    )
                    StatCard(
                        modifier = Modifier.weight(1f),
                        title = "Registered",
                        value = "${FakePlayerRepository.events.count { it.isRegistered }}",
                        iconChar = "🎟️",
                        accentColor = AppColors.Yellow
                    )
                    StatCard(
                        modifier = Modifier.weight(1f),
                        title = "Matches",
                        value = "${player.totalMatches}",
                        iconChar = "🏐",
                        accentColor = Color(0xFF3B82F6)
                    )
                }
            }

            // Quick Action Bar
            item {
                Card(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp, vertical = 6.dp),
                    shape = RoundedCornerShape(16.dp),
                    colors = CardDefaults.cardColors(containerColor = AppColors.DarkGreen)
                ) {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column(modifier = Modifier.weight(1f)) {
                            Text(
                                text = "Training Attendance QR",
                                color = Color.White,
                                fontWeight = FontWeight.Bold,
                                fontSize = 16.sp
                            )
                            Text(
                                text = "Scan session code displayed by coach",
                                color = Color.White.copy(alpha = 0.8f),
                                fontSize = 12.sp
                            )
                        }

                        Button(
                            onClick = { onNavigate(Routes.SCANNER) },
                            shape = RoundedCornerShape(10.dp),
                            colors = ButtonDefaults.buttonColors(
                                containerColor = AppColors.Yellow,
                                contentColor = AppColors.DarkText
                            )
                        ) {
                            Text(text = "Scan Now", fontWeight = FontWeight.ExtraBold)
                        }
                    }
                }
            }

            // Upcoming Events Section
            item {
                SectionHeader(
                    title = "Upcoming Fixtures & Events",
                    actionText = "View All",
                    onActionClick = { onNavigate(Routes.EVENTS) }
                )
            }

            item {
                LazyRow(
                    contentPadding = PaddingValues(horizontal = 16.dp),
                    horizontalArrangement = Arrangement.spacedBy(14.dp)
                ) {
                    items(upcomingEvents.take(4)) { event ->
                        Box(modifier = Modifier.width(280.dp)) {
                            EventCard(
                                event = event,
                                buttonText = if (event.isRegistered) "✓ Registered" else "Register Participation",
                                onButtonClick = {
                                    FakePlayerRepository.toggleRegistration(event.id)
                                }
                            )
                        }
                    }
                }
            }

            // Announcements & News
            item {
                SectionHeader(
                    title = "Announcements",
                    actionText = null,
                    onActionClick = {}
                )
            }

            items(announcements) { announcement ->
                Box(modifier = Modifier.padding(horizontal = 16.dp, vertical = 5.dp)) {
                    AnnouncementCard(announcement = announcement)
                }
            }

            // Recent Matches Section
            item {
                SectionHeader(
                    title = "Team Fixtures & Results",
                    actionText = null,
                    onActionClick = {}
                )
            }

            items(matches) { match ->
                Box(modifier = Modifier.padding(horizontal = 16.dp, vertical = 5.dp)) {
                    MatchFixtureCard(match = match)
                }
            }
        }
    }
}

@Composable
private fun SectionHeader(
    title: String,
    actionText: String?,
    onActionClick: () -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(start = 16.dp, end = 16.dp, top = 20.dp, bottom = 8.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            text = title,
            color = AppColors.DarkGreen,
            fontWeight = FontWeight.ExtraBold,
            fontSize = 18.sp
        )
        if (actionText != null) {
            Text(
                text = actionText,
                color = AppColors.Green,
                fontWeight = FontWeight.Bold,
                fontSize = 13.sp,
                modifier = Modifier.clickable(onClick = onActionClick)
            )
        }
    }
}
