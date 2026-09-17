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
import androidx.compose.material.icons.filled.Badge
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Email
import androidx.compose.material.icons.filled.Groups
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.Logout
import androidx.compose.material.icons.filled.Phone
import androidx.compose.material.icons.filled.SportsVolleyball
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
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
import java.time.LocalDate
import java.time.LocalTime
import java.time.OffsetDateTime
import java.time.format.DateTimeFormatter
import java.util.Locale

private val ProfileGreen = Color(0xFF1A5F3F)
private val ProfileYellow = Color(0xFFFBBF24)
private val ProfileBackground = Color(0xFFF9FAFB)
private val ProfileBorder = Color(0xFFE5E7EB)
private val ProfileMuted = Color(0xFF6B7280)
private val ProfileText = Color(0xFF111827)

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
                if (errorMessage == null) errorMessage = it.message ?: "Could not load attendance."
            }

        isLoading = false
    }

    Scaffold(
        containerColor = ProfileBackground,
        bottomBar = {
            AppBottomBar(selectedRoute = Routes.PROFILE, onNavigate = onNavigate)
        }
    ) { innerPadding ->
        when {
            isLoading -> Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(innerPadding),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator(color = ProfileGreen)
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
    onLogout: () -> Unit
) {
    val initials = player.name
        .trim()
        .split(Regex("\\s+"))
        .filter(String::isNotBlank)
        .take(2)
        .mapNotNull { it.firstOrNull()?.uppercase() }
        .joinToString("")
        .ifBlank { "PV" }

    val total = attendance.size
    val present = attendance.count { it.status.equals("Present", true) }
    val absent = attendance.count { it.status.equals("Absent", true) }
    val rate = if (total == 0) 0.0 else present.toDouble() / total.toDouble() * 100.0

    LazyColumn(
        modifier = Modifier
            .fillMaxSize()
            .padding(innerPadding),
        contentPadding = PaddingValues(bottom = 28.dp)
    ) {
        item {
            ProfileHeader(player = player, initials = initials)
        }

        item {
            ProfileStatsRow(
                attendanceRecords = total,
                matches = player.matches,
                attendanceRate = rate
            )
        }

        item {
            ProfileInfoCard(
                title = "Personal Information",
                rows = listOf(
                    ProfileRow(Icons.Filled.CalendarMonth, "Age", "${player.age} years old"),
                    ProfileRow(Icons.Filled.SportsVolleyball, "Position", player.position),
                    ProfileRow(Icons.Filled.Groups, "Team", player.team),
                    ProfileRow(Icons.Filled.Info, "Classification", player.disability),
                    ProfileRow(Icons.Filled.Badge, "Player ID", player.id.toString()),
                    ProfileRow(Icons.Filled.CheckCircle, "Status", player.status)
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
            SectionTitle("Attendance History")
        }

        when {
            attendanceError != null && attendance.isEmpty() -> item {
                EmptyAttendanceCard(attendanceError)
            }

            attendance.isEmpty() -> item {
                EmptyAttendanceCard("No attendance records are available yet.")
            }

            else -> items(attendance, key = { it.id }) { record ->
                AttendanceCard(record)
            }
        }

        item {
            Button(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 18.dp),
                onClick = onLogout,
                colors = ButtonDefaults.buttonColors(
                    containerColor = Color.White,
                    contentColor = AppColors.Error
                ),
                border = BorderStroke(1.dp, AppColors.Error.copy(alpha = 0.28f)),
                shape = RoundedCornerShape(12.dp),
                contentPadding = PaddingValues(vertical = 13.dp)
            ) {
                Icon(
                    Icons.Filled.Logout,
                    contentDescription = null,
                    modifier = Modifier.size(19.dp)
                )
                Spacer(Modifier.width(8.dp))
                Text("Logout", fontWeight = FontWeight.SemiBold)
            }
        }
    }
}

@Composable
private fun ProfileHeader(player: PlayerProfileResponse, initials: String) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .background(ProfileGreen)
            .padding(horizontal = 20.dp, vertical = 18.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            modifier = Modifier.fillMaxWidth(),
            text = "My Profile",
            color = Color.White,
            fontSize = 19.sp,
            fontWeight = FontWeight.SemiBold,
            textAlign = TextAlign.Center
        )

        Spacer(Modifier.size(18.dp))

        Box(
            modifier = Modifier
                .size(94.dp)
                .background(Color.White.copy(alpha = 0.14f), CircleShape),
            contentAlignment = Alignment.Center
        ) {
            Box(
                modifier = Modifier
                    .size(82.dp)
                    .background(Color.White, CircleShape),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = initials,
                    color = ProfileGreen,
                    fontWeight = FontWeight.Bold,
                    fontSize = 28.sp
                )
            }

            Box(
                modifier = Modifier
                    .align(Alignment.BottomEnd)
                    .size(27.dp)
                    .background(ProfileYellow, CircleShape),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    Icons.Filled.CheckCircle,
                    contentDescription = null,
                    tint = ProfileText,
                    modifier = Modifier.size(15.dp)
                )
            }
        }

        Spacer(Modifier.size(12.dp))

        Text(
            text = player.name,
            color = Color.White,
            fontWeight = FontWeight.SemiBold,
            fontSize = 21.sp,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis
        )

        Text(
            text = listOf(player.position, player.team)
                .filter { it.isNotBlank() }
                .joinToString(" • "),
            color = Color.White.copy(alpha = 0.80f),
            fontSize = 13.sp
        )

        Surface(
            modifier = Modifier.padding(top = 10.dp),
            color = Color.White.copy(alpha = 0.14f),
            shape = RoundedCornerShape(999.dp)
        ) {
            Text(
                modifier = Modifier.padding(horizontal = 13.dp, vertical = 5.dp),
                text = player.status.ifBlank { "Player" },
                color = Color.White,
                fontSize = 11.sp,
                fontWeight = FontWeight.SemiBold
            )
        }
    }
}

@Composable
private fun ProfileStatsRow(
    attendanceRecords: Int,
    matches: Int,
    attendanceRate: Double
) {
    Surface(
        modifier = Modifier.fillMaxWidth(),
        color = Color.White,
        shadowElevation = 1.dp
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(vertical = 15.dp),
            horizontalArrangement = Arrangement.SpaceEvenly
        ) {
            ProfileStat("Attendance", attendanceRecords.toString(), Modifier.weight(1f))
            ProfileStat("Matches", matches.toString(), Modifier.weight(1f))
            ProfileStat("Rate", String.format(Locale.getDefault(), "%.0f%%", attendanceRate), Modifier.weight(1f))
        }
    }
}

@Composable
private fun ProfileStat(label: String, value: String, modifier: Modifier = Modifier) {
    Column(
        modifier = modifier,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            text = value,
            color = ProfileGreen,
            fontWeight = FontWeight.SemiBold,
            fontSize = 19.sp
        )
        Text(
            text = label,
            color = ProfileMuted,
            fontSize = 11.sp
        )
    }
}

private data class ProfileRow(
    val icon: ImageVector,
    val label: String,
    val value: String
)

@Composable
private fun ProfileInfoCard(title: String, rows: List<ProfileRow>) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 8.dp),
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, ProfileBorder),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
    ) {
        Column(
            modifier = Modifier.padding(17.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            Text(
                text = title,
                color = ProfileGreen,
                fontWeight = FontWeight.SemiBold,
                fontSize = 16.sp
            )

            rows.forEach { row ->
                ProfileInfoRow(row)
            }
        }
    }
}

@Composable
private fun ProfileInfoRow(row: ProfileRow) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Box(
            modifier = Modifier
                .size(40.dp)
                .background(Color(0xFFF0F7F3), RoundedCornerShape(10.dp)),
            contentAlignment = Alignment.Center
        ) {
            Icon(
                imageVector = row.icon,
                contentDescription = null,
                tint = ProfileGreen,
                modifier = Modifier.size(20.dp)
            )
        }

        Spacer(Modifier.width(12.dp))

        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = row.label,
                color = ProfileMuted,
                fontSize = 11.sp
            )
            Text(
                text = row.value.ifBlank { "—" },
                color = ProfileText,
                fontWeight = FontWeight.Medium,
                fontSize = 14.sp,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis
            )
        }
    }
}

@Composable
private fun SectionTitle(title: String) {
    Text(
        modifier = Modifier.padding(start = 16.dp, end = 16.dp, top = 16.dp, bottom = 7.dp),
        text = title,
        color = ProfileText,
        fontWeight = FontWeight.SemiBold,
        fontSize = 17.sp
    )
}

@Composable
private fun EmptyAttendanceCard(message: String) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 5.dp),
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, ProfileBorder)
    ) {
        Text(
            modifier = Modifier.padding(16.dp),
            text = message,
            color = ProfileMuted,
            fontSize = 13.sp
        )
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
        border = BorderStroke(1.dp, ProfileBorder),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
    ) {
        Row(
            modifier = Modifier.padding(14.dp),
            verticalAlignment = Alignment.Top
        ) {
            Box(
                modifier = Modifier
                    .size(40.dp)
                    .background(
                        if (present) Color(0xFFF0F7F3) else AppColors.Error.copy(alpha = 0.08f),
                        CircleShape
                    ),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    Icons.Filled.CheckCircle,
                    contentDescription = null,
                    tint = if (present) ProfileGreen else AppColors.Error,
                    modifier = Modifier.size(20.dp)
                )
            }

            Spacer(Modifier.width(12.dp))

            Column(
                modifier = Modifier.weight(1f),
                verticalArrangement = Arrangement.spacedBy(4.dp)
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.Top
                ) {
                    Text(
                        modifier = Modifier.weight(1f),
                        text = attendance.eventTitle,
                        color = ProfileText,
                        fontWeight = FontWeight.SemiBold,
                        maxLines = 2,
                        overflow = TextOverflow.Ellipsis
                    )

                    Spacer(Modifier.width(8.dp))

                    Surface(
                        color = if (present) Color(0xFFF0F7F3) else AppColors.Error.copy(alpha = 0.08f),
                        shape = RoundedCornerShape(999.dp)
                    ) {
                        Text(
                            modifier = Modifier.padding(horizontal = 8.dp, vertical = 3.dp),
                            text = attendance.status,
                            color = if (present) ProfileGreen else AppColors.Error,
                            fontWeight = FontWeight.SemiBold,
                            fontSize = 10.sp
                        )
                    }
                }

                Text(
                    text = "${formatProfileDate(attendance.eventDate)} • ${formatProfileTime(attendance.eventTime)}",
                    color = ProfileMuted,
                    fontSize = 12.sp
                )

                Text(
                    text = attendance.eventLocation,
                    color = ProfileMuted,
                    fontSize = 12.sp,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )
            }
        }
    }
}

private fun formatProfileDate(raw: String): String {
    val value = raw.trim()
    if (value.isBlank()) return "—"

    val output = DateTimeFormatter.ofPattern("dd MMM yyyy", Locale.ENGLISH)

    return runCatching {
        OffsetDateTime.parse(value).toLocalDate().format(output)
    }.recoverCatching {
        LocalDate.parse(value.substringBefore("T")).format(output)
    }.getOrDefault(value.substringBefore("T"))
}

private fun formatProfileTime(raw: String): String {
    val value = raw.trim()
    if (value.isBlank()) return "—"

    val output = DateTimeFormatter.ofPattern("HH:mm", Locale.ENGLISH)

    return runCatching {
        LocalTime.parse(value).format(output)
    }.recoverCatching {
        LocalTime.parse(value.substringBefore("+").substringBefore("Z")).format(output)
    }.getOrDefault(value.take(5))
}
