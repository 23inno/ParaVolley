package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
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
    val player =
        FakePlayerRepository.currentPlayer

    var editing by rememberSaveable {
        mutableStateOf(false)
    }

    var location by rememberSaveable {
        mutableStateOf(player.location)
    }

    var phone by rememberSaveable {
        mutableStateOf(player.phone)
    }

    var emergencyPhone by rememberSaveable {
        mutableStateOf(
            player.emergencyContactPhone
        )
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
        LazyColumn(
            modifier = Modifier.padding(innerPadding),
            contentPadding = PaddingValues(
                bottom = 24.dp
            )
        ) {
            item {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .background(AppColors.DarkGreen)
                        .padding(24.dp),
                    horizontalAlignment =
                        Alignment.CenterHorizontally
                ) {
                    Box(
                        modifier = Modifier
                            .background(
                                color = Color.White,
                                shape = CircleShape
                            )
                            .padding(24.dp),
                        contentAlignment =
                            Alignment.Center
                    ) {
                        Text(
                            text = player.firstName
                                .first()
                                .toString(),
                            color = AppColors.DarkGreen,
                            fontWeight = FontWeight.Bold,
                            fontSize = 28.sp
                        )
                    }

                    Text(
                        modifier = Modifier.padding(
                            top = 12.dp
                        ),
                        text = player.fullName,
                        color = Color.White,
                        fontWeight = FontWeight.Bold,
                        fontSize = 25.sp
                    )

                    Text(
                        text =
                            "${player.playerNumber} • ${player.position}",
                        color = Color.White
                    )
                }
            }

            item {
                ProfileSection(
                    title = "Personal Information",
                    fields = listOf(
                        "Age" to
                                "${player.age} years old",
                        "Position" to
                                player.position,
                        "Classification" to
                                player.classification,
                        "Location" to
                                location
                    )
                )
            }

            item {
                ProfileSection(
                    title = "Contact Information",
                    fields = listOf(
                        "Email" to player.email,
                        "Phone" to phone
                    )
                )
            }

            item {
                ProfileSection(
                    title = "Emergency Contact",
                    fields = listOf(
                        "Name" to
                                player.emergencyContactName,
                        "Relationship" to
                                player.emergencyContactRelationship,
                        "Phone" to
                                emergencyPhone
                    )
                )
            }

            if (editing) {
                item {
                    Card(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp),
                        colors = CardDefaults.cardColors(
                            containerColor = Color.White
                        )
                    ) {
                        Column(
                            modifier = Modifier.padding(
                                16.dp
                            ),
                            verticalArrangement =
                                Arrangement.spacedBy(12.dp)
                        ) {
                            Text(
                                text = "Edit Profile",
                                color = AppColors.DarkGreen,
                                fontWeight = FontWeight.Bold,
                                fontSize = 20.sp
                            )

                            OutlinedTextField(
                                modifier =
                                    Modifier.fillMaxWidth(),
                                value = location,
                                onValueChange = {
                                    location = it
                                },
                                label = {
                                    Text("Location")
                                },
                                singleLine = true
                            )

                            OutlinedTextField(
                                modifier =
                                    Modifier.fillMaxWidth(),
                                value = phone,
                                onValueChange = {
                                    phone = it
                                },
                                label = {
                                    Text("Phone")
                                },
                                singleLine = true
                            )

                            OutlinedTextField(
                                modifier =
                                    Modifier.fillMaxWidth(),
                                value = emergencyPhone,
                                onValueChange = {
                                    emergencyPhone = it
                                },
                                label = {
                                    Text(
                                        "Emergency phone"
                                    )
                                },
                                singleLine = true
                            )

                            Button(
                                modifier =
                                    Modifier.fillMaxWidth(),
                                onClick = {
                                    editing = false
                                },
                                colors =
                                    ButtonDefaults.buttonColors(
                                        containerColor =
                                            AppColors.Yellow,
                                        contentColor =
                                            AppColors.DarkText
                                    )
                            ) {
                                Text("Save Changes")
                            }
                        }
                    }
                }
            } else {
                item {
                    Button(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(
                                start = 16.dp,
                                end = 16.dp,
                                top = 8.dp
                            ),
                        onClick = {
                            editing = true
                        },
                        colors =
                            ButtonDefaults.buttonColors(
                                containerColor =
                                    AppColors.Yellow,
                                contentColor =
                                    AppColors.DarkText
                            )
                    ) {
                        Text("Edit Profile")
                    }
                }
            }

            item {
                OutlinedButton(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(16.dp),
                    onClick = onLogout
                ) {
                    Text("Logout")
                }
            }
        }
    }
}

@Composable
private fun ProfileSection(
    title: String,
    fields: List<Pair<String, String>>
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(
                horizontal = 16.dp,
                vertical = 8.dp
            ),
        colors = CardDefaults.cardColors(
            containerColor = Color.White
        )
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement =
                Arrangement.spacedBy(12.dp)
        ) {
            Text(
                text = title,
                color = AppColors.DarkGreen,
                fontWeight = FontWeight.Bold,
                fontSize = 19.sp
            )

            fields.forEach { field ->
                Column {
                    Text(
                        text = field.first,
                        color = AppColors.GreyText
                    )

                    Text(
                        text = field.second,
                        fontWeight = FontWeight.Medium
                    )
                }
            }
        }
    }
}