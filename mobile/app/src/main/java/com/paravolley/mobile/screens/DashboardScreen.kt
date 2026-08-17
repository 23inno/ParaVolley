package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.components.AppBottomBar
import com.paravolley.mobile.components.EventCard
import com.paravolley.mobile.data.FakePlayerRepository
import com.paravolley.mobile.navigation.Routes
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun DashboardScreen(
    onNavigate: (String) -> Unit,
    onOpenNotifications: () -> Unit
) {
    val player = FakePlayerRepository.currentPlayer

    val upcomingEvents =
        FakePlayerRepository.events.filter {
            !it.isPast
        }

    val notificationPreview =
        FakePlayerRepository.notifications.take(3)

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
            contentPadding = PaddingValues(
                bottom = 24.dp
            )
        ) {
            item {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .background(AppColors.DarkGreen)
                        .padding(22.dp)
                ) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement =
                            Arrangement.SpaceBetween
                    ) {
                        Column {
                            Text(
                                text = "Welcome,",
                                color = Color.White
                            )

                            Text(
                                text = player.firstName,
                                color = Color.White,
                                fontSize = 28.sp,
                                fontWeight = FontWeight.Bold
                            )
                        }

                        Button(
                            onClick = onOpenNotifications,
                            colors = ButtonDefaults.buttonColors(
                                containerColor = AppColors.Yellow,
                                contentColor = AppColors.DarkText
                            )
                        ) {
                            Text("2 new")
                        }
                    }
                }
            }

            item {
                DashboardHeading(
                    title = "Upcoming Events",
                    buttonText = "View all",
                    onButtonClick = {
                        onNavigate(Routes.EVENTS)
                    }
                )
            }

            item {
                LazyRow(
                    contentPadding = PaddingValues(
                        horizontal = 16.dp
                    ),
                    horizontalArrangement =
                        Arrangement.spacedBy(12.dp)
                ) {
                    items(
                        upcomingEvents.take(3)
                    ) { event ->
                        Column(
                            modifier =
                                Modifier.fillParentMaxWidth(
                                    0.88f
                                )
                        ) {
                            EventCard(
                                event = event,
                                buttonText = "View details",
                                onButtonClick = {
                                    onNavigate(Routes.EVENTS)
                                }
                            )
                        }
                    }
                }
            }

            item {
                Text(
                    modifier = Modifier.padding(
                        start = 16.dp,
                        top = 24.dp,
                        bottom = 12.dp
                    ),
                    text = "Quick Actions",
                    color = AppColors.DarkGreen,
                    fontWeight = FontWeight.Bold,
                    fontSize = 21.sp
                )
            }

            item {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp),
                    horizontalArrangement =
                        Arrangement.spacedBy(12.dp)
                ) {
                    Button(
                        modifier = Modifier.weight(1f),
                        onClick = {
                            onNavigate(Routes.SCANNER)
                        },
                        colors = ButtonDefaults.buttonColors(
                            containerColor = AppColors.Yellow,
                            contentColor = AppColors.DarkText
                        )
                    ) {
                        Text("Scan QR")
                    }

                    Button(
                        modifier = Modifier.weight(1f),
                        onClick = {
                            onNavigate(Routes.EVENTS)
                        },
                        colors = ButtonDefaults.buttonColors(
                            containerColor = AppColors.DarkGreen
                        )
                    ) {
                        Text("View events")
                    }
                }
            }

            item {
                DashboardHeading(
                    title = "Notifications",
                    buttonText = "View all",
                    onButtonClick = onOpenNotifications
                )
            }

            items(notificationPreview) { notification ->
                Card(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(
                            horizontal = 16.dp,
                            vertical = 5.dp
                        ),
                    colors = CardDefaults.cardColors(
                        containerColor =
                            if (notification.isRead) {
                                Color.White
                            } else {
                                AppColors.UnreadBlue
                            }
                    )
                ) {
                    Column(
                        modifier = Modifier.padding(16.dp)
                    ) {
                        Text(
                            text = notification.title,
                            color = AppColors.DarkGreen,
                            fontWeight = FontWeight.Bold
                        )

                        Spacer(
                            modifier = Modifier.height(4.dp)
                        )

                        Text(
                            text = notification.message
                        )

                        Spacer(
                            modifier = Modifier.height(5.dp)
                        )

                        Text(
                            text = notification.timeAgo,
                            color = AppColors.GreyText
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun DashboardHeading(
    title: String,
    buttonText: String,
    onButtonClick: () -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(
                start = 16.dp,
                end = 8.dp,
                top = 20.dp,
                bottom = 8.dp
            ),
        horizontalArrangement =
            Arrangement.SpaceBetween
    ) {
        Text(
            text = title,
            color = AppColors.DarkGreen,
            fontWeight = FontWeight.Bold,
            fontSize = 21.sp
        )

        TextButton(
            onClick = onButtonClick
        ) {
            Text(buttonText)
        }
    }
}