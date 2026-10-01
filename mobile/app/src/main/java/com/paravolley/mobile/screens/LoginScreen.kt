package com.paravolley.mobile.screens

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Email
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Visibility
import androidx.compose.material.icons.filled.VisibilityOff
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LocalTextStyle
import androidx.compose.material3.OutlinedButton
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
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.graphics.Color
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
                errorMessage = "Enter your phone number or email."
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
                                errorMessage =
                                    "This mobile app is for player accounts only."
                            }
                        }
                        .onFailure { exception ->
                            isLoading = false
                            errorMessage =
                                exception.message ?: "Login failed."
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
        unfocusedBorderColor = Color.Transparent,
        focusedLabelColor = AppColors.Green,
        unfocusedLabelColor = AppColors.GreyText,
        focusedLeadingIconColor = AppColors.Green,
        unfocusedLeadingIconColor = AppColors.Green,
        focusedTrailingIconColor = AppColors.Green,
        unfocusedTrailingIconColor = Color(0xFF7C8793),
        focusedContainerColor = Color(0xFFF4F8F6),
        unfocusedContainerColor = Color(0xFFF4F8F6)
    )

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFFF3F6F4))
            .safeDrawingPadding()
            .verticalScroll(rememberScrollState())
            .imePadding()
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .background(AppColors.Green)
                .padding(
                    start = 24.dp,
                    end = 24.dp,
                    top = 28.dp,
                    bottom = 46.dp
                ),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Image(
                painter = painterResource(
                    R.drawable.paravolley_mpumalanga_logo
                ),
                contentDescription = "ParaVolley Mpumalanga logo",
                modifier = Modifier
                    .size(82.dp)
                    .clip(RoundedCornerShape(18.dp))
            )

            Spacer(Modifier.height(14.dp))

            Text(
                text = "ParaVolley Mpumalanga",
                color = Color.White,
                fontWeight = FontWeight.Bold,
                fontSize = 26.sp,
                lineHeight = 31.sp,
                textAlign = TextAlign.Center
            )

            Spacer(Modifier.height(10.dp))

            Surface(
                color = AppColors.Yellow,
                shape = RoundedCornerShape(50)
            ) {
                Text(
                    modifier = Modifier.padding(
                        horizontal = 16.dp,
                        vertical = 7.dp
                    ),
                    text = "PLAYER PORTAL",
                    color = AppColors.DarkText,
                    fontSize = 12.sp,
                    fontWeight = FontWeight.Bold,
                    letterSpacing = 1.sp
                )
            }
        }

        Surface(
            modifier = Modifier
                .fillMaxWidth()
                .offset(y = (-22).dp),
            color = Color.White,
            shape = RoundedCornerShape(
                topStart = 28.dp,
                topEnd = 28.dp
            ),
            shadowElevation = 6.dp
        ) {
            Column(
                modifier = Modifier.padding(
                    start = 22.dp,
                    end = 22.dp,
                    top = 30.dp,
                    bottom = 34.dp
                ),
                verticalArrangement = Arrangement.spacedBy(18.dp)
            ) {
                Column(
                    verticalArrangement = Arrangement.spacedBy(7.dp)
                ) {
                    Text(
                        text = "Welcome back",
                        color = AppColors.DarkText,
                        fontSize = 30.sp,
                        lineHeight = 34.sp,
                        fontWeight = FontWeight.Bold
                    )

                    Text(
                        text = "Sign in to continue to your player dashboard.",
                        color = AppColors.GreyText,
                        fontSize = 15.sp,
                        lineHeight = 22.sp
                    )
                }

                Column(
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Text(
                        text = "Phone number or email",
                        color = AppColors.DarkText,
                        fontSize = 14.sp,
                        fontWeight = FontWeight.SemiBold
                    )

                    OutlinedTextField(
                        modifier = Modifier
                            .fillMaxWidth()
                            .heightIn(min = 62.dp)
                            .focusRequester(emailFocusRequester),
                        value = email,
                        onValueChange = {
                            email = it
                            errorMessage = null
                        },
                        enabled = !isLoading,
                        textStyle = LocalTextStyle.current.copy(
                            fontSize = 16.sp
                        ),
                        placeholder = {
                            Text(
                                text = "079 123 4567 or email@example.com",
                                fontSize = 15.sp,
                                color = Color(0xFF8B949E)
                            )
                        },
                        leadingIcon = {
                            Surface(
                                color = AppColors.Green.copy(alpha = 0.10f),
                                shape = CircleShape
                            ) {
                                Icon(
                                    imageVector = Icons.Filled.Email,
                                    contentDescription = null,
                                    tint = AppColors.Green,
                                    modifier = Modifier
                                        .padding(9.dp)
                                        .size(20.dp)
                                )
                            }
                        },
                        singleLine = true,
                        keyboardOptions = KeyboardOptions(
                            imeAction =
                                if (password.isBlank()) {
                                    ImeAction.Next
                                } else {
                                    ImeAction.Done
                                }
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
                        shape = RoundedCornerShape(16.dp),
                        colors = fieldColors
                    )
                }

                Column(
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Text(
                        text = "Password",
                        color = AppColors.DarkText,
                        fontSize = 14.sp,
                        fontWeight = FontWeight.SemiBold
                    )

                    OutlinedTextField(
                        modifier = Modifier
                            .fillMaxWidth()
                            .heightIn(min = 62.dp)
                            .focusRequester(passwordFocusRequester),
                        value = password,
                        onValueChange = {
                            password = it
                            errorMessage = null
                        },
                        enabled = !isLoading,
                        textStyle = LocalTextStyle.current.copy(
                            fontSize = 16.sp
                        ),
                        placeholder = {
                            Text(
                                text = "Enter your password",
                                fontSize = 15.sp,
                                color = Color(0xFF8B949E)
                            )
                        },
                        leadingIcon = {
                            Surface(
                                color = AppColors.Green.copy(alpha = 0.10f),
                                shape = CircleShape
                            ) {
                                Icon(
                                    imageVector = Icons.Filled.Lock,
                                    contentDescription = null,
                                    tint = AppColors.Green,
                                    modifier = Modifier
                                        .padding(9.dp)
                                        .size(20.dp)
                                )
                            }
                        },
                        trailingIcon = {
                            IconButton(
                                enabled = !isLoading,
                                onClick = {
                                    passwordVisible = !passwordVisible
                                }
                            ) {
                                Icon(
                                    imageVector =
                                        if (passwordVisible) {
                                            Icons.Filled.VisibilityOff
                                        } else {
                                            Icons.Filled.Visibility
                                        },
                                    contentDescription =
                                        if (passwordVisible) {
                                            "Hide password"
                                        } else {
                                            "Show password"
                                        }
                                )
                            }
                        },
                        visualTransformation =
                            if (passwordVisible) {
                                VisualTransformation.None
                            } else {
                                PasswordVisualTransformation()
                            },
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
                        shape = RoundedCornerShape(16.dp),
                        colors = fieldColors
                    )
                }

                TextButton(
                    modifier = Modifier.align(Alignment.End),
                    enabled = !isLoading,
                    onClick = {
                        val webBaseUrl =
                            BuildConfig.PUBLIC_WEB_BASE_URL.trimEnd('/')

                        uriHandler.openUri(
                            "$webBaseUrl/Account/ForgotPassword"
                        )
                    }
                ) {
                    Text(
                        text = "Forgot password?",
                        color = AppColors.Green,
                        fontSize = 14.sp,
                        fontWeight = FontWeight.Bold
                    )
                }

                errorMessage?.let { message ->
                    Surface(
                        modifier = Modifier.fillMaxWidth(),
                        color = AppColors.Error.copy(alpha = 0.08f),
                        shape = RoundedCornerShape(14.dp)
                    ) {
                        Text(
                            modifier = Modifier.padding(14.dp),
                            text = message,
                            color = AppColors.Error,
                            fontSize = 13.sp,
                            lineHeight = 18.sp
                        )
                    }
                }

                Button(
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(58.dp),
                    enabled = !isLoading,
                    onClick = {
                        submitLogin()
                    },
                    colors = ButtonDefaults.buttonColors(
                        containerColor = AppColors.Yellow,
                        contentColor = AppColors.DarkText
                    ),
                    shape = RoundedCornerShape(16.dp)
                ) {
                    if (isLoading) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(22.dp),
                            color = AppColors.DarkText,
                            strokeWidth = 2.dp
                        )
                    } else {
                        Text(
                            text = "Sign in",
                            fontWeight = FontWeight.Bold,
                            fontSize = 17.sp
                        )
                    }
                }

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    HorizontalDivider(
                        modifier = Modifier.weight(1f),
                        color = Color(0xFFE1E7E4)
                    )
                    Text(
                        text = "New to ParaVolley?",
                        color = AppColors.GreyText,
                        fontSize = 13.sp
                    )
                    HorizontalDivider(
                        modifier = Modifier.weight(1f),
                        color = Color(0xFFE1E7E4)
                    )
                }

                OutlinedButton(
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(56.dp),
                    enabled = !isLoading,
                    onClick = onRegister,
                    border = BorderStroke(
                        width = 1.5.dp,
                        color = AppColors.Green
                    ),
                    colors = ButtonDefaults.outlinedButtonColors(
                        contentColor = AppColors.Green
                    ),
                    shape = RoundedCornerShape(16.dp)
                ) {
                    Text(
                        text = "Register as a Player",
                        fontSize = 16.sp,
                        fontWeight = FontWeight.Bold
                    )
                }

                Text(
                    modifier = Modifier.fillMaxWidth(),
                    text = "Secure player access • ParaVolley Mpumalanga",
                    color = Color(0xFF9AA39F),
                    textAlign = TextAlign.Center,
                    fontSize = 12.sp
                )
            }
        }
    }
}
