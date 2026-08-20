package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.network.AuthRepository
import com.paravolley.mobile.network.SessionManager
import com.paravolley.mobile.ui.theme.AppColors
import kotlinx.coroutines.launch

@Composable
fun LoginScreen(
    onLoginSuccessful: () -> Unit
) {
    var email by rememberSaveable {
        mutableStateOf("")
    }

    var password by rememberSaveable {
        mutableStateOf("")
    }

    var passwordVisible by rememberSaveable {
        mutableStateOf(false)
    }

    var errorMessage by rememberSaveable {
        mutableStateOf<String?>(null)
    }

    var isLoading by rememberSaveable {
        mutableStateOf(false)
    }

    val context = LocalContext.current

    val authRepository = remember {
        AuthRepository()
    }

    val sessionManager = remember {
        SessionManager(
            context.applicationContext
        )
    }

    val coroutineScope =
        rememberCoroutineScope()

    val loginFieldColors =
        OutlinedTextFieldDefaults.colors(
            focusedTextColor = AppColors.DarkText,
            unfocusedTextColor = AppColors.DarkText,
            disabledTextColor = AppColors.GreyText,
            cursorColor = AppColors.DarkGreen,
            focusedLabelColor = AppColors.DarkGreen,
            unfocusedLabelColor = AppColors.GreyText,
            disabledLabelColor = AppColors.GreyText,
            focusedBorderColor = AppColors.DarkGreen,
            unfocusedBorderColor = AppColors.GreyText,
            disabledBorderColor = Color.LightGray,
            focusedTrailingIconColor = AppColors.DarkGreen,
            unfocusedTrailingIconColor = AppColors.DarkGreen
        )

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(
                AppColors.LightBackground
            )
            .safeDrawingPadding()
            .verticalScroll(
                rememberScrollState()
            )
            .imePadding()
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .background(
                    AppColors.DarkGreen
                )
                .padding(
                    vertical = 36.dp
                ),
            horizontalAlignment =
                Alignment.CenterHorizontally
        ) {
            Box(
                modifier = Modifier
                    .background(
                        color = Color.White,
                        shape = CircleShape
                    )
                    .padding(18.dp),
                contentAlignment =
                    Alignment.Center
            ) {
                Text(
                    text = "PVM",
                    color =
                        AppColors.DarkGreen,
                    fontWeight =
                        FontWeight.Bold,
                    fontSize = 22.sp
                )
            }

            Spacer(
                modifier =
                    Modifier.height(16.dp)
            )

            Text(
                text =
                    "ParaVolley Mpumalanga",
                color = Color.White,
                fontWeight =
                    FontWeight.Bold,
                fontSize = 24.sp
            )
        }

        Card(
            modifier = Modifier
                .fillMaxWidth()
                .padding(20.dp),
            colors =
                CardDefaults.cardColors(
                    containerColor =
                        Color.White
                )
        ) {
            Column(
                modifier =
                    Modifier.padding(
                        20.dp
                    ),
                verticalArrangement =
                    Arrangement.spacedBy(
                        14.dp
                    )
            ) {
                Text(
                    text = "Player Login",
                    color =
                        AppColors.DarkGreen,
                    fontWeight =
                        FontWeight.Bold,
                    fontSize = 22.sp
                )

                OutlinedTextField(
                    modifier =
                        Modifier
                            .fillMaxWidth(),
                    value = email,
                    onValueChange = {
                        email = it
                        errorMessage = null
                    },
                    label = {
                        Text("Email")
                    },
                    singleLine = true,
                    enabled = !isLoading,
                    colors = loginFieldColors
                )

                OutlinedTextField(
                    modifier =
                        Modifier
                            .fillMaxWidth(),
                    value = password,
                    onValueChange = {
                        password = it
                        errorMessage = null
                    },
                    label = {
                        Text("Password")
                    },
                    singleLine = true,
                    enabled = !isLoading,
                    colors = loginFieldColors,
                    visualTransformation =
                        if (
                            passwordVisible
                        ) {
                            VisualTransformation
                                .None
                        } else {
                            PasswordVisualTransformation()
                        },
                    trailingIcon = {
                        TextButton(
                            enabled =
                                !isLoading,
                            onClick = {
                                passwordVisible =
                                    !passwordVisible
                            }
                        ) {
                            Text(
                                text =
                                    if (
                                        passwordVisible
                                    ) {
                                        "Hide"
                                    } else {
                                        "Show"
                                    }
                            )
                        }
                    }
                )

                TextButton(
                    modifier =
                        Modifier.align(
                            Alignment.End
                        ),
                    enabled = !isLoading,
                    onClick = {}
                ) {
                    Text(
                        "Forgot password?"
                    )
                }

                errorMessage?.let {
                    message ->
                    Text(
                        text = message,
                        color = Color.Red
                    )
                }

                Button(
                    modifier =
                        Modifier
                            .fillMaxWidth(),
                    enabled = !isLoading,
                    onClick = {
                        if (
                            email.isBlank() ||
                            password.isBlank()
                        ) {
                            errorMessage =
                                "Enter your email and password."

                            return@Button
                        }

                        isLoading = true
                        errorMessage = null

                        coroutineScope.launch {
                            val result =
                                authRepository
                                    .login(
                                        email =
                                            email,
                                        password =
                                            password
                                    )

                            result
                                .onSuccess {
                                    loginResponse ->

                                    isLoading =
                                        false

                                    if (
                                        loginResponse
                                            .user
                                            .role
                                            .equals(
                                                "Player",
                                                ignoreCase =
                                                    true
                                            )
                                    ) {
                                        sessionManager
                                            .saveLogin(
                                                loginResponse
                                            )

                                        onLoginSuccessful()
                                    } else {
                                        errorMessage =
                                            "This mobile app is for player accounts only."
                                    }
                                }
                                .onFailure {
                                    exception ->

                                    isLoading =
                                        false

                                    errorMessage =
                                        exception
                                            .message
                                            ?: "Login failed."
                                }
                        }
                    },
                    colors =
                        ButtonDefaults
                            .buttonColors(
                                containerColor =
                                    AppColors.Yellow,
                                contentColor =
                                    AppColors.DarkText
                            )
                ) {
                    if (isLoading) {
                        CircularProgressIndicator()
                    } else {
                        Text(
                            text = "Login",
                            fontWeight =
                                FontWeight.Bold
                        )
                    }
                }

                Text(
                    modifier =
                        Modifier
                            .fillMaxWidth(),
                    text =
                        "Sign in using your approved ParaVolley player account.",
                    color =
                        AppColors.GreyText,
                    textAlign =
                        TextAlign.Center
                )
            }
        }
    }
}
