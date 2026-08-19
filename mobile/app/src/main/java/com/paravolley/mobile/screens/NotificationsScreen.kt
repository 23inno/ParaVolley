package com.paravolley.mobile.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Campaign
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.network.models.AnnouncementDto
import com.paravolley.mobile.ui.theme.*

@Composable
fun NotificationsScreen(announcements: List<AnnouncementDto>) {
    Column(modifier = Modifier.fillMaxSize().padding(16.dp)) {
        Text(
            text = "Announcements & Alerts",
            style = MaterialTheme.typography.titleLarge,
            fontWeight = FontWeight.Bold,
            color = ParaGreenPrimary,
            modifier = Modifier.padding(bottom = 16.dp)
        )

        if (announcements.isEmpty()) {
            Text(text = "No new announcements.", color = ParaTextMuted)
        } else {
            LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                items(announcements) { item ->
                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        shape = RoundedCornerShape(12.dp),
                        colors = CardDefaults.cardColors(containerColor = ParaSurface),
                        elevation = CardDefaults.cardElevation(2.dp)
                    ) {
                        Row(modifier = Modifier.padding(16.dp)) {
                            Icon(
                                imageVector = Icons.Default.Campaign,
                                contentDescription = null,
                                tint = ParaGoldSecondary,
                                modifier = Modifier.size(24.dp)
                            )
                            Spacer(modifier = Modifier.width(12.dp))
                            Column {
                                Text(
                                    text = item.title,
                                    fontWeight = FontWeight.Bold,
                                    fontSize = 14.sp,
                                    color = ParaTextDark
                                )
                                Spacer(modifier = Modifier.height(4.dp))
                                Text(
                                    text = item.content,
                                    fontSize = 12.sp,
                                    color = ParaTextMuted
                                )
                                Spacer(modifier = Modifier.height(6.dp))
                                Text(
                                    text = item.createdAt ?: "Recently",
                                    fontSize = 10.sp,
                                    color = ParaTextMuted.copy(alpha = 0.7f)
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}
