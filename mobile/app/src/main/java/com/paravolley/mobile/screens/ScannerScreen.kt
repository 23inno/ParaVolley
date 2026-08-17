package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun ScannerScreen(
    onBack: () -> Unit
) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF111714))
            .safeDrawingPadding()
    ) {
        Button(
            modifier = Modifier
                .align(Alignment.TopStart)
                .padding(20.dp),
            onClick = onBack,
            colors = ButtonDefaults.buttonColors(
                containerColor = AppColors.Yellow,
                contentColor = AppColors.DarkText
            )
        ) {
            Text("Back")
        }

        Column(
            modifier = Modifier
                .align(Alignment.Center)
                .padding(24.dp),
            horizontalAlignment =
                Alignment.CenterHorizontally,
            verticalArrangement =
                Arrangement.spacedBy(22.dp)
        ) {
            Text(
                text = "Scan to Check In",
                color = Color.White,
                fontWeight = FontWeight.Bold,
                fontSize = 26.sp
            )

            Box(
                modifier = Modifier
                    .size(270.dp)
                    .border(
                        width = 5.dp,
                        color = AppColors.Yellow
                    ),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text =
                        "Camera preview\nwill appear here",
                    color = Color.White,
                    textAlign = TextAlign.Center
                )
            }

            Text(
                text =
                    "Align the event QR code inside the yellow frame.",
                color = Color.White,
                textAlign = TextAlign.Center
            )

            Text(
                text =
                    "Camera integration is the next development phase.",
                color = AppColors.Yellow,
                textAlign = TextAlign.Center
            )
        }
    }
}