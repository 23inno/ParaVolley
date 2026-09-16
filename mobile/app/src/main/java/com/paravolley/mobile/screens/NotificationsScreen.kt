package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.data.FakePlayerRepository
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun NotificationsScreen(
    onBack: () -> Unit
) {
    var notifications by remember { mutableStateOf(FakePlayerRepository.notifications) }
    val unreadCount = notifications.count { !it.isRead }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(AppColors.LightBackground)
            .systemBarsPadding()
    ) {
        // Header
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .background(AppColors.DarkGreen)
                .padding(16.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Button(
                    onClick = onBack,
                    shape = RoundedCornerShape(10.dp),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = AppColors.Yellow,
                        contentColor = AppColors.DarkText
                    )
                ) {
                    Text("← Back", fontWeight = FontWeight.Bold)
                }

                Column(horizontalAlignment = Alignment.End) {
                    Text(
                        text = "Notifications",
                        color = Color.White,
                        fontSize = 20.sp,
                        fontWeight = FontWeight.Bold
                    )
                    Text(
                        text = "$unreadCount unread",
                        color = AppColors.Yellow,
                        fontSize = 11.sp,
                        fontWeight = FontWeight.Medium
                    )
                }
            }
        }

        // Actions
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 10.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = "Recent Alerts",
                fontWeight = FontWeight.Bold,
                color = AppColors.DarkText,
                fontSize = 16.sp
            )

            TextButton(
                onClick = {
                    notifications = notifications.map { it.copy(isRead = true) }.toMutableStateList()
                }
            ) {
                Text(
                    text = "Mark all as read",
                    color = AppColors.Green,
                    fontWeight = FontWeight.Bold,
                    fontSize = 12.sp
                )
            }
        }

        // Notification Items List
        LazyColumn(
            contentPadding = PaddingValues(horizontal = 16.dp, vertical = 6.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            items(notifications) { notification ->
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(14.dp),
                    colors = CardDefaults.cardColors(
                        containerColor = if (notification.isRead) Color.White else AppColors.UnreadBlue
                    ),
                    elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
                ) {
                    Row(
                        modifier = Modifier.padding(16.dp),
                        verticalAlignment = Alignment.Top,
                        horizontalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        Text(
                            text = when (notification.type) {
                                "Event" -> "🏆"
                                "Message" -> "💬"
                                "Achievement" -> "🎖️"
                                else -> "📢"
                            },
                            fontSize = 24.sp
                        )

                        Column(modifier = Modifier.weight(1f)) {
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = notification.title,
                                    fontWeight = FontWeight.Bold,
                                    color = AppColors.DarkText,
                                    fontSize = 14.sp
                                )

                                if (!notification.isRead) {
                                    Box(
                                        modifier = Modifier
                                            .clip(RoundedCornerShape(4.dp))
                                            .background(AppColors.Green)
                                            .padding(horizontal = 6.dp, vertical = 2.dp)
                                    ) {
                                        Text(
                                            text = "NEW",
                                            color = Color.White,
                                            fontWeight = FontWeight.Black,
                                            fontSize = 9.sp
                                        )
                                    }
                                }
                            }

                            Spacer(modifier = Modifier.height(4.dp))

                            Text(
                                text = notification.message,
                                color = AppColors.GreyText,
                                fontSize = 12.sp,
                                lineHeight = 16.sp
                            )

                            Spacer(modifier = Modifier.height(6.dp))

                            Text(
                                text = notification.timeAgo,
                                color = AppColors.LightGrey,
                                fontSize = 10.sp
                            )
                        }
                    }
                }
            }
        }
    }
}
