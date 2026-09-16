package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.components.AppBottomBar
import com.paravolley.mobile.data.FakePlayerRepository
import com.paravolley.mobile.navigation.Routes
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun ProfileScreen(
    onNavigate: (String) -> Unit,
    onLogout: () -> Unit
) {
    val player = FakePlayerRepository.currentPlayer.value
    var isEditing by rememberSaveable { mutableStateOf(false) }

    var location by rememberSaveable { mutableStateOf(player.location) }
    var phone by rememberSaveable { mutableStateOf(player.phone) }
    var emergencyPhone by rememberSaveable { mutableStateOf(player.emergencyContactPhone) }

    Scaffold(
        containerColor = AppColors.LightBackground,
        bottomBar = {
            AppBottomBar(
                selectedRoute = Routes.PROFILE,
                onNavigate = onNavigate
            )
        }
    ) { innerPadding ->
        LazyColumn(
            modifier = Modifier.padding(innerPadding),
            contentPadding = PaddingValues(bottom = 24.dp)
        ) {
            // Athlete Profile Hero
            item {
                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .background(AppColors.DarkGreen)
                        .padding(vertical = 32.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        Box(
                            modifier = Modifier
                                .size(76.dp)
                                .clip(CircleShape)
                                .background(AppColors.Yellow),
                            contentAlignment = Alignment.Center
                        ) {
                            Text(
                                text = "${player.firstName.first()}${player.surname.first()}",
                                color = AppColors.DarkText,
                                fontWeight = FontWeight.Black,
                                fontSize = 28.sp
                            )
                        }

                        Spacer(modifier = Modifier.height(12.dp))

                        Text(
                            text = player.fullName,
                            color = Color.White,
                            fontWeight = FontWeight.ExtraBold,
                            fontSize = 22.sp
                        )

                        Text(
                            text = "${player.playerNumber} • ${player.position}",
                            color = AppColors.Yellow,
                            fontWeight = FontWeight.Bold,
                            fontSize = 14.sp
                        )

                        Text(
                            text = player.classification,
                            color = Color.White.copy(alpha = 0.85f),
                            fontSize = 12.sp
                        )
                    }
                }
            }

            // Athlete Information Card
            item {
                ProfileSection(
                    title = "Athlete Classification & Details",
                    items = listOf(
                        "World ParaVolley Class" to player.classification,
                        "Registered Team" to player.team,
                        "Player Position" to player.position,
                        "Age" to "${player.age} Years",
                        "Location" to location,
                        "Status" to player.status
                    )
                )
            }

            // Contact Information
            item {
                ProfileSection(
                    title = "Contact Information",
                    items = listOf(
                        "Email Address" to player.email,
                        "Phone Number" to phone
                    )
                )
            }

            // Emergency Contact
            item {
                ProfileSection(
                    title = "Emergency Contact (Next of Kin)",
                    items = listOf(
                        "Contact Name" to player.emergencyContactName,
                        "Relationship" to player.emergencyContactRelationship,
                        "Emergency Phone" to emergencyPhone
                    )
                )
            }

            // Inline Edit Mode
            if (isEditing) {
                item {
                    Card(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp),
                        shape = RoundedCornerShape(16.dp),
                        colors = CardDefaults.cardColors(containerColor = Color.White)
                    ) {
                        Column(
                            modifier = Modifier.padding(16.dp),
                            verticalArrangement = Arrangement.spacedBy(10.dp)
                        ) {
                            Text(
                                text = "Edit Personal Details",
                                fontWeight = FontWeight.Bold,
                                color = AppColors.DarkGreen,
                                fontSize = 16.sp
                            )
                            OutlinedTextField(
                                value = location,
                                onValueChange = { location = it },
                                label = { Text("Location") },
                                singleLine = true,
                                modifier = Modifier.fillMaxWidth()
                            )
                            OutlinedTextField(
                                value = phone,
                                onValueChange = { phone = it },
                                label = { Text("Phone") },
                                singleLine = true,
                                modifier = Modifier.fillMaxWidth()
                            )
                            OutlinedTextField(
                                value = emergencyPhone,
                                onValueChange = { emergencyPhone = it },
                                label = { Text("Emergency Phone") },
                                singleLine = true,
                                modifier = Modifier.fillMaxWidth()
                            )
                            Button(
                                onClick = { isEditing = false },
                                modifier = Modifier.fillMaxWidth(),
                                shape = RoundedCornerShape(10.dp),
                                colors = ButtonDefaults.buttonColors(
                                    containerColor = AppColors.Yellow,
                                    contentColor = AppColors.DarkText
                                )
                            ) {
                                Text("Save Profile Updates", fontWeight = FontWeight.Bold)
                            }
                        }
                    }
                }
            } else {
                item {
                    Box(modifier = Modifier.padding(horizontal = 16.dp, vertical = 6.dp)) {
                        Button(
                            onClick = { isEditing = true },
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(46.dp),
                            shape = RoundedCornerShape(10.dp),
                            colors = ButtonDefaults.buttonColors(
                                containerColor = AppColors.LightGreen,
                                contentColor = AppColors.DarkGreen
                            )
                        ) {
                            Text("Edit Contact Details", fontWeight = FontWeight.Bold)
                        }
                    }
                }
            }

            // Sign Out Button
            item {
                Box(modifier = Modifier.padding(horizontal = 16.dp, vertical = 10.dp)) {
                    OutlinedButton(
                        onClick = onLogout,
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(46.dp),
                        shape = RoundedCornerShape(10.dp),
                        colors = ButtonDefaults.outlinedButtonColors(contentColor = AppColors.DangerRed)
                    ) {
                        Text("Sign Out", fontWeight = FontWeight.Bold)
                    }
                }
            }
        }
    }
}

@Composable
private fun ProfileSection(
    title: String,
    items: List<Pair<String, String>>
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 6.dp),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            Text(
                text = title,
                fontWeight = FontWeight.Bold,
                color = AppColors.DarkGreen,
                fontSize = 15.sp
            )

            items.forEach { (label, value) ->
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text(text = label, color = AppColors.GreyText, fontSize = 12.sp)
                    Text(text = value, fontWeight = FontWeight.SemiBold, fontSize = 12.sp)
                }
                Divider(color = AppColors.Border.copy(alpha = 0.5f))
            }
        }
    }
}
