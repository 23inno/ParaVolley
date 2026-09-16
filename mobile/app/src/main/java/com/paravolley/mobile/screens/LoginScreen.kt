package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.components.ParaVolleyLogo
import com.paravolley.mobile.data.FakePlayerRepository
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun LoginScreen(
    onLoginSuccessful: () -> Unit
) {
    var email by rememberSaveable { mutableStateOf("thabo.mokoena@paravolley.co.za") }
    var password by rememberSaveable { mutableStateOf("Password123!") }
    var passwordVisible by rememberSaveable { mutableStateOf(false) }
    var errorMessage by rememberSaveable { mutableStateOf<String?>(null) }
    var showRegisterDialog by rememberSaveable { mutableStateOf(false) }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(AppColors.LightBackground)
            .imePadding()
            .verticalScroll(rememberScrollState())
    ) {
        // Hero Header with Brand Logo
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .background(AppColors.DarkGreen)
                .padding(vertical = 40.dp, horizontal = 24.dp),
            contentAlignment = Alignment.Center
        ) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                ParaVolleyLogo(size = 110.dp, showText = true)
                Spacer(modifier = Modifier.height(10.dp))
                Text(
                    text = "Official Athlete & Member Portal",
                    color = Color.White.copy(alpha = 0.85f),
                    fontSize = 13.sp,
                    fontWeight = FontWeight.Medium
                )
            }
        }

        // Login Form Card
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .padding(20.dp)
                .offset(y = (-15).dp),
            shape = RoundedCornerShape(20.dp),
            colors = CardDefaults.cardColors(containerColor = Color.White),
            elevation = CardDefaults.cardElevation(defaultElevation = 6.dp)
        ) {
            Column(
                modifier = Modifier.padding(24.dp),
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) {
                Text(
                    text = "Athlete Login",
                    color = AppColors.DarkGreen,
                    fontWeight = FontWeight.ExtraBold,
                    fontSize = 22.sp
                )
                Text(
                    text = "Enter your credentials to access team schedules, matches and QR attendance check-ins.",
                    color = AppColors.GreyText,
                    fontSize = 13.sp,
                    lineHeight = 18.sp
                )

                OutlinedTextField(
                    value = email,
                    onValueChange = {
                        email = it
                        errorMessage = null
                    },
                    label = { Text("Email Address") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(12.dp)
                )

                OutlinedTextField(
                    value = password,
                    onValueChange = {
                        password = it
                        errorMessage = null
                    },
                    label = { Text("Password") },
                    singleLine = true,
                    visualTransformation = if (passwordVisible) VisualTransformation.None else PasswordVisualTransformation(),
                    trailingIcon = {
                        TextButton(onClick = { passwordVisible = !passwordVisible }) {
                            Text(
                                text = if (passwordVisible) "Hide" else "Show",
                                color = AppColors.Green,
                                fontWeight = FontWeight.Bold
                            )
                        }
                    },
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(12.dp)
                )

                errorMessage?.let { msg ->
                    Text(
                        text = msg,
                        color = AppColors.DangerRed,
                        fontSize = 12.sp,
                        fontWeight = FontWeight.Medium
                    )
                }

                Button(
                    onClick = {
                        if (email.isBlank() || password.isBlank()) {
                            errorMessage = "Please enter both email and password."
                        } else {
                            onLoginSuccessful()
                        }
                    },
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(52.dp),
                    shape = RoundedCornerShape(12.dp),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = AppColors.Yellow,
                        contentColor = AppColors.DarkText
                    )
                ) {
                    Text(
                        text = "Sign In",
                        fontWeight = FontWeight.ExtraBold,
                        fontSize = 16.sp
                    )
                }

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    TextButton(onClick = { showRegisterDialog = true }) {
                        Text(
                            text = "New Player? Register",
                            color = AppColors.Green,
                            fontWeight = FontWeight.Bold
                        )
                    }

                    TextButton(onClick = {
                        email = "thabo.mokoena@paravolley.co.za"
                        password = "Password123!"
                        onLoginSuccessful()
                    }) {
                        Text(
                            text = "Demo Auto-Fill",
                            color = AppColors.GreyText,
                            fontSize = 12.sp
                        )
                    }
                }
            }
        }
    }

    if (showRegisterDialog) {
        RegisterPlayerDialog(
            onDismiss = { showRegisterDialog = false },
            onRegistered = {
                showRegisterDialog = false
                onLoginSuccessful()
            }
        )
    }
}

@Composable
private fun RegisterPlayerDialog(
    onDismiss: () -> Unit,
    onRegistered: () -> Unit
) {
    var name by remember { mutableStateOf("") }
    var regEmail by remember { mutableStateOf("") }
    var phone by remember { mutableStateOf("") }
    var classification by remember { mutableStateOf("1.5 Minimal Impairment") }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = {
            Text(
                text = "Register as ParaVolley Athlete",
                fontWeight = FontWeight.Bold,
                color = AppColors.DarkGreen
            )
        },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                Text(
                    text = "Player registrations are verified according to World ParaVolley classification guidelines.",
                    fontSize = 12.sp,
                    color = AppColors.GreyText
                )
                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it },
                    label = { Text("Full Name") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = regEmail,
                    onValueChange = { regEmail = it },
                    label = { Text("Email Address") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = phone,
                    onValueChange = { phone = it },
                    label = { Text("Phone Number") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = classification,
                    onValueChange = { classification = it },
                    label = { Text("Medical Classification") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
            }
        },
        confirmButton = {
            Button(
                onClick = onRegistered,
                colors = ButtonDefaults.buttonColors(containerColor = AppColors.Yellow, contentColor = AppColors.DarkText)
            ) {
                Text("Submit Application", fontWeight = FontWeight.Bold)
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text("Cancel", color = AppColors.GreyText)
            }
        }
    )
}
