package com.paravolley.mobile.components

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.Groups
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
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
    buttonText: String,
    buttonEnabled: Boolean,
    onButtonClick: () -> Unit
) {
    val registered = registrationStatus.equals("Registered", ignoreCase = true)
    var showDetails by rememberSaveable(event.id) { mutableStateOf(false) }

    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, Color(0xFFF3F4F6)),
        elevation = CardDefaults.cardElevation(defaultElevation = 3.dp)
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(11.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.Top
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Row(
                        horizontalArrangement = Arrangement.spacedBy(7.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
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

                    Spacer(Modifier.height(10.dp))

                    Text(
                        text = event.title,
                        color = AppColors.DarkText,
                        fontWeight = FontWeight.SemiBold,
                        fontSize = 17.sp,
                        maxLines = 2,
                        overflow = TextOverflow.Ellipsis
                    )
                }
            }

            EventInfoRow(Icons.Filled.CalendarMonth, event.date)
            EventInfoRow(Icons.Filled.Schedule, event.time)
            EventInfoRow(Icons.Filled.LocationOn, event.location)

            if (showDetails) {
                HorizontalDivider(color = Color(0xFFF3F4F6))

                if (event.description.isNotBlank()) {
                    Text(
                        text = event.description,
                        color = AppColors.GreyText,
                        fontSize = 13.sp,
                        lineHeight = 19.sp
                    )
                }

                EventInfoRow(
                    icon = Icons.Filled.Groups,
                    text = "${event.participants} participants"
                )
            }

            TextButton(
                modifier = Modifier.align(Alignment.Start),
                onClick = { showDetails = !showDetails }
            ) {
                Text(
                    text = if (showDetails) "Hide Details" else "View Details",
                    color = AppColors.Green,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 13.sp
                )
            }

            Button(
                modifier = Modifier.fillMaxWidth(),
                enabled = buttonEnabled,
                onClick = onButtonClick,
                shape = RoundedCornerShape(10.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = if (registered) Color.White else AppColors.Yellow,
                    contentColor = if (registered) AppColors.Green else AppColors.DarkText,
                    disabledContainerColor = Color(0xFFF3F4F6),
                    disabledContentColor = AppColors.GreyText
                ),
                border = if (registered && buttonEnabled) {
                    BorderStroke(1.5.dp, AppColors.Green)
                } else {
                    null
                }
            ) {
                Text(
                    text = buttonText,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 14.sp
                )
            }
        }
    }
}

@Composable
private fun EventInfoRow(icon: ImageVector, text: String) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Box(
            modifier = Modifier
                .size(30.dp)
                .background(AppColors.LightGreen, CircleShape),
            contentAlignment = Alignment.Center
        ) {
            Icon(
                imageVector = icon,
                contentDescription = null,
                tint = AppColors.Green,
                modifier = Modifier.size(16.dp)
            )
        }

        Spacer(Modifier.width(9.dp))

        Text(
            modifier = Modifier.weight(1f),
            text = text,
            color = AppColors.GreyText,
            fontSize = 13.sp,
            maxLines = 2,
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
