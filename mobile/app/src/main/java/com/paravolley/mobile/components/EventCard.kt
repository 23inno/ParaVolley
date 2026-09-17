package com.paravolley.mobile.components

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.network.EventResponse
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun EventCard(
    event: EventResponse,
    registrationStatus: String?,
    formattedDate: String,
    formattedTime: String,
    onViewEvent: () -> Unit
) {
    val registered = registrationStatus.equals("Registered", ignoreCase = true)

    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, AppColors.Border),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.Top
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = event.title,
                        color = AppColors.DarkText,
                        fontWeight = FontWeight.Bold,
                        fontSize = 17.sp,
                        maxLines = 2,
                        overflow = TextOverflow.Ellipsis
                    )

                    Spacer(Modifier.size(7.dp))

                    Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                        EventChip(
                            text = event.type,
                            backgroundColor = AppColors.LightGreen,
                            contentColor = AppColors.Green
                        )

                        if (registered) {
                            EventChip(
                                text = "Registered",
                                backgroundColor = AppColors.WarningBackground,
                                contentColor = AppColors.WarningText
                            )
                        } else {
                            EventChip(
                                text = event.status,
                                backgroundColor = Color(0xFFF3F4F6),
                                contentColor = AppColors.GreyText
                            )
                        }
                    }
                }
            }

            EventInfoRow(
                icon = Icons.Filled.CalendarMonth,
                text = formattedDate
            )
            EventInfoRow(
                icon = Icons.Filled.Schedule,
                text = formattedTime
            )
            EventInfoRow(
                icon = Icons.Filled.LocationOn,
                text = event.location
            )

            Button(
                modifier = Modifier.fillMaxWidth(),
                onClick = onViewEvent,
                shape = RoundedCornerShape(9.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = AppColors.Yellow,
                    contentColor = AppColors.DarkText
                )
            ) {
                Text(
                    text = "View Event",
                    fontWeight = FontWeight.Bold
                )
            }
        }
    }
}

@Composable
private fun EventInfoRow(
    icon: ImageVector,
    text: String
) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Box(
            modifier = Modifier
                .size(30.dp)
                .background(Color(0xFFF3F4F6), CircleShape),
            contentAlignment = Alignment.Center
        ) {
            Icon(
                imageVector = icon,
                contentDescription = null,
                tint = AppColors.GreyText,
                modifier = Modifier.size(16.dp)
            )
        }

        Spacer(Modifier.width(9.dp))

        Text(
            modifier = Modifier.weight(1f),
            text = text,
            color = AppColors.DarkText,
            fontSize = 13.sp,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis
        )
    }
}

@Composable
private fun EventChip(
    text: String,
    backgroundColor: Color,
    contentColor: Color
) {
    Surface(
        color = backgroundColor,
        contentColor = contentColor,
        shape = RoundedCornerShape(999.dp)
    ) {
        Text(
            text = text,
            modifier = Modifier.padding(horizontal = 9.dp, vertical = 4.dp),
            fontWeight = FontWeight.SemiBold,
            fontSize = 10.sp
        )
    }
}
