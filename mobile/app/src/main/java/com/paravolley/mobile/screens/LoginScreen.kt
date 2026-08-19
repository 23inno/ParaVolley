package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Email
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.SportsVolleyball
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.ui.theme.*

@Composable
fun LoginScreen(
    isLoading: Boolean,
    errorMessage: String?,
    onLoginClick: (String, String) -> Unit
) {
    var email by remember { mutableStateOf("john.doe@email.com") }
    var password by remember { mutableStateOf("password") }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(ParaBackground)
            .verticalScroll(rememberScrollState())
    ) {
        // Header Green Banner
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .height(240.dp)
                .background(ParaGreenPrimary),
            contentAlignment = Alignment.Center
        ) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Surface(
                    shape = RoundedCornerShape(16.dp),
                    color = ParaGoldSecondary,
                    modifier = Modifier.size(64.dp)
                ) {
                    Box(contentAlignment = Alignment.Center) {
                        Icon(
                            imageVector = Icons.Default.SportsVolleyball,
                            contentDescription = "Logo",
                            tint = ParaGreenDark,
                            modifier = Modifier.size(36.dp)
                        )
                    }
                }
                Spacer(modifier = Modifier.height(12.dp))
                Text(
                    text = "ParaVolley Mpumalanga",
                    color = ParaSurface,
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold
                )
            }
        }

        // Login Card
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 24.dp)
                .padding(top = 180.dp),
            shape = RoundedCornerShape(20.dp),
            colors = CardDefaults.cardColors(containerColor = ParaSurface),
            elevation = CardDefaults.cardElevation(defaultElevation = 6.dp)
        ) {
            Column(modifier = Modifier.padding(24.dp)) {
                Text(
                    text = "Sign In",
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold,
                    color = ParaTextDark
                )
                Text(
                    text = "Access your training & tournament portal",
                    fontSize = 12.sp,
                    color = ParaTextMuted
                )

                Spacer(modifier = Modifier.height(20.dp))

                if (errorMessage != null) {
                    Surface(
                        color = ParaError.copy(alpha = 0.1f),
                        shape = RoundedCornerShape(8.dp),
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Text(
                            text = errorMessage,
                            color = ParaError,
                            fontSize = 12.sp,
                            modifier = Modifier.padding(8.dp)
                        )
                    }
                    Spacer(modifier = Modifier.height(12.dp))
                }

                OutlinedTextField(
                    value = email,
                    onValueChange = { email = it },
                    label = { Text("Email Address") },
                    leadingIcon = { Icon(Icons.Default.Email, contentDescription = null, tint = ParaTextMuted) },
                    singleLine = true,
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email),
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(12.dp)
                )

                Spacer(modifier = Modifier.height(14.dp))

                OutlinedTextField(
                    value = password,
                    onValueChange = { password = it },
                    label = { Text("Password") },
                    leadingIcon = { Icon(Icons.Default.Lock, contentDescription = null, tint = ParaTextMuted) },
                    singleLine = true,
                    visualTransformation = PasswordVisualTransformation(),
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(12.dp)
                )

                Spacer(modifier = Modifier.height(22.dp))

                Button(
                    onClick = { onLoginClick(email, password) },
                    enabled = !isLoading,
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(50.dp),
                    shape = RoundedCornerShape(12.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = ParaGoldSecondary, contentColor = ParaTextDark)
                ) {
                    if (isLoading) {
                        CircularProgressIndicator(modifier = Modifier.size(24.dp), color = ParaTextDark)
                    } else {
                        Text("Login", fontWeight = FontWeight.Bold, fontSize = 16.sp)
                    }
                }
            }
        }
    }
}
