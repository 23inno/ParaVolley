package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.network.QrAttendanceRepository
import com.paravolley.mobile.network.QrCheckInResponse
import com.paravolley.mobile.ui.theme.AppColors
import kotlinx.coroutines.launch

@Composable
fun ScannerScreen(
    onBack: () -> Unit
) {
    val context =
        LocalContext.current

    val repository =
        remember {
            QrAttendanceRepository(
                context.applicationContext
            )
        }

    val coroutineScope =
        rememberCoroutineScope()

    var qrToken by remember {
        mutableStateOf("")
    }

    var isCheckingIn by remember {
        mutableStateOf(false)
    }

    var errorMessage by remember {
        mutableStateOf<String?>(null)
    }

    var checkInResult by remember {
        mutableStateOf<QrCheckInResponse?>(
            null
        )
    }

    Box(
        modifier =
            Modifier
                .fillMaxSize()
                .background(
                    Color(0xFF111714)
                )
                .safeDrawingPadding()
    ) {
        Button(
            modifier =
                Modifier
                    .align(
                        Alignment.TopStart
                    )
                    .padding(20.dp),
            onClick =
                onBack,
            colors =
                ButtonDefaults.buttonColors(
                    containerColor =
                        AppColors.Yellow,
                    contentColor =
                        AppColors.DarkText
                )
        ) {
            Text("Back")
        }

        Column(
            modifier =
                Modifier
                    .align(
                        Alignment.Center
                    )
                    .padding(24.dp),
            horizontalAlignment =
                Alignment.CenterHorizontally,
            verticalArrangement =
                Arrangement.spacedBy(
                    18.dp
                )
        ) {
            Text(
                text =
                    "QR Attendance",
                color =
                    Color.White,
                fontWeight =
                    FontWeight.Bold,
                fontSize =
                    26.sp
            )

            Box(
                modifier =
                    Modifier
                        .size(220.dp)
                        .border(
                            width = 5.dp,
                            color =
                                AppColors.Yellow
                        ),
                contentAlignment =
                    Alignment.Center
            ) {
                Text(
                    text =
                        "QR camera scanner\nwill use this area",
                    color =
                        Color.White,
                    textAlign =
                        TextAlign.Center
                )
            }

            Text(
                text =
                    "Enter the event QR token below to test the real check-in system.",
                color =
                    Color.White,
                textAlign =
                    TextAlign.Center
            )

            OutlinedTextField(
                modifier =
                    Modifier.fillMaxWidth(),
                value =
                    qrToken,
                onValueChange = {
                    value ->

                    qrToken = value
                    errorMessage = null
                    checkInResult = null
                },
                label = {
                    Text(
                        "QR attendance token"
                    )
                },
                singleLine =
                    true,
                colors =
                    OutlinedTextFieldDefaults.colors(
                        focusedTextColor = Color.White,
                        unfocusedTextColor = Color.White,
                        cursorColor = AppColors.Yellow,
                        focusedLabelColor = AppColors.Yellow,
                        unfocusedLabelColor = Color.White,
                        focusedBorderColor = AppColors.Yellow,
                        unfocusedBorderColor = Color.White
                    )
            )

            Button(
                modifier =
                    Modifier.fillMaxWidth(),
                enabled =
                    !isCheckingIn &&
                        qrToken
                            .isNotBlank(),
                onClick = {
                    isCheckingIn = true
                    errorMessage = null
                    checkInResult = null

                    coroutineScope.launch {
                        repository
                            .checkIn(
                                qrToken
                            )
                            .onSuccess {
                                response ->

                                checkInResult =
                                    response

                                qrToken = ""
                            }
                            .onFailure {
                                exception ->

                                errorMessage =
                                    exception.message
                                        ?: "Check-in failed."
                            }

                        isCheckingIn = false
                    }
                },
                colors =
                    ButtonDefaults.buttonColors(
                        containerColor =
                            AppColors.Yellow,
                        contentColor =
                            AppColors.DarkText
                    )
            ) {
                if (isCheckingIn) {
                    CircularProgressIndicator(
                        modifier =
                            Modifier.size(
                                20.dp
                            )
                    )
                } else {
                    Text(
                        text =
                            "Check In",
                        fontWeight =
                            FontWeight.Bold
                    )
                }
            }

            errorMessage?.let {
                message ->

                Text(
                    text =
                        message,
                    color =
                        Color(0xFFFF8A80),
                    textAlign =
                        TextAlign.Center,
                    fontWeight =
                        FontWeight.Medium
                )
            }

            checkInResult?.let {
                result ->

                Column(
                    modifier =
                        Modifier
                            .fillMaxWidth()
                            .background(
                                Color.White
                            )
                            .padding(16.dp),
                    verticalArrangement =
                        Arrangement.spacedBy(
                            5.dp
                        )
                ) {
                    Text(
                        text =
                            "Check-in successful",
                        color =
                            AppColors.DarkGreen,
                        fontWeight =
                            FontWeight.Bold,
                        fontSize =
                            18.sp
                    )

                    Text(
                        text =
                            result.playerName,
                        fontWeight =
                            FontWeight.Medium
                    )

                    Text(
                        text =
                            result.eventTitle
                    )

                    Text(
                        text =
                            "${result.eventDate} • ${result.eventTime}"
                    )

                    Text(
                        text =
                            result.eventLocation
                    )

                    Text(
                        text =
                            "Attendance: ${result.status}",
                        color =
                            AppColors.Green,
                        fontWeight =
                            FontWeight.Bold
                    )
                }
            }

            Text(
                text =
                    "Camera scanning will be connected during device testing. The real backend check-in is connected here.",
                color =
                    AppColors.Yellow,
                textAlign =
                    TextAlign.Center,
                fontSize =
                    13.sp
            )
        }
    }
}
