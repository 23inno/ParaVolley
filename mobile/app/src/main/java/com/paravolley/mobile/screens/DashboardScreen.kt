package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.QrCodeScanner
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.components.EventCard
import com.paravolley.mobile.network.models.DashboardResponseDto
import com.paravolley.mobile.ui.theme.*

@Composable
fun DashboardScreen(
    dashboardData: DashboardResponseDto?,
    onScanClick: () -> Unit,
    onViewEventsClick: () -> Unit,
    onNotificationsClick: () -> Unit,
    onEventRegisterClick: (Int) -> Unit
) {
    Scaffold(
        containerColor = ParaBackground
    ) { paddingValues ->
        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
        ) {
            // Header Banner
            item {
                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .background(ParaGreenPrimary)
                        .padding(horizontal = 20.dp, vertical = 24.dp)
                ) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Surface(
                                shape = CircleShape,
                                color = ParaGoldSecondary,
                                modifier = Modifier.size(46.dp)
                            ) {
                                Box(contentAlignment = Alignment.Center) {
                                    Text(
                                        text = dashboardData?.playerName?.take(2)?.uppercase() ?: "PV",
                                        fontWeight = FontWeight.Bold,
                                        color = ParaTextDark
                                    )
                                }
                            }
                            Spacer(modifier = Modifier.width(12.dp))
                            Column {
                                Text(text = "Welcome back,", fontSize = 12.sp, color = ParaSurface.copy(alpha = 0.8f))
                                Text(
                                    text = dashboardData?.playerName ?: "Athlete",
                                    fontSize = 18.sp,
                                    fontWeight = FontWeight.Bold,
                                    color = ParaSurface
                                )
                            }
                        }
                        IconButton(onClick = onNotificationsClick) {
                            Icon(
                                imageVector = Icons.Default.Notifications,
                                contentDescription = "Notifications",
                                tint = ParaGoldSecondary
                            )
                        }
                    }
                }
            }

            // Quick Actions (Scan QR & View Events)
            item {
                Column(modifier = Modifier.padding(20.dp)) {
                    Text(
                        text = "Quick Actions",
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold,
                        color = ParaTextDark
                    )
                    Spacer(modifier = Modifier.height(12.dp))
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(14.dp)
                    ) {
                        Card(
                            onClick = onScanClick,
                            modifier = Modifier.weight(1f),
                            shape = RoundedCornerShape(16.dp),
                            colors = CardDefaults.cardColors(containerColor = ParaSurface),
                            elevation = CardDefaults.cardElevation(2.dp)
                        ) {
                            Column(
                                modifier = Modifier.padding(18.dp),
                                horizontalAlignment = Alignment.CenterHorizontally
                            ) {
                                Surface(
                                    shape = CircleShape,
                                    color = ParaGreenLight,
                                    modifier = Modifier.size(50.dp)
                                ) {
                                    Box(contentAlignment = Alignment.Center) {
                                        Icon(Icons.Default.QrCodeScanner, contentDescription = null, tint = ParaGreenPrimary)
                                    }
                                }
                                Spacer(modifier = Modifier.height(8.dp))
                                Text("Scan QR", fontWeight = FontWeight.Bold, fontSize = 14.sp)
                                Text("Check-in", fontSize = 11.sp, color = ParaTextMuted)
                            }
                        }

                        Card(
                            onClick = onViewEventsClick,
                            modifier = Modifier.weight(1f),
                            shape = RoundedCornerShape(16.dp),
                            colors = CardDefaults.cardColors(containerColor = ParaSurface),
                            elevation = CardDefaults.cardElevation(2.dp)
                        ) {
                            Column(
                                modifier = Modifier.padding(18.dp),
                                horizontalAlignment = Alignment.CenterHorizontally
                            ) {
                                Surface(
                                    shape = CircleShape,
                                    color = ParaWarningBg,
                                    modifier = Modifier.size(50.dp)
                                ) {
                                    Box(contentAlignment = Alignment.Center) {
                                        Icon(Icons.Default.CalendarMonth, contentDescription = null, tint = ParaWarningText)
                                    }
                                }
                                Spacer(modifier = Modifier.height(8.dp))
                                Text("Schedule", fontWeight = FontWeight.Bold, fontSize = 14.sp)
                                Text("View events", fontSize = 11.sp, color = ParaTextMuted)
                            }
                        }
                    }
                }
            }

            // Upcoming Events
            item {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 20.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = "Upcoming Events",
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold,
                        color = ParaTextDark
                    )
                    TextButton(onClick = onViewEventsClick) {
                        Text("View All", color = ParaGreenPrimary, fontWeight = FontWeight.Bold)
                    }
                }
            }

            val upcoming = dashboardData?.upcomingEvents ?: emptyList()
            if (upcoming.isEmpty()) {
                item {
                    Text(
                        text = "No upcoming events scheduled.",
                        color = ParaTextMuted,
                        fontSize = 13.sp,
                        modifier = Modifier.padding(horizontal = 20.dp, vertical = 10.dp)
                    )
                }
            } else {
                items(upcoming) { event ->
                    Box(modifier = Modifier.padding(horizontal = 20.dp, vertical = 6.dp)) {
                        EventCard(
                            event = event,
                            isRegistered = dashboardData?.registeredEventIds?.contains(event.id) == true,
                            onActionClick = { onEventRegisterClick(event.id) }
                        )
                    }
                }
            }
        }
    }
}
