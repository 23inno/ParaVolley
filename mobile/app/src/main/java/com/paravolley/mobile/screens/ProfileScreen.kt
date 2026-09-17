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
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Badge
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Email
import androidx.compose.material.icons.filled.Groups
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.Logout
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Phone
import androidx.compose.material.icons.filled.SportsVolleyball
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.OutlinedButton
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
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.components.AppBottomBar
import com.paravolley.mobile.navigation.Routes
import com.paravolley.mobile.network.AttendanceRepository
import com.paravolley.mobile.network.AttendanceResponse
import com.paravolley.mobile.network.PlayerProfileResponse
import com.paravolley.mobile.network.PlayerRepository
import com.paravolley.mobile.network.SessionManager
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun ProfileScreen(
    onNavigate: (String) -> Unit,
    onLogout: () -> Unit
) {
    val context = LocalContext.current
    val playerRepository = remember { PlayerRepository(context.applicationContext) }
    val attendanceRepository = remember { AttendanceRepository(context.applicationContext) }
    val sessionManager = remember { SessionManager(context.applicationContext) }

    var player by remember { mutableStateOf<PlayerProfileResponse?>(null) }
    var attendance by remember { mutableStateOf<List<AttendanceResponse>>(emptyList()) }
    var isLoading by remember { mutableStateOf(true) }
    var errorMessage by remember { mutableStateOf<String?>(null) }

    LaunchedEffect(Unit) {
        isLoading = true
        errorMessage = null

        val playerResult = playerRepository.getProfile()
        val attendanceResult = attendanceRepository.getMyAttendance()

        playerResult
            .onSuccess { player = it }
            .onFailure { errorMessage = it.message ?: "Could not load profile." }

        attendanceResult
            .onSuccess { attendance = it }
            .onFailure {
                if (errorMessage == null) {
                    errorMessage = it.message ?: "Could not load attendance."
                }
            }

        isLoading = false
    }

    Scaffold(
        containerColor = AppColors.LightBackground,
        bottomBar = {
            AppBottomBar(
                selectedRoute = Routes.PROFILE,
                onNavigate = onNavigate
            )
        }
    ) { innerPadding ->
        when {
            isLoading -> Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(innerPadding),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator(color = AppColors.Green)
            }

            player == null -> Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(innerPadding)
                    .padding(24.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.Center
            ) {
                Text(
                    text = errorMessage ?: "Could not load the player profile.",
                    color = AppColors.Error,
                    textAlign = TextAlign.Center
                )
                Spacer(Modifier.size(14.dp))
                OutlinedButton(
                    onClick = {
                        sessionManager.clearSession()
                        onLogout()
                    }
                ) {
                    Text("Return to Login")
                }
            }

            else -> ProfileContent(
                player = player!!,
                attendance = attendance,
                attendanceError = errorMessage,
                innerPadding = innerPadding,
                onBack = { onNavigate(Routes.DASHBOARD) },
                onLogout = {
                    sessionManager.clearSession()
                    onLogout()
                }
            )
        }
    }
}

@Composable
private fun ProfileContent(
    player: PlayerProfileResponse,
    attendance: List<AttendanceResponse>,
    attendanceError: String?,
    innerPadding: PaddingValues,
    onBack: () -> Unit,
    onLogout: () -> Unit
) {
    val total = attendance.size
    val present = attendance.count { it.status.equals("Present", true) }
    val absent = attendance.count { it.status.equals("Absent", true) }
    val rate = if (total == 0) 0.0 else present.toDouble() / total.toDouble() * 100.0

    LazyColumn(
        modifier = Modifier
            .fillMaxSize()
            .padding(innerPadding),
        contentPadding = PaddingValues(bottom = 24.dp)
    ) {
        item {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(AppColors.Green)
                    .padding(horizontal = 8.dp, vertical = 10.dp),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Box(modifier = Modifier.fillMaxWidth()) {
                    IconButton(
                        modifier = Modifier.align(Alignment.CenterStart),
                        onClick = onBack
                    ) {
                        Icon(
                            imageVector = Icons.Filled.ArrowBack,
                            contentDescription = "Back",
                            tint = Color.White
                        )
                    }

                    Text(
                        modifier = Modifier.align(Alignment.Center),
                        text = "My Profile",
                        color = Color.White,
                        fontSize = 19.sp,
                        fontWeight = FontWeight.SemiBold
                    )
                }

                Spacer(Modifier.size(12.dp))

                Box {
                    Box(
                        modifier = Modifier
                            .size(82.dp)
                            .background(Color.White, CircleShape),
                        contentAlignment = Alignment.Center
                    ) {
                        Icon(
                            imageVector = Icons.Filled.Person,
                            contentDescription = null,
                            tint = AppColors.Green,
                            modifier = Modifier.size(42.dp)
                        )
                    }

                    Box(
                        modifier = Modifier
                            .align(Alignment.BottomEnd)
                            .size(24.dp)
                            .background(AppColors.Yellow, CircleShape),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            text = "PV",
                            color = AppColors.DarkText,
                            fontWeight = FontWeight.Bold,
                            fontSize = 8.sp
                        )
                    }
                }

                Spacer(Modifier.size(10.dp))

                Text(
                    text = player.name,
                    color = Color.White,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 19.sp,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )

                Text(
                    text = listOf(player.position, player.team)
                        .filter { it.isNotBlank() }
                        .joinToString(" • "),
                    color = Color.White.copy(alpha = 0.8f),
                    fontSize = 13.sp
                )

                Surface(
                    modifier = Modifier.padding(top = 9.dp, bottom = 8.dp),
                    color = Color.White.copy(alpha = 0.15f),
                    shape = RoundedCornerShape(999.dp)
                ) {
                    Text(
                        modifier = Modifier.padding(horizontal = 12.dp, vertical = 5.dp),
                        text = player.status,
                        color = Color.White,
                        fontSize = 11.sp,
                        fontWeight = FontWeight.SemiBold
                    )
                }
            }
        }

        item {
            ProfileStats(
                total = total,
                present = present,
                rate = rate
            )
        }

        item {
            ProfileInfoCard(
                title = "Personal Information",
                rows = listOf(
                    ProfileRow(Icons.Filled.Badge, "Player ID", player.id.toString()),
                    ProfileRow(Icons.Filled.CalendarMonth, "Age", "${player.age} years old"),
                    ProfileRow(Icons.Filled.SportsVolleyball, "Position", player.position),
                    ProfileRow(Icons.Filled.Groups, "Team", player.team),
                    ProfileRow(Icons.Filled.CheckCircle, "Matches", player.matches.toString()),
                    ProfileRow(Icons.Filled.Info, "Classification", player.disability)
                )
            )
        }

        item {
            ProfileInfoCard(
                title = "Contact Information",
                rows = listOf(
                    ProfileRow(Icons.Filled.Email, "Email", player.email),
                    ProfileRow(Icons.Filled.Phone, "Phone", player.phone)
                )
            )
        }

        item {
            Text(
                modifier = Modifier.padding(start = 16.dp, top = 20.dp, bottom = 8.dp),
                text = "Attendance History",
                color = AppColors.Green,
                fontWeight = FontWeight.SemiBold,
                fontSize = 17.sp
            )
        }

        if (attendanceError != null && attendance.isEmpty()) {
            item {
                Text(
                    modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
                    text = attendanceError,
                    color = AppColors.Error
                )
            }
        } else if (attendance.isEmpty()) {
            item {
                Text(
                    modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
                    text = "No attendance records are available yet.",
                    color = AppColors.GreyText
                )
            }
        } else {
            items(attendance, key = { it.id }) { record ->
                AttendanceCard(record)
            }
        }

        item {
            OutlinedButton(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 16.dp),
                onClick = onLogout,
                shape = RoundedCornerShape(10.dp),
                border = BorderStroke(1.5.dp, AppColors.Border),
                colors = ButtonDefaults.outlinedButtonColors(
                    contentColor = AppColors.DarkText
                )
            ) {
                Icon(
                    imageVector = Icons.Filled.Logout,
                    contentDescription = null,
                    modifier = Modifier.size(19.dp)
                )
                Spacer(Modifier.width(8.dp))
                Text("Logout", fontWeight = FontWeight.SemiBold)
            }
        }
    }
}

private data class ProfileRow(
    val icon: ImageVector,
    val label: String,
    val value: String
)

@Composable
private fun ProfileStats(
    total: Int,
    present: Int,
    rate: Double
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .background(Color.White)
            .padding(vertical = 14.dp)
    ) {
        StatValue("Records", total.toString(), Modifier.weight(1f))
        ProfileStatDivider()
        StatValue("Present", present.toString(), Modifier.weight(1f))
        ProfileStatDivider()
        StatValue("Rate", String.format("%.0f%%", rate), Modifier.weight(1f))
    }
    HorizontalDivider(color = Color(0xFFF3F4F6))
}

@Composable
private fun ProfileStatDivider() {
    Box(
        modifier = Modifier
            .width(1.dp)
            .size(width = 1.dp, height = 34.dp)
            .background(Color(0xFFF3F4F6))
    )
}

@Composable
private fun StatValue(
    label: String,
    value: String,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            text = value,
            color = AppColors.Green,
            fontWeight = FontWeight.SemiBold,
            fontSize = 18.sp
        )
        Text(
            text = label,
            color = AppColors.GreyText,
            fontSize = 10.sp
        )
    }
}

@Composable
private fun ProfileInfoCard(
    title: String,
    rows: List<ProfileRow>
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 7.dp),
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, Color(0xFFF3F4F6)),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
    ) {
        Column(
            modifier = Modifier.padding(18.dp),
            verticalArrangement = Arrangement.spacedBy(15.dp)
        ) {
            Text(
                text = title,
                color = AppColors.Green,
                fontWeight = FontWeight.SemiBold,
                fontSize = 16.sp
            )

            rows.forEach { row ->
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Box(
                        modifier = Modifier
                            .size(40.dp)
                            .background(AppColors.LightGreen, RoundedCornerShape(9.dp)),
                        contentAlignment = Alignment.Center
                    ) {
                        Icon(
                            imageVector = row.icon,
                            contentDescription = null,
                            tint = AppColors.Green,
                            modifier = Modifier.size(20.dp)
                        )
                    }

                    Spacer(Modifier.width(12.dp))

                    Column(modifier = Modifier.weight(1f)) {
                        Text(
                            text = row.label,
                            color = AppColors.GreyText,
                            fontSize = 11.sp
                        )
                        Text(
                            text = row.value.ifBlank { "—" },
                            color = AppColors.DarkText,
                            fontWeight = FontWeight.Medium,
                            fontSize = 14.sp,
                            maxLines = 2,
                            overflow = TextOverflow.Ellipsis
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun AttendanceCard(attendance: AttendanceResponse) {
    val present = attendance.status.equals("Present", true)

    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 5.dp),
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, Color(0xFFF3F4F6))
    ) {
        Row(
            modifier = Modifier.padding(14.dp),
            verticalAlignment = Alignment.Top
        ) {
            Box(
                modifier = Modifier
                    .size(40.dp)
                    .background(
                        if (present) AppColors.LightGreen else AppColors.Error.copy(alpha = 0.08f),
                        CircleShape
                    ),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Filled.CheckCircle,
                    contentDescription = null,
                    tint = if (present) AppColors.Green else AppColors.Error,
                    modifier = Modifier.size(20.dp)
                )
            }

            Spacer(Modifier.width(12.dp))

            Column(
                modifier = Modifier.weight(1f),
                verticalArrangement = Arrangement.spacedBy(3.dp)
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text(
                        modifier = Modifier.weight(1f),
                        text = attendance.eventTitle,
                        color = AppColors.DarkText,
                        fontWeight = FontWeight.SemiBold,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis
                    )
                    Text(
                        text = attendance.status,
                        color = if (present) AppColors.Green else AppColors.Error,
                        fontWeight = FontWeight.SemiBold,
                        fontSize = 11.sp
                    )
                }
                Text(
                    text = "${attendance.eventDate} • ${attendance.eventTime}",
                    color = AppColors.GreyText,
                    fontSize = 12.sp
                )
                Text(
                    text = attendance.eventLocation,
                    color = AppColors.GreyText,
                    fontSize = 12.sp
                )
            }
        }
    }
}
