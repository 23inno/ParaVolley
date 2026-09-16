package com.paravolley.mobile.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.model.AnnouncementItem
import com.paravolley.mobile.model.MatchFixture
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun StatCard(
    modifier: Modifier = Modifier,
    title: String,
    value: String,
    iconChar: String,
    accentColor: Color = AppColors.Green
) {
    Card(
        modifier = modifier,
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(
            modifier = Modifier.padding(14.dp),
            verticalArrangement = Arrangement.SpaceBetween
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(text = iconChar, fontSize = 22.sp)
                Box(
                    modifier = Modifier
                        .size(8.dp)
                        .clip(RoundedCornerShape(4.dp))
                        .background(accentColor)
                )
            }
            Spacer(modifier = Modifier.height(10.dp))
            Text(
                text = value,
                fontSize = 20.sp,
                fontWeight = FontWeight.ExtraBold,
                color = AppColors.DarkText
            )
            Text(
                text = title,
                fontSize = 11.sp,
                color = AppColors.GreyText,
                fontWeight = FontWeight.Medium
            )
        }
    }
}

@Composable
fun AnnouncementCard(
    announcement: AnnouncementItem,
    modifier: Modifier = Modifier
) {
    Card(
        modifier = modifier.fillMaxWidth(),
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(
            modifier = Modifier.padding(14.dp),
            verticalArrangement = Arrangement.spacedBy(6.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                if (announcement.isPinned) {
                    Text(
                        text = "📌 PINNED",
                        color = AppColors.DarkGreen,
                        fontWeight = FontWeight.Bold,
                        fontSize = 11.sp
                    )
                }
                Text(
                    text = announcement.date,
                    color = AppColors.GreyText,
                    fontSize = 11.sp
                )
            }
            Text(
                text = announcement.title,
                fontWeight = FontWeight.Bold,
                fontSize = 15.sp,
                color = AppColors.DarkText
            )
            Text(
                text = announcement.excerpt,
                color = AppColors.GreyText,
                fontSize = 12.sp,
                lineHeight = 17.sp
            )
        }
    }
}

@Composable
fun MatchFixtureCard(
    match: MatchFixture,
    modifier: Modifier = Modifier
) {
    Card(
        modifier = modifier.fillMaxWidth(),
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(
            modifier = Modifier.padding(14.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Text(
                    text = match.tournament,
                    color = AppColors.Green,
                    fontWeight = FontWeight.Bold,
                    fontSize = 11.sp
                )
                Text(
                    text = match.status,
                    color = if (match.status == "Completed") AppColors.Green else AppColors.YellowHover,
                    fontWeight = FontWeight.Bold,
                    fontSize = 11.sp
                )
            }

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = match.teamA,
                    fontWeight = FontWeight.Bold,
                    fontSize = 14.sp,
                    modifier = Modifier.weight(1f)
                )

                if (match.scoreA != null && match.scoreB != null) {
                    Box(
                        modifier = Modifier
                            .clip(RoundedCornerShape(8.dp))
                            .background(AppColors.LightGreen)
                            .padding(horizontal = 12.dp, vertical = 4.dp)
                    ) {
                        Text(
                            text = "${match.scoreA} - ${match.scoreB}",
                            fontWeight = FontWeight.Black,
                            fontSize = 14.sp,
                            color = AppColors.DarkGreen
                        )
                    }
                } else {
                    Text(
                        text = "VS",
                        fontWeight = FontWeight.Black,
                        fontSize = 12.sp,
                        color = AppColors.GreyText
                    )
                }

                Text(
                    text = match.teamB,
                    fontWeight = FontWeight.Bold,
                    fontSize = 14.sp,
                    modifier = Modifier.weight(1f)
                )
            }

            Text(
                text = "📍 ${match.venue} • ${match.date} ${match.time}",
                fontSize = 11.sp,
                color = AppColors.GreyText
            )
        }
    }
}
