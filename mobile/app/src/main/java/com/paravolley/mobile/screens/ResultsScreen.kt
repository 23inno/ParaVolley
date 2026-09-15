package com.paravolley.mobile.screens

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
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
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

    val repository = remember {
        MatchesRepository(
            context.applicationContext
        )
    }

    var matches by remember {
        mutableStateOf<List<MatchResponse>>(
            emptyList()
        )
    }

    var isLoading by remember {
        mutableStateOf(true)
    }

    var errorMessage by remember {
        mutableStateOf<String?>(null)
    }

    suspend fun loadData() {
        isLoading = true
        errorMessage = null

        repository
            .getMatches()
            .onSuccess { response ->
                matches = response
            }
            .onFailure { exception ->
                errorMessage =
                    exception.message
                        ?: "Could not load match results."
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
                    .background(AppColors.DarkGreen)
                    .padding(
                        horizontal = 20.dp,
                        vertical = 18.dp
                    )
            ) {
                Text(
                    text = "Match Results",
                    color = Color.White,
                    fontWeight = FontWeight.Bold,
                    fontSize = 25.sp
                )

                Spacer(
                    modifier = Modifier.height(4.dp)
                )

                Text(
                    text = "Latest ParaVolley results and fixtures",
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
                                ?: "Could not load match results.",
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
                        Text(
                            text = "No matches are available yet.",
                            color = AppColors.GreyText,
                            textAlign = TextAlign.Center
                        )
                    }
                }

                else -> {
                    LazyColumn(
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
}

@Composable
private fun MatchResultCard(
    match: MatchResponse
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(
            containerColor = Color.White
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
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    modifier = Modifier.weight(1f),
                    text = match.tournament
                        .ifBlank { "ParaVolley Match" },
                    color = AppColors.DarkGreen,
                    fontWeight = FontWeight.Bold,
                    fontSize = 14.sp
                )

                Spacer(
                    modifier = Modifier.width(10.dp)
                )

                MatchStatusPill(
                    status = match.status
                )
            }

            Spacer(
                modifier = Modifier.height(16.dp)
            )

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
                    textAlign = TextAlign.Start
                )

                Column(
                    modifier = Modifier.padding(horizontal = 12.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Text(
                        text = matchScore(match),
                        color = AppColors.DarkGreen,
                        fontWeight = FontWeight.Bold,
                        fontSize = 22.sp
                    )

                    Text(
                        text = if (
                            match.scoreA != null &&
                            match.scoreB != null
                        ) {
                            "Final score"
                        } else {
                            "VS"
                        },
                        color = AppColors.GreyText,
                        fontSize = 11.sp
                    )
                }

                Text(
                    modifier = Modifier.weight(1f),
                    text = match.teamB,
                    color = AppColors.DarkText,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 16.sp,
                    textAlign = TextAlign.End
                )
            }

            Spacer(
                modifier = Modifier.height(16.dp)
            )

            Text(
                text = buildString {
                    append(formatMatchDate(match.date))
                    if (match.time.isNotBlank()) {
                        append("  •  ")
                        append(match.time)
                    }
                },
                color = AppColors.DarkText,
                fontWeight = FontWeight.Medium,
                fontSize = 13.sp
            )

            if (match.venue.isNotBlank()) {
                Spacer(
                    modifier = Modifier.height(4.dp)
                )

                Text(
                    text = match.venue,
                    color = AppColors.GreyText,
                    fontSize = 13.sp
                )
            }
        }
    }
}

@Composable
private fun MatchStatusPill(
    status: String
) {
    val normalized = status.lowercase()

    val backgroundColor = when (normalized) {
        "completed" -> AppColors.LightGreen
        "cancelled" -> AppColors.WarningBackground
        "inprogress" -> AppColors.WarningBackground
        "scheduled" -> AppColors.UnreadBlue
        else -> AppColors.LightBackground
    }

    val textColor = when (normalized) {
        "completed" -> AppColors.DarkGreen
        "cancelled" -> AppColors.Error
        "inprogress" -> AppColors.WarningText
        "scheduled" -> AppColors.DarkGreen
        else -> AppColors.GreyText
    }

    Surface(
        color = backgroundColor,
        shape = RoundedCornerShape(50.dp)
    ) {
        Text(
            modifier = Modifier.padding(
                horizontal = 10.dp,
                vertical = 5.dp
            ),
            text = friendlyStatus(status),
            color = textColor,
            fontWeight = FontWeight.SemiBold,
            fontSize = 11.sp
        )
    }
}

private fun matchScore(
    match: MatchResponse
): String {
    return if (
        match.scoreA != null &&
        match.scoreB != null
    ) {
        "${match.scoreA} - ${match.scoreB}"
    } else {
        "VS"
    }
}

private fun friendlyStatus(
    status: String
): String {
    return when (status.lowercase()) {
        "inprogress" -> "In progress"
        "completed" -> "Completed"
        "cancelled" -> "Cancelled"
        "scheduled" -> "Scheduled"
        else -> status
    }
}

private fun formatMatchDate(
    value: String
): String {
    val datePart = value.substringBefore("T")
    val parts = datePart.split("-")

    if (parts.size != 3) {
        return value
    }

    val year = parts[0].toIntOrNull()
        ?: return value
    val month = parts[1].toIntOrNull()
        ?: return value
    val day = parts[2].toIntOrNull()
        ?: return value

    val monthName = listOf(
        "Jan", "Feb", "Mar", "Apr",
        "May", "Jun", "Jul", "Aug",
        "Sep", "Oct", "Nov", "Dec"
    ).getOrNull(month - 1)
        ?: return value

    return "$day $monthName $year"
}
