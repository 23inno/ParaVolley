package com.paravolley.mobile.components

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Text
import com.paravolley.mobile.util.DateUtils
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.paravolley.mobile.model.SportsEvent
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun EventCard(
    event: SportsEvent,
    buttonText: String,
    onButtonClick: () -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(
            containerColor = Color.White
        ),
        elevation = CardDefaults.cardElevation(
            defaultElevation = 2.dp
        )
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(7.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Text(
                    text = event.category,
                    color = AppColors.Green
                )

                Text(
                    text = event.status,
                    color = AppColors.GreyText
                )
            }
            // Shows relative time e.g., "In 3 days" or "Passed 2 days ago"
val isPast = DateUtils.isEventPast(event.date)
val relativeTime = DateUtils.getRelativeTimeText(event.date)

Row(
    verticalAlignment = Alignment.CenterVertically,
    horizontalArrangement = Arrangement.spacedBy(6.dp)
) {
    Text(text = "🗓", fontSize = 13.sp)
    Text(
        text = "${event.date} • ${event.time}",
        color = AppColors.GreyText,
        fontSize = 13.sp
    )
    Spacer(modifier = Modifier.width(4.dp))
    Text(
        text = "($relativeTime)",
        color = if (isPast) AppColors.GreyText else AppColors.Green,
        fontWeight = FontWeight.Bold,
        fontSize = 12.sp
    )
}

            Text(
                text = event.title,
                color = AppColors.DarkGreen,
                fontWeight = FontWeight.Bold
            )

            Text(
                text = "Date: ${event.date}"
            )

            Text(
                text = "Time: ${event.time}"
            )

            Text(
                text = "Location: ${event.location}"
            )

            event.spotsRemaining?.let { remaining ->
                Text(
                    text = "$remaining spaces remaining",
                    color = AppColors.GreyText
                )
            }

            Button(
                modifier = Modifier.fillMaxWidth(),
                onClick = onButtonClick,
                colors = ButtonDefaults.buttonColors(
                    containerColor = AppColors.Yellow,
                    contentColor = AppColors.DarkText
                )
            ) {
                Text(
                    text = buttonText,
                    fontWeight = FontWeight.Bold
                )
            }
        }
    }
}
