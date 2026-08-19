package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.network.models.PlayerProfileDto
import com.paravolley.mobile.ui.theme.*

@Composable
fun ProfileScreen(
    profile: PlayerProfileDto?,
    onSignOutClick: () -> Unit
) {
    Column(modifier = Modifier.fillMaxSize().background(ParaBackground)) {
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .background(ParaGreenPrimary)
                .padding(vertical = 32.dp),
            contentAlignment = Alignment.Center
        ) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Surface(
                    shape = CircleShape,
                    color = ParaGoldSecondary,
                    modifier = Modifier.size(80.dp)
                ) {
                    Box(contentAlignment = Alignment.Center) {
                        Text(
                            text = profile?.fullName?.take(2)?.uppercase() ?: "PV",
                            fontSize = 24.sp,
                            fontWeight = FontWeight.Bold,
                            color = ParaTextDark
                        )
                    }
                }
                Spacer(modifier = Modifier.height(12.dp))
                Text(
                    text = profile?.fullName ?: "Player Name",
                    color = ParaSurface,
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = "${profile?.position ?: "Athlete"} · Classification: ${profile?.classification ?: "N/A"}",
                    color = ParaSurface.copy(alpha = 0.8f),
                    fontSize = 13.sp
                )
            }
        }

        Column(modifier = Modifier.padding(20.dp)) {
            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(16.dp),
                colors = CardDefaults.cardColors(containerColor = ParaSurface),
                elevation = CardDefaults.cardElevation(2.dp)
            ) {
                Column(modifier = Modifier.padding(18.dp)) {
                    Text("Athlete Details", fontWeight = FontWeight.Bold, fontSize = 15.sp, color = ParaGreenPrimary)
                    Spacer(modifier = Modifier.height(12.dp))
                    ProfileField(label = "Email Address", value = profile?.email ?: "N/A")
                    ProfileField(label = "Emergency Contact", value = profile?.emergencyContact ?: "None on file")
                    ProfileField(label = "Medical Classification", value = profile?.classification ?: "Standard")
                }
            }

            Spacer(modifier = Modifier.height(24.dp))

            Button(
                onClick = onSignOutClick,
                modifier = Modifier.fillMaxWidth().height(48.dp),
                shape = RoundedCornerShape(12.dp),
                colors = ButtonDefaults.buttonColors(containerColor = ParaError.copy(alpha = 0.1f), contentColor = ParaError)
            ) {
                Text("Sign Out", fontWeight = FontWeight.Bold)
            }
        }
    }
}

@Composable
private fun ProfileField(label: String, value: String) {
    Column(modifier = Modifier.padding(vertical = 4.dp)) {
        Text(text = label, fontSize = 11.sp, color = ParaTextMuted)
        Text(text = value, fontSize = 14.sp, fontWeight = FontWeight.Medium, color = ParaTextDark)
    }
}
