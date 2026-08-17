package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
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
    var notifications by remember {
        mutableStateOf(
            FakePlayerRepository.notifications
        )
    }

    val unreadCount =
        notifications.count {
            !it.isRead
        }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(AppColors.LightBackground)
            .safeDrawingPadding()
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .background(AppColors.DarkGreen)
                .padding(18.dp)
        ) {
            Button(
                onClick = onBack,
                colors = ButtonDefaults.buttonColors(
                    containerColor = AppColors.Yellow,
                    contentColor = AppColors.DarkText
                )
            ) {
                Text("Back")
            }

            Text(
                modifier = Modifier.padding(
                    top = 14.dp
                ),
                text = "Notifications",
                color = Color.White,
                fontSize = 25.sp,
                fontWeight = FontWeight.Bold
            )

            Text(
                text = "$unreadCount unread",
                color = Color.White
            )
        }

        LazyColumn(
            modifier = Modifier.weight(1f),
            contentPadding = PaddingValues(16.dp),
            verticalArrangement =
                Arrangement.spacedBy(10.dp)
        ) {
            item {
                Button(
                    modifier = Modifier.fillMaxWidth(),
                    onClick = {
                        notifications =
                            notifications.map {
                                it.copy(isRead = true)
                            }
                    },
                    colors = ButtonDefaults.buttonColors(
                        containerColor = AppColors.Yellow,
                        contentColor = AppColors.DarkText
                    )
                ) {
                    Text("Mark all as read")
                }
            }

            items(notifications) { notification ->
                Card(
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
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement =
                                Arrangement.SpaceBetween
                        ) {
                            Text(
                                text = notification.title,
                                color = AppColors.DarkGreen,
                                fontWeight = FontWeight.Bold
                            )

                            if (!notification.isRead) {
                                Text(
                                    text = "NEW",
                                    color = AppColors.Green,
                                    fontWeight = FontWeight.Bold
                                )
                            }
                        }

                        Text(
                            modifier = Modifier.padding(
                                top = 6.dp
                            ),
                            text = notification.message
                        )

                        Text(
                            modifier = Modifier.padding(
                                top = 6.dp
                            ),
                            text = notification.timeAgo,
                            color = AppColors.GreyText
                        )
                    }
                }
            }
        }
    }
}