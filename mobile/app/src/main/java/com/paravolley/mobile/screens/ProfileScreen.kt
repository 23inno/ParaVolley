package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
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
    val context =
        LocalContext.current

    val playerRepository =
        remember {
            PlayerRepository(
                context.applicationContext
            )
        }

    val attendanceRepository =
        remember {
            AttendanceRepository(
                context.applicationContext
            )
        }

    val sessionManager =
        remember {
            SessionManager(
                context.applicationContext
            )
        }

    var player by remember {
        mutableStateOf<PlayerProfileResponse?>(
            null
        )
    }

    var attendance by remember {
        mutableStateOf<List<AttendanceResponse>>(
            emptyList()
        )
    }

    var isLoading by remember {
        mutableStateOf(true)
    }

    var errorMessage by remember {
        mutableStateOf<String?>(null)
    }

    LaunchedEffect(Unit) {
        isLoading = true
        errorMessage = null

        val playerResult =
            playerRepository
                .getProfile()

        val attendanceResult =
            attendanceRepository
                .getMyAttendance()

        playerResult
            .onSuccess { response ->
                player = response
            }
            .onFailure { exception ->
                errorMessage =
                    exception.message
                        ?: "Could not load profile."
            }

        attendanceResult
            .onSuccess { response ->
                attendance = response
            }
            .onFailure { exception ->
                if (errorMessage == null) {
                    errorMessage =
                        exception.message
                            ?: "Could not load attendance."
                }
            }

        isLoading = false
    }

    Scaffold(
        containerColor =
            AppColors.LightBackground,
        bottomBar = {
            AppBottomBar(
                selectedRoute =
                    Routes.PROFILE,
                onNavigate =
                    onNavigate
            )
        }
    ) { innerPadding ->

        when {
            isLoading -> {
                Box(
                    modifier =
                        Modifier
                            .padding(
                                innerPadding
                            )
                            .fillMaxWidth()
                            .padding(40.dp),
                    contentAlignment =
                        Alignment.Center
                ) {
                    CircularProgressIndicator()
                }
            }

            player == null -> {
                Column(
                    modifier =
                        Modifier
                            .padding(
                                innerPadding
                            )
                            .fillMaxWidth()
                            .padding(24.dp),
                    horizontalAlignment =
                        Alignment.CenterHorizontally,
                    verticalArrangement =
                        Arrangement.spacedBy(
                            16.dp
                        )
                ) {
                    Text(
                        text =
                            errorMessage
                                ?: "Could not load the player profile.",
                        color =
                            Color.Red
                    )

                    OutlinedButton(
                        onClick = {
                            sessionManager
                                .clearSession()

                            onLogout()
                        }
                    ) {
                        Text(
                            "Return to Login"
                        )
                    }
                }
            }

            else -> {
                ProfileContent(
                    player =
                        player!!,
                    attendance =
                        attendance,
                    attendanceError =
                        errorMessage,
                    innerPadding =
                        innerPadding,
                    onLogout = {
                        sessionManager
                            .clearSession()

                        onLogout()
                    }
                )
            }
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
    val totalAttendance =
        attendance.size

    val presentAttendance =
        attendance.count {
            item ->
            item.status.equals(
                "Present",
                ignoreCase = true
            )
        }

    val absentAttendance =
        attendance.count {
            item ->
            item.status.equals(
                "Absent",
                ignoreCase = true
            )
        }

    val attendanceRate =
        if (totalAttendance == 0) {
            0.0
        } else {
            presentAttendance
                .toDouble() /
                totalAttendance
                .toDouble() *
                100.0
        }

    LazyColumn(
        modifier =
            Modifier.padding(
                innerPadding
            ),
        contentPadding =
            PaddingValues(
                bottom = 24.dp
            )
    ) {
        item {
            Column(
                modifier =
                    Modifier
                        .fillMaxWidth()
                        .background(
                            AppColors.DarkGreen
                        )
                        .padding(24.dp),
                horizontalAlignment =
                    Alignment.CenterHorizontally
            ) {
                Box(
                    modifier =
                        Modifier
                            .background(
                                color =
                                    Color.White,
                                shape =
                                    CircleShape
                            )
                            .padding(24.dp),
                    contentAlignment =
                        Alignment.Center
                ) {
                    Text(
                        text =
                            player.name
                                .trim()
                                .firstOrNull()
                                ?.uppercase()
                                ?: "P",
                        color =
                            AppColors.DarkGreen,
                        fontWeight =
                            FontWeight.Bold,
                        fontSize =
                            28.sp
                    )
                }

                Text(
                    modifier =
                        Modifier.padding(
                            top = 12.dp
                        ),
                    text =
                        player.name,
                    color =
                        Color.White,
                    fontWeight =
                        FontWeight.Bold,
                    fontSize =
                        25.sp
                )

                Text(
                    text =
                        player.position,
                    color =
                        Color.White
                )

                Text(
                    text =
                        player.team,
                    color =
                        Color.White
                )
            }
        }

        item {
            ProfileSection(
                title =
                    "Player Information",
                fields =
                    listOf(
                        "Player ID" to
                            player.id
                                .toString(),

                        "Age" to
                            "${player.age} years old",

                        "Position" to
                            player.position,

                        "Team" to
                            player.team,

                        "Status" to
                            player.status,

                        "Matches" to
                            player.matches
                                .toString(),

                        "Disability" to
                            player.disability
                    )
            )
        }

        item {
            ProfileSection(
                title =
                    "Contact Information",
                fields =
                    listOf(
                        "Email" to
                            player.email,

                        "Phone" to
                            player.phone
                    )
            )
        }

        item {
            ProfileSection(
                title =
                    "Attendance Summary",
                fields =
                    listOf(
                        "Total Records" to
                            totalAttendance
                                .toString(),

                        "Present" to
                            presentAttendance
                                .toString(),

                        "Absent" to
                            absentAttendance
                                .toString(),

                        "Attendance Rate" to
                            String.format(
                                "%.1f%%",
                                attendanceRate
                            )
                    )
            )
        }

        item {
            Text(
                modifier =
                    Modifier.padding(
                        start = 16.dp,
                        top = 18.dp,
                        bottom = 6.dp
                    ),
                text =
                    "Attendance History",
                color =
                    AppColors.DarkGreen,
                fontWeight =
                    FontWeight.Bold,
                fontSize =
                    21.sp
            )
        }

        if (
            attendanceError != null &&
            attendance.isEmpty()
        ) {
            item {
                Text(
                    modifier =
                        Modifier.padding(
                            horizontal =
                                16.dp,
                            vertical =
                                8.dp
                        ),
                    text =
                        attendanceError,
                    color =
                        Color.Red
                )
            }
        } else if (
            attendance.isEmpty()
        ) {
            item {
                Text(
                    modifier =
                        Modifier.padding(
                            horizontal =
                                16.dp,
                            vertical =
                                8.dp
                        ),
                    text =
                        "No attendance records are available yet.",
                    color =
                        AppColors.GreyText
                )
            }
        } else {
            items(
                items =
                    attendance,
                key = {
                    record ->
                    record.id
                }
            ) { record ->
                AttendanceCard(
                    attendance =
                        record
                )
            }
        }

        item {
            Button(
                modifier =
                    Modifier
                        .fillMaxWidth()
                        .padding(16.dp),
                onClick =
                    onLogout,
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
                        "Logout",
                    fontWeight =
                        FontWeight.Bold
                )
            }
        }
    }
}

@Composable
private fun AttendanceCard(
    attendance: AttendanceResponse
) {
    Card(
        modifier =
            Modifier
                .fillMaxWidth()
                .padding(
                    horizontal = 16.dp,
                    vertical = 6.dp
                ),
        colors =
            CardDefaults.cardColors(
                containerColor =
                    Color.White
            )
    ) {
        Column(
            modifier =
                Modifier.padding(
                    16.dp
                ),
            verticalArrangement =
                Arrangement.spacedBy(
                    5.dp
                )
        ) {
            Text(
                text =
                    attendance.eventTitle,
                color =
                    AppColors.DarkGreen,
                fontWeight =
                    FontWeight.Bold
            )

            Text(
                text =
                    "Status: ${attendance.status}",
                color =
                    if (
                        attendance.status
                            .equals(
                                "Present",
                                ignoreCase =
                                    true
                            )
                    ) {
                        AppColors.Green
                    } else {
                        Color.Red
                    },
                fontWeight =
                    FontWeight.Medium
            )

            Text(
                text =
                    "Event date: ${attendance.eventDate}"
            )

            Text(
                text =
                    "Time: ${attendance.eventTime}"
            )

            Text(
                text =
                    "Location: ${attendance.eventLocation}"
            )

            Text(
                text =
                    "Attendance date: ${attendance.attendanceDate}",
                color =
                    AppColors.GreyText
            )
        }
    }
}

@Composable
private fun ProfileSection(
    title: String,
    fields: List<Pair<String, String>>
) {
    Card(
        modifier =
            Modifier
                .fillMaxWidth()
                .padding(
                    horizontal = 16.dp,
                    vertical = 8.dp
                ),
        colors =
            CardDefaults.cardColors(
                containerColor =
                    Color.White
            )
    ) {
        Column(
            modifier =
                Modifier.padding(
                    16.dp
                ),
            verticalArrangement =
                Arrangement.spacedBy(
                    12.dp
                )
        ) {
            Text(
                text =
                    title,
                color =
                    AppColors.DarkGreen,
                fontWeight =
                    FontWeight.Bold,
                fontSize =
                    19.sp
            )

            fields.forEach {
                field ->

                Column {
                    Text(
                        text =
                            field.first,
                        color =
                            AppColors.GreyText
                    )

                    Text(
                        text =
                            field.second,
                        fontWeight =
                            FontWeight.Medium
                    )
                }
            }
        }
    }
}