package com.paravolley.mobile.screens

import androidx.compose.foundation.Image
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
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Email
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Visibility
import androidx.compose.material.icons.filled.VisibilityOff
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Surface
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
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalUriHandler
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.BuildConfig
import com.paravolley.mobile.R
import com.paravolley.mobile.network.AuthRepository
import com.paravolley.mobile.network.SessionManager
import com.paravolley.mobile.ui.theme.AppColors
import kotlinx.coroutines.launch

@Composable
fun LoginScreen(
    onLoginSuccessful: () -> Unit,
    onRegister: () -> Unit
) {
    var email by rememberSaveable { mutableStateOf("") }
    var password by rememberSaveable { mutableStateOf("") }
    var passwordVisible by rememberSaveable { mutableStateOf(false) }
    var errorMessage by rememberSaveable { mutableStateOf<String?>(null) }
    var isLoading by rememberSaveable { mutableStateOf(false) }

    val context = LocalContext.current
    val uriHandler = LocalUriHandler.current
    val authRepository = remember { AuthRepository() }
    val sessionManager = remember { SessionManager(context.applicationContext) }
    val coroutineScope = rememberCoroutineScope()
    val emailFocusRequester = remember { FocusRequester() }
    val passwordFocusRequester = remember { FocusRequester() }

    fun submitLogin() {
        if (isLoading) return

        when {
            email.isBlank() -> {
                errorMessage = "Enter your email or username."
                emailFocusRequester.requestFocus()
            }
            password.isBlank() -> {
                errorMessage = "Enter your password."
                passwordFocusRequester.requestFocus()
            }
            else -> {
                isLoading = true
                errorMessage = null
                coroutineScope.launch {
                    authRepository.login(
                        email = email,
                        password = password
                    )
                        .onSuccess { response ->
                            isLoading = false
                            if (response.user.role.equals("Player", ignoreCase = true)) {
                                sessionManager.saveLogin(response)
                                onLoginSuccessful()
                            } else {
                                errorMessage = "This mobile app is for player accounts only."
                            }
                        }
                        .onFailure { exception ->
                            isLoading = false
                            errorMessage = exception.message ?: "Login failed."
                        }
                }
            }
        }
    }

    val fieldColors = OutlinedTextFieldDefaults.colors(
        focusedTextColor = AppColors.DarkText,
        unfocusedTextColor = AppColors.DarkText,
        cursorColor = AppColors.Green,
        focusedBorderColor = AppColors.Green,
        unfocusedBorderColor = AppColors.Border,
        focusedLabelColor = AppColors.Green,
        unfocusedLabelColor = AppColors.GreyText,
        focusedLeadingIconColor = AppColors.Green,
        unfocusedLeadingIconColor = Color(0xFF9CA3AF),
        focusedTrailingIconColor = AppColors.Green,
        unfocusedTrailingIconColor = Color(0xFF9CA3AF),
        focusedContainerColor = Color.White,
        unfocusedContainerColor = Color.White
    )

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.White)
            .safeDrawingPadding()
            .verticalScroll(rememberScrollState())
            .imePadding()
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .background(AppColors.Green)
                .padding(top = 40.dp, bottom = 30.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Image(
                painter = painterResource(R.drawable.paravolley_mpumalanga_logo),
                contentDescription = "ParaVolley Mpumalanga logo",
                modifier = Modifier
                    .size(96.dp)
                    .clip(RoundedCornerShape(14.dp))
            )
            Spacer(Modifier.height(14.dp))
            Text(
                text = "ParaVolley Mpumalanga",
                color = Color.White,
                fontWeight = FontWeight.SemiBold,
                fontSize = 24.sp
            )
            Text(
                modifier = Modifier.padding(top = 4.dp),
                text = "Player Portal",
                color = Color.White.copy(alpha = 0.78f),
                fontSize = 14.sp
            )
        }

        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 24.dp, vertical = 30.dp),
            verticalArrangement = Arrangement.spacedBy(18.dp)
        ) {
            Column(verticalArrangement = Arrangement.spacedBy(5.dp)) {
                Text(
                    text = "Welcome back",
                    color = AppColors.DarkText,
                    fontSize = 24.sp,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = "Sign in to access your events, attendance and player profile.",
                    color = AppColors.GreyText,
                    fontSize = 14.sp
                )
            }

            OutlinedTextField(
                modifier = Modifier
                    .fillMaxWidth()
                    .focusRequester(emailFocusRequester),
                value = email,
                onValueChange = {
                    email = it
                    errorMessage = null
                },
                enabled = !isLoading,
                label = { Text("Email or Username") },
                placeholder = { Text("Enter your email") },
                leadingIcon = {
                    Icon(Icons.Filled.Email, contentDescription = null)
                },
                singleLine = true,
                keyboardOptions = KeyboardOptions(
                    imeAction = if (password.isBlank()) ImeAction.Next else ImeAction.Done
                ),
                keyboardActions = KeyboardActions(
                    onNext = {
                        passwordFocusRequester.requestFocus()
                    },
                    onDone = {
                        if (password.isBlank()) {
                            passwordFocusRequester.requestFocus()
                        } else {
                            submitLogin()
                        }
                    }
                ),
                shape = RoundedCornerShape(10.dp),
                colors = fieldColors
            )

            OutlinedTextField(
                modifier = Modifier
                    .fillMaxWidth()
                    .focusRequester(passwordFocusRequester),
                value = password,
                onValueChange = {
                    password = it
                    errorMessage = null
                },
                enabled = !isLoading,
                label = { Text("Password") },
                placeholder = { Text("Enter your password") },
                leadingIcon = {
                    Icon(Icons.Filled.Lock, contentDescription = null)
                },
                trailingIcon = {
                    IconButton(
                        enabled = !isLoading,
                        onClick = { passwordVisible = !passwordVisible }
                    ) {
                        Icon(
                            imageVector = if (passwordVisible) Icons.Filled.VisibilityOff else Icons.Filled.Visibility,
                            contentDescription = if (passwordVisible) "Hide password" else "Show password"
                        )
                    }
                },
                visualTransformation = if (passwordVisible) VisualTransformation.None else PasswordVisualTransformation(),
                singleLine = true,
                keyboardOptions = KeyboardOptions(
                    imeAction = ImeAction.Done
                ),
                keyboardActions = KeyboardActions(
                    onDone = {
                        if (email.isBlank()) {
                            emailFocusRequester.requestFocus()
                        } else {
                            submitLogin()
                        }
                    }
                ),
                shape = RoundedCornerShape(10.dp),
                colors = fieldColors
            )

            TextButton(
                modifier = Modifier.align(Alignment.End),
                enabled = !isLoading,
                onClick = {
                    val baseUrl = BuildConfig.API_BASE_URL.trimEnd('/')
                    uriHandler.openUri("$baseUrl/Account/ForgotPassword")
                }
            ) {
                Text("Forgot Password?", color = AppColors.Green)
            }

            errorMessage?.let { message ->
                Surface(
                    modifier = Modifier.fillMaxWidth(),
                    color = AppColors.Error.copy(alpha = 0.08f),
                    shape = RoundedCornerShape(10.dp)
                ) {
                    Text(
                        modifier = Modifier.padding(12.dp),
                        text = message,
                        color = AppColors.Error,
                        fontSize = 13.sp
                    )
                }
            }

            Button(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(52.dp),
                enabled = !isLoading,
                onClick = { submitLogin() },
                colors = ButtonDefaults.buttonColors(
                    containerColor = AppColors.Yellow,
                    contentColor = AppColors.DarkText
                ),
                shape = RoundedCornerShape(10.dp)
            ) {
                if (isLoading) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(22.dp),
                        color = AppColors.DarkText,
                        strokeWidth = 2.dp
                    )
                } else {
                    Text("Login", fontWeight = FontWeight.Bold, fontSize = 16.sp)
                }
            }

            Spacer(Modifier.height(12.dp))

            Text(
                modifier = Modifier.fillMaxWidth(),
                text = "Don't have an account?",
                color = AppColors.GreyText,
                textAlign = TextAlign.Center,
                fontSize = 14.sp
            )
            TextButton(
                modifier = Modifier.fillMaxWidth(),
                enabled = !isLoading,
                onClick = onRegister
            ) {
                Text(
                    text = "Register as a Player",
                    color = AppColors.Green,
                    fontWeight = FontWeight.SemiBold
                )
            }
        }
    }
}
