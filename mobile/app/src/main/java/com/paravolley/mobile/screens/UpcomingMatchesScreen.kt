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
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.WindowInsetsSides
import androidx.compose.foundation.layout.only
import androidx.compose.foundation.layout.safeDrawing
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material.icons.filled.SportsVolleyball
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
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
import java.time.LocalDate
import java.time.LocalTime
import java.time.format.DateTimeFormatter
import java.util.Locale

@Composable
fun UpcomingMatchesScreen(
    onNavigate: (String) -> Unit
) {
    val context = LocalContext.current
    val repository = remember {
        MatchesRepository(context.applicationContext)
    }

    var matches by remember {
        mutableStateOf<List<MatchResponse>>(emptyList())
    }
    var isLoading by remember { mutableStateOf(true) }
    var errorMessage by remember { mutableStateOf<String?>(null) }

    LaunchedEffect(Unit) {
        isLoading = true
        errorMessage = null

        repository.getUpcomingMatches()
            .onSuccess { matches = it }
            .onFailure {
                errorMessage =
                    it.message ?: "Could not load upcoming matches."
            }

        isLoading = false
    }

    Scaffold(
        containerColor = AppColors.LightBackground,
        contentWindowInsets = WindowInsets.safeDrawing.only(
            WindowInsetsSides.Top + WindowInsetsSides.Horizontal
        ),
        bottomBar = {
            AppBottomBar(
                selectedRoute = Routes.UPCOMING_MATCHES,
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
                    .background(AppColors.DarkGreen)
                    .padding(horizontal = 20.dp, vertical = 18.dp)
            ) {
                Row(
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Icon(
                        imageVector = Icons.Filled.SportsVolleyball,
                        contentDescription = null,
                        tint = AppColors.Yellow,
                        modifier = Modifier.size(28.dp)
                    )

                    Spacer(Modifier.width(10.dp))

                    Text(
                        text = "Upcoming Matches",
                        color = Color.White,
                        fontWeight = FontWeight.Bold,
                        fontSize = 25.sp
                    )
                }

                Spacer(Modifier.height(4.dp))

                Text(
                    text = "Scheduled ParaVolley fixtures",
                    color = Color.White.copy(alpha = 0.82f),
                    fontSize = 14.sp
                )
            }

            when {
                isLoading -> {
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(40.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        CircularProgressIndicator(
                            color = AppColors.Green
                        )
                    }
                }

                errorMessage != null -> {
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(24.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            text = errorMessage
                                ?: "Could not load upcoming matches.",
                            color = AppColors.Error,
                            textAlign = TextAlign.Center
                        )
                    }
                }

                matches.isEmpty() -> {
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(32.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Column(
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Icon(
                                imageVector = Icons.Filled.SportsVolleyball,
                                contentDescription = null,
                                tint = AppColors.Green,
                                modifier = Modifier.size(40.dp)
                            )
                            Spacer(Modifier.height(12.dp))
                            Text(
                                text = "No upcoming matches are scheduled.",
                                color = AppColors.GreyText,
                                textAlign = TextAlign.Center
                            )
                        }
                    }
                }

                else -> {
                    LazyColumn(
                        modifier = Modifier.fillMaxSize(),
                        contentPadding = PaddingValues(16.dp),
                        verticalArrangement =
                            Arrangement.spacedBy(12.dp)
                    ) {
                        items(
                            items = matches,
                            key = { match -> match.id }
                        ) { match ->
                            UpcomingMatchCard(match)
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun UpcomingMatchCard(
    match: MatchResponse
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(
            containerColor = Color.White
        ),
        border = BorderStroke(
            1.dp,
            Color(0xFFE5E7EB)
        ),
        elevation = CardDefaults.cardElevation(
            defaultElevation = 2.dp
        )
    ) {
        Column(
            modifier = Modifier.padding(16.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    modifier = Modifier.weight(1f),
                    text = match.tournament
                        .ifBlank { "ParaVolley Match" },
                    color = AppColors.DarkGreen,
                    fontWeight = FontWeight.Bold,
                    fontSize = 14.sp,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )

                Spacer(Modifier.width(10.dp))

                Surface(
                    color = AppColors.LightGreen,
                    shape = RoundedCornerShape(999.dp)
                ) {
                    Text(
                        modifier = Modifier.padding(
                            horizontal = 10.dp,
                            vertical = 5.dp
                        ),
                        text = "Scheduled",
                        color = AppColors.DarkGreen,
                        fontSize = 11.sp,
                        fontWeight = FontWeight.SemiBold
                    )
                }
            }

            Spacer(Modifier.height(16.dp))

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
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )

                Text(
                    modifier = Modifier.padding(horizontal = 12.dp),
                    text = "VS",
                    color = AppColors.Green,
                    fontWeight = FontWeight.Bold,
                    fontSize = 17.sp
                )

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

            Spacer(Modifier.height(16.dp))

            MatchInfoRow(
                icon = Icons.Filled.CalendarMonth,
                value = formatUpcomingMatchDate(match.date)
            )

            Spacer(Modifier.height(8.dp))

            MatchInfoRow(
                icon = Icons.Filled.Schedule,
                value = formatUpcomingMatchTime(match.time)
            )

            Spacer(Modifier.height(8.dp))

            MatchInfoRow(
                icon = Icons.Filled.LocationOn,
                value = match.venue.ifBlank { "Venue TBC" }
            )
        }
    }
}

@Composable
private fun MatchInfoRow(
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    value: String
) {
    Row(
        verticalAlignment = Alignment.CenterVertically
    ) {
        Box(
            modifier = Modifier
                .size(32.dp)
                .background(
                    AppColors.LightGreen,
                    RoundedCornerShape(8.dp)
                ),
            contentAlignment = Alignment.Center
        ) {
            Icon(
                imageVector = icon,
                contentDescription = null,
                tint = AppColors.Green,
                modifier = Modifier.size(16.dp)
            )
        }

        Spacer(Modifier.width(10.dp))

        Text(
            text = value,
            color = AppColors.GreyText,
            fontSize = 13.sp,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis
        )
    }
}

private fun formatUpcomingMatchDate(
    rawDate: String
): String {
    val trimmed = rawDate.trim()
    if (trimmed.isBlank()) return "Date TBC"

    val dateOnly = trimmed
        .substringBefore("T")
        .substringBefore(" ")

    return runCatching {
        LocalDate
            .parse(dateOnly, DateTimeFormatter.ISO_LOCAL_DATE)
            .format(
                DateTimeFormatter.ofPattern(
                    "dd MMM yyyy",
                    Locale.getDefault()
                )
            )
    }.getOrDefault(dateOnly)
}

private fun formatUpcomingMatchTime(
    rawTime: String
): String {
    val trimmed = rawTime.trim()
    if (trimmed.isBlank()) return "Time TBC"

    return runCatching {
        LocalTime
            .parse(trimmed, DateTimeFormatter.ISO_LOCAL_TIME)
            .format(
                DateTimeFormatter.ofPattern(
                    "HH:mm",
                    Locale.getDefault()
                )
            )
    }.getOrDefault(trimmed)
}
