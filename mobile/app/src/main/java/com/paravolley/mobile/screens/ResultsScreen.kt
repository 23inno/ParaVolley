package com.paravolley.mobile.screens

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.EmojiEvents
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.components.AppBottomBar
import com.paravolley.mobile.navigation.Routes
import com.paravolley.mobile.network.MatchResponse
import com.paravolley.mobile.network.MatchesRepository
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun ResultsScreen(
    onNavigate: (String) -> Unit
) {
    val context = LocalContext.current
    val repository = remember { MatchesRepository(context.applicationContext) }

    var matches by remember { mutableStateOf<List<MatchResponse>>(emptyList()) }
    var isLoading by remember { mutableStateOf(true) }
    var errorMessage by remember { mutableStateOf<String?>(null) }

    suspend fun loadData() {
        isLoading = true
        errorMessage = null

        repository.getMatches()
            .onSuccess { matches = it }
            .onFailure {
                errorMessage = it.message ?: "Could not load match results."
            }

        isLoading = false
    }

    LaunchedEffect(Unit) {
        loadData()
    }

    Scaffold(
        containerColor = AppColors.LightBackground,
        bottomBar = {
            AppBottomBar(
                selectedRoute = Routes.RESULTS,
                onNavigate = onNavigate
            )
        }
    ) { innerPadding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
        ) {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(Color.White)
                    .padding(horizontal = 20.dp, vertical = 16.dp)
            ) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Box(
                        modifier = Modifier
                            .size(42.dp)
                            .background(AppColors.LightGreen, CircleShape),
                        contentAlignment = Alignment.Center
                    ) {
                        Icon(
                            imageVector = Icons.Filled.EmojiEvents,
                            contentDescription = null,
                            tint = AppColors.Green,
                            modifier = Modifier.size(22.dp)
                        )
                    }
                    Spacer(Modifier.width(12.dp))
                    Column {
                        Text(
                            text = "Match Results",
                            color = AppColors.Green,
                            fontWeight = FontWeight.SemiBold,
                            fontSize = 20.sp
                        )
                        Text(
                            text = "Latest ParaVolley results and fixtures",
                            color = AppColors.GreyText,
                            fontSize = 12.sp
                        )
                    }
                }
            }

            HorizontalDivider(color = AppColors.Border)

            when {
                isLoading -> Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(40.dp),
                    contentAlignment = Alignment.Center
                ) {
                    CircularProgressIndicator(color = AppColors.Green)
                }

                errorMessage != null -> Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(24.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = errorMessage ?: "Could not load match results.",
                        color = AppColors.Error,
                        textAlign = TextAlign.Center
                    )
                }

                matches.isEmpty() -> Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(32.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = "No matches are available yet.",
                        color = AppColors.GreyText,
                        textAlign = TextAlign.Center
                    )
                }

                else -> LazyColumn(
                    modifier = Modifier.fillMaxSize(),
                    contentPadding = PaddingValues(16.dp),
                    verticalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    items(
                        items = matches,
                        key = { match -> match.id }
                    ) { match ->
                        MatchResultCard(match)
                    }
                }
            }
        }
    }
}

@Composable
private fun MatchResultCard(match: MatchResponse) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, Color(0xFFF3F4F6)),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(13.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    modifier = Modifier.weight(1f),
                    text = match.tournament.ifBlank { "ParaVolley Match" },
                    color = AppColors.Green,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 13.sp,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )

                Spacer(Modifier.width(10.dp))

                MatchStatusPill(status = match.status)
            }

            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    modifier = Modifier.weight(1f),
                    text = match.teamA,
                    color = AppColors.DarkText,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 16.sp,
                    textAlign = TextAlign.Start,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )

                Column(
                    modifier = Modifier.padding(horizontal = 12.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Surface(
                        color = AppColors.LightGreen,
                        shape = RoundedCornerShape(10.dp)
                    ) {
                        Text(
                            modifier = Modifier.padding(horizontal = 12.dp, vertical = 8.dp),
                            text = matchScore(match),
                            color = AppColors.Green,
                            fontWeight = FontWeight.Bold,
                            fontSize = 20.sp
                        )
                    }
                    Spacer(Modifier.height(4.dp))
                    Text(
                        text = if (match.scoreA != null && match.scoreB != null) {
                            "Final score"
                        } else {
                            "VS"
                        },
                        color = AppColors.GreyText,
                        fontSize = 10.sp
                    )
                }

                Text(
                    modifier = Modifier.weight(1f),
                    text = match.teamB,
                    color = AppColors.DarkText,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 16.sp,
                    textAlign = TextAlign.End,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )
            }

            HorizontalDivider(color = Color(0xFFF3F4F6))

            Row(verticalAlignment = Alignment.CenterVertically) {
                Box(
                    modifier = Modifier
                        .size(32.dp)
                        .background(AppColors.LightGreen, RoundedCornerShape(8.dp)),
                    contentAlignment = Alignment.Center
                ) {
                    Icon(
                        imageVector = Icons.Filled.CalendarMonth,
                        contentDescription = null,
                        tint = AppColors.Green,
                        modifier = Modifier.size(17.dp)
                    )
                }
                Spacer(Modifier.width(9.dp))
                Text(
                    text = buildString {
                        append(formatMatchDate(match.date))
                        if (match.time.isNotBlank()) {
                            append(" • ")
                            append(match.time)
                        }
                    },
                    color = AppColors.GreyText,
                    fontSize = 12.sp
                )
            }

            if (match.venue.isNotBlank()) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Box(
                        modifier = Modifier
                            .size(32.dp)
                            .background(AppColors.LightGreen, RoundedCornerShape(8.dp)),
                        contentAlignment = Alignment.Center
                    ) {
                        Icon(
                            imageVector = Icons.Filled.LocationOn,
                            contentDescription = null,
                            tint = AppColors.Green,
                            modifier = Modifier.size(17.dp)
                        )
                    }
                    Spacer(Modifier.width(9.dp))
                    Text(
                        text = match.venue,
                        color = AppColors.GreyText,
                        fontSize = 12.sp,
                        maxLines = 2,
                        overflow = TextOverflow.Ellipsis
                    )
                }
            }
        }
    }
}

@Composable
private fun MatchStatusPill(status: String) {
    val normalized = status.lowercase()

    val backgroundColor = when (normalized) {
        "completed" -> AppColors.LightGreen
        "cancelled" -> AppColors.Error.copy(alpha = 0.08f)
        "inprogress" -> AppColors.WarningBackground
        "scheduled" -> AppColors.UnreadBlue
        else -> AppColors.LightBackground
    }

    val textColor = when (normalized) {
        "completed" -> AppColors.Green
        "cancelled" -> AppColors.Error
        "inprogress" -> AppColors.WarningText
        "scheduled" -> AppColors.Green
        else -> AppColors.GreyText
    }

    Surface(
        color = backgroundColor,
        shape = RoundedCornerShape(999.dp)
    ) {
        Text(
            modifier = Modifier.padding(horizontal = 10.dp, vertical = 5.dp),
            text = friendlyStatus(status),
            color = textColor,
            fontWeight = FontWeight.SemiBold,
            fontSize = 10.sp
        )
    }
}

private fun matchScore(match: MatchResponse): String {
    return if (match.scoreA != null && match.scoreB != null) {
        "${match.scoreA} - ${match.scoreB}"
    } else {
        "VS"
    }
}

private fun friendlyStatus(status: String): String {
    return when (status.lowercase()) {
        "inprogress" -> "In progress"
        "completed" -> "Completed"
        "cancelled" -> "Cancelled"
        "scheduled" -> "Scheduled"
        else -> status
    }
}

private fun formatMatchDate(value: String): String {
    val datePart = value.substringBefore("T")
    val parts = datePart.split("-")

    if (parts.size != 3) return value

    val year = parts[0].toIntOrNull() ?: return value
    val month = parts[1].toIntOrNull() ?: return value
    val day = parts[2].toIntOrNull() ?: return value

    val monthName = listOf(
        "Jan", "Feb", "Mar", "Apr",
        "May", "Jun", "Jul", "Aug",
        "Sep", "Oct", "Nov", "Dec"
    ).getOrNull(month - 1) ?: return value

    return "$day $monthName $year"
}
