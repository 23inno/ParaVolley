package com.paravolley.mobile.screens

import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.data.FakePlayerRepository
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun ScannerScreen(
    onBack: () -> Unit
) {
    var manualToken by remember { mutableStateOf("") }
    var verificationResult by remember { mutableStateOf<String?>(null) }
    var isSuccess by remember { mutableStateOf(false) }

    // Laser Animation Effect
    val infiniteTransition = rememberInfiniteTransition(label = "laser")
    val laserPosition by infiniteTransition.animateFloat(
        initialValue = 0f,
        targetValue = 1f,
        animationSpec = infiniteRepeatable(
            animation = tween(2000, easing = LinearEasing),
            repeatMode = RepeatMode.Reverse
        ),
        label = "laserY"
    )

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF0F1714))
            .systemBarsPadding()
    ) {
        Column(
            modifier = Modifier.fillMaxSize(),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.SpaceBetween
        ) {
            // Top App Bar
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(16.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Button(
                    onClick = onBack,
                    shape = RoundedCornerShape(10.dp),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = AppColors.Yellow,
                        contentColor = AppColors.DarkText
                    )
                ) {
                    Text("← Back", fontWeight = FontWeight.Bold)
                }

                Text(
                    text = "QR Attendance",
                    color = Color.White,
                    fontWeight = FontWeight.Bold,
                    fontSize = 18.sp
                )

                Box(modifier = Modifier.size(40.dp))
            }

            // QR Reticle Frame with Laser
            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) {
                Text(
                    text = "Point camera at Coach's Dynamic QR Code",
                    color = Color.White.copy(alpha = 0.9f),
                    fontSize = 14.sp,
                    textAlign = TextAlign.Center,
                    modifier = Modifier.padding(horizontal = 24.dp)
                )

                Box(
                    modifier = Modifier
                        .size(260.dp)
                        .border(4.dp, AppColors.Yellow, RoundedCornerShape(24.dp))
                        .clip(RoundedCornerShape(24.dp))
                        .background(Color.Black.copy(alpha = 0.4f)),
                    contentAlignment = Alignment.Center
                ) {
                    // Animated laser scanning line
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(3.dp)
                            .offset(y = ((laserPosition - 0.5f) * 240).dp)
                            .background(AppColors.Yellow)
                    )

                    Text(
                        text = "Camera Viewfinder\nActive",
                        color = Color.White.copy(alpha = 0.6f),
                        textAlign = TextAlign.Center,
                        fontSize = 12.sp
                    )
                }

                Text(
                    text = "Codes refresh every 15 minutes for security",
                    color = AppColors.GreyText,
                    fontSize = 12.sp
                )
            }

            // Quick Simulation & Manual Check-In Controls
            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(16.dp),
                shape = RoundedCornerShape(16.dp),
                colors = CardDefaults.cardColors(containerColor = Color(0xFF1E2623))
            ) {
                Column(
                    modifier = Modifier.padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    Button(
                        onClick = {
                            val res = FakePlayerRepository.recordCheckIn("DEMO-QR-TOKEN")
                            isSuccess = res.first
                            verificationResult = res.second
                        },
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(48.dp),
                        shape = RoundedCornerShape(10.dp),
                        colors = ButtonDefaults.buttonColors(
                            containerColor = AppColors.Yellow,
                            contentColor = AppColors.DarkText
                        )
                    ) {
                        Text("Simulate Successful QR Scan", fontWeight = FontWeight.ExtraBold)
                    }

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        OutlinedTextField(
                            value = manualToken,
                            onValueChange = { manualToken = it },
                            placeholder = { Text("Or enter 6-digit code") },
                            singleLine = true,
                            modifier = Modifier.weight(1f),
                            shape = RoundedCornerShape(10.dp),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedTextColor = Color.White,
                                unfocusedTextColor = Color.White
                            )
                        )
                        Button(
                            onClick = {
                                if (manualToken.isNotBlank()) {
                                    val res = FakePlayerRepository.recordCheckIn(manualToken)
                                    isSuccess = res.first
                                    verificationResult = res.second
                                }
                            },
                            shape = RoundedCornerShape(10.dp),
                            colors = ButtonDefaults.buttonColors(
                                containerColor = AppColors.Green,
                                contentColor = Color.White
                            )
                        ) {
                            Text("Verify")
                        }
                    }
                }
            }
        }

        // Verification Dialog Modal
        verificationResult?.let { msg ->
            AlertDialog(
                onDismissRequest = { verificationResult = null },
                title = {
                    Text(
                        text = if (isSuccess) "✓ Attendance Verified" else "Check-In Notice",
                        fontWeight = FontWeight.Bold,
                        color = if (isSuccess) AppColors.Green else AppColors.DangerRed
                    )
                },
                text = { Text(text = msg) },
                confirmButton = {
                    Button(
                        onClick = {
                            verificationResult = null
                            onBack()
                        },
                        colors = ButtonDefaults.buttonColors(containerColor = AppColors.Yellow, contentColor = AppColors.DarkText)
                    ) {
                        Text("Done", fontWeight = FontWeight.Bold)
                    }
                }
            )
        }
    }
}
