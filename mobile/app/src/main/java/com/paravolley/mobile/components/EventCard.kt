package com.paravolley.mobile.components

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CalendarToday
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.network.models.EventDto
import com.paravolley.mobile.ui.theme.*

@Composable
fun EventCard(
    event: EventDto,
    isRegistered: Boolean,
    onActionClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Card(
        modifier = modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = ParaSurface),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Surface(
                    color = if (isRegistered) ParaGreenLight else ParaWarningBg,
                    shape = RoundedCornerShape(20.dp)
                ) {
                    Text(
                        text = if (isRegistered) "Registered" else (event.type ?: "Event"),
                        color = if (isRegistered) ParaGreenPrimary else ParaWarningText,
                        fontSize = 11.sp,
                        style = MaterialTheme.typography.labelSmall,
                        modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp)
                    )
                }
            }

            Spacer(modifier = Modifier.height(8.dp))
            Text(
                text = event.title,
                style = MaterialTheme.typography.titleMedium,
                color = ParaTextDark
            )
            Spacer(modifier = Modifier.height(6.dp))

            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(
                    imageVector = Icons.Default.CalendarToday,
                    contentDescription = null,
                    tint = ParaTextMuted,
                    modifier = Modifier.size(14.dp)
                )
                Spacer(modifier = Modifier.width(6.dp))
                Text(
                    text = event.startDate ?: "Date TBA",
                    style = MaterialTheme.typography.bodySmall,
                    color = ParaTextMuted
                )
            }

            Spacer(modifier = Modifier.height(4.dp))
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(
                    imageVector = Icons.Default.LocationOn,
                    contentDescription = null,
                    tint = ParaTextMuted,
                    modifier = Modifier.size(14.dp)
                )
                Spacer(modifier = Modifier.width(6.dp))
                Text(
                    text = event.venue ?: "Mpumalanga Grounds",
                    style = MaterialTheme.typography.bodySmall,
                    color = ParaTextMuted
                )
            }

            Spacer(modifier = Modifier.height(14.dp))
            Button(
                onClick = onActionClick,
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(10.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = if (isRegistered) ParaError.copy(alpha = 0.1f) else ParaGoldSecondary,
                    contentColor = if (isRegistered) ParaError else ParaTextDark
                )
            ) {
                Text(text = if (isRegistered) "Cancel Registration" else "Register")
            }
        }
    }
}
