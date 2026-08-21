package com.paravolley.mobile.components

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
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
    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp),
        colors =
            CardDefaults.cardColors(
                containerColor = Color.White
            ),
        elevation =
            CardDefaults.cardElevation(
                defaultElevation = 2.dp
            )
    ) {
        Column(
            modifier =
                Modifier.padding(
                    16.dp
                ),
            verticalArrangement =
                Arrangement.spacedBy(
                    7.dp
                )
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                EventChip(event.type, AppColors.LightGreen, AppColors.Green)
                EventChip(
                    registrationStatus ?: event.status,
                    if (registrationStatus.equals("Registered", true)) {
                        AppColors.WarningBackground
                    } else {
                        AppColors.LightGreen
                    },
                    if (registrationStatus.equals("Registered", true)) {
                        AppColors.WarningText
                    } else {
                        AppColors.Green
                    }
                )
            }

            Text(
                text =
                    event.title,
                color =
                    AppColors.DarkGreen,
                fontWeight =
                    FontWeight.Bold,
                style = androidx.compose.material3.MaterialTheme.typography.titleMedium
            )

            Text(
                text = "${event.date}  •  ${event.time}",
                color = AppColors.DarkText,
                fontWeight = FontWeight.Medium
            )

            Text(
                text = event.location,
                color = AppColors.DarkText
            )

            Text(
                text =
                    "Participants: ${event.participants}",
                color =
                    AppColors.GreyText
            )

            if (
                event.description
                    .isNotBlank()
            ) {
                Text(
                    text =
                        event.description,
                    color =
                        AppColors.GreyText
                )
            }

            Button(
                modifier = Modifier.fillMaxWidth(),
                enabled =
                    buttonEnabled,
                onClick =
                    onButtonClick,
                shape = RoundedCornerShape(12.dp),
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
                    text =
                        buttonText,
                    fontWeight =
                        FontWeight.Bold
                )
            }
        }
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
            modifier = Modifier.padding(horizontal = 10.dp, vertical = 5.dp),
            fontWeight = FontWeight.SemiBold,
            style = androidx.compose.material3.MaterialTheme.typography.labelMedium
        )
    }
}
