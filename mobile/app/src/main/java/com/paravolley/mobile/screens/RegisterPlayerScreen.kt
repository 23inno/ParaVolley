package com.paravolley.mobile.screens

import android.util.Patterns
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.ContactPhone
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.SportsVolleyball
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
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
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.network.AuthRepository
import com.paravolley.mobile.network.RegisterPlayerRequest
import com.paravolley.mobile.ui.theme.AppColors
import kotlinx.coroutines.launch

@Composable
fun RegisterPlayerScreen(
    onBackToLogin: () -> Unit
) {
    var name by rememberSaveable { mutableStateOf("") }
    var position by rememberSaveable { mutableStateOf("") }
    var team by rememberSaveable { mutableStateOf("") }
    var age by rememberSaveable { mutableStateOf("") }
    var email by rememberSaveable { mutableStateOf("") }
    var phone by rememberSaveable { mutableStateOf("") }
    var disability by rememberSaveable { mutableStateOf("") }
    var password by rememberSaveable { mutableStateOf("") }
    var confirmPassword by rememberSaveable { mutableStateOf("") }
    var message by rememberSaveable { mutableStateOf<String?>(null) }
    var registrationComplete by rememberSaveable { mutableStateOf(false) }
    var isLoading by rememberSaveable { mutableStateOf(false) }

    val repository = remember { AuthRepository() }
    val scope = rememberCoroutineScope()

    val fieldColors = OutlinedTextFieldDefaults.colors(
        focusedTextColor = AppColors.DarkText,
        unfocusedTextColor = AppColors.DarkText,
        cursorColor = AppColors.Green,
        focusedBorderColor = AppColors.Green,
        unfocusedBorderColor = AppColors.Border,
        focusedContainerColor = Color.White,
        unfocusedContainerColor = Color.White,
        focusedPlaceholderColor = Color(0xFF9CA3AF),
        unfocusedPlaceholderColor = Color(0xFF9CA3AF)
    )

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(AppColors.LightBackground)
            .safeDrawingPadding()
            .verticalScroll(rememberScrollState())
            .imePadding()
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(AppColors.Green)
                .padding(horizontal = 6.dp, vertical = 10.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(
                enabled = !isLoading,
                onClick = onBackToLogin
            ) {
                Icon(
                    imageVector = Icons.Filled.ArrowBack,
                    contentDescription = "Back",
                    tint = Color.White
                )
            }

            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "Player Registration",
                    color = Color.White,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 20.sp
                )
                Text(
                    text = "Create your ParaVolley player account",
                    color = Color.White.copy(alpha = 0.78f),
                    fontSize = 12.sp
                )
            }

            Spacer(Modifier.width(44.dp))
        }

        Column(
            modifier = Modifier.padding(horizontal = 16.dp, vertical = 18.dp),
            verticalArrangement = Arrangement.spacedBy(14.dp)
        ) {
            Surface(
                color = AppColors.LightGreen,
                shape = RoundedCornerShape(12.dp)
            ) {
                Text(
                    modifier = Modifier.padding(14.dp),
                    text = "Your registration will be submitted for administrator approval before mobile access is activated.",
                    color = AppColors.Green,
                    fontSize = 13.sp,
                    lineHeight = 19.sp
                )
            }

            RegistrationSection(
                title = "Player Details",
                icon = Icons.Filled.SportsVolleyball
            ) {
                RegistrationField("Full name", "Enter your full name", name, fieldColors, isLoading) { name = it }
                RegistrationField("Position", "e.g. Setter", position, fieldColors, isLoading) { position = it }
                RegistrationField("Team", "Enter your team", team, fieldColors, isLoading) { team = it }
                RegistrationField(
                    label = "Age",
                    placeholder = "Enter your age",
                    value = age,
                    colors = fieldColors,
                    isLoading = isLoading,
                    keyboardType = KeyboardType.Number
                ) {
                    age = it.filter(Char::isDigit)
                }
                RegistrationField(
                    label = "Disability / classification",
                    placeholder = "Optional",
                    value = disability,
                    colors = fieldColors,
                    isLoading = isLoading
                ) { disability = it }
            }

            RegistrationSection(
                title = "Contact Details",
                icon = Icons.Filled.ContactPhone
            ) {
                RegistrationField(
                    label = "Email",
                    placeholder = "name@example.com",
                    value = email,
                    colors = fieldColors,
                    isLoading = isLoading,
                    keyboardType = KeyboardType.Email
                ) { email = it }
                RegistrationField(
                    label = "Phone",
                    placeholder = "+27 ...",
                    value = phone,
                    colors = fieldColors,
                    isLoading = isLoading,
                    keyboardType = KeyboardType.Phone
                ) { phone = it }
            }

            RegistrationSection(
                title = "Account Security",
                icon = Icons.Filled.Lock
            ) {
                RegistrationField(
                    label = "Password",
                    placeholder = "Minimum 8 characters",
                    value = password,
                    colors = fieldColors,
                    isLoading = isLoading,
                    keyboardType = KeyboardType.Password,
                    isPassword = true
                ) { password = it }
                RegistrationField(
                    label = "Confirm password",
                    placeholder = "Re-enter your password",
                    value = confirmPassword,
                    colors = fieldColors,
                    isLoading = isLoading,
                    keyboardType = KeyboardType.Password,
                    isPassword = true
                ) { confirmPassword = it }
            }

            message?.let {
                Surface(
                    modifier = Modifier.fillMaxWidth(),
                    color = if (registrationComplete) {
                        AppColors.LightGreen
                    } else {
                        AppColors.Error.copy(alpha = 0.08f)
                    },
                    shape = RoundedCornerShape(10.dp)
                ) {
                    Text(
                        modifier = Modifier.padding(13.dp),
                        text = it,
                        color = if (registrationComplete) AppColors.Green else AppColors.Error,
                        fontSize = 13.sp,
                        lineHeight = 19.sp
                    )
                }
            }

            Button(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(50.dp),
                enabled = !isLoading && !registrationComplete,
                colors = ButtonDefaults.buttonColors(
                    containerColor = AppColors.Yellow,
                    contentColor = AppColors.DarkText
                ),
                shape = RoundedCornerShape(10.dp),
                onClick = {
                    val parsedAge = age.toIntOrNull()
                    message = validateRegistration(
                        name,
                        position,
                        team,
                        parsedAge,
                        email,
                        phone,
                        password,
                        confirmPassword
                    )

                    if (message != null) return@Button

                    isLoading = true
                    scope.launch {
                        repository.registerPlayer(
                            RegisterPlayerRequest(
                                name = name.trim(),
                                position = position.trim(),
                                team = team.trim(),
                                age = parsedAge!!,
                                email = email.trim(),
                                phone = phone.trim(),
                                disability = disability.trim(),
                                password = password
                            )
                        ).onSuccess {
                            isLoading = false
                            registrationComplete = true
                            message = it.message
                        }.onFailure {
                            isLoading = false
                            message = it.message ?: "Registration failed."
                        }
                    }
                }
            ) {
                if (isLoading) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(20.dp),
                        color = AppColors.DarkText,
                        strokeWidth = 2.dp
                    )
                } else {
                    Text(
                        text = "Submit Registration",
                        fontWeight = FontWeight.SemiBold
                    )
                }
            }

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.Center,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = if (registrationComplete) {
                        "Registration submitted."
                    } else {
                        "Already registered?"
                    },
                    color = AppColors.GreyText,
                    fontSize = 13.sp
                )
                TextButton(
                    enabled = !isLoading,
                    onClick = onBackToLogin
                ) {
                    Text(
                        text = "Back to Login",
                        color = AppColors.Green,
                        fontWeight = FontWeight.SemiBold,
                        fontSize = 13.sp
                    )
                }
            }
        }
    }
}

@Composable
private fun RegistrationSection(
    title: String,
    icon: ImageVector,
    content: @Composable () -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.cardColors(containerColor = Color.White),
        border = BorderStroke(1.dp, Color(0xFFF3F4F6)),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Box(
                    modifier = Modifier
                        .size(38.dp)
                        .background(AppColors.LightGreen, RoundedCornerShape(9.dp)),
                    contentAlignment = Alignment.Center
                ) {
                    Icon(
                        imageVector = icon,
                        contentDescription = null,
                        tint = AppColors.Green,
                        modifier = Modifier.size(20.dp)
                    )
                }
                Spacer(Modifier.width(10.dp))
                Text(
                    text = title,
                    color = AppColors.Green,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 16.sp
                )
            }

            content()
        }
    }
}

@Composable
private fun RegistrationField(
    label: String,
    placeholder: String,
    value: String,
    colors: androidx.compose.material3.TextFieldColors,
    isLoading: Boolean,
    keyboardType: KeyboardType = KeyboardType.Text,
    isPassword: Boolean = false,
    onValueChange: (String) -> Unit
) {
    Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
        Text(
            text = label,
            color = Color(0xFF374151),
            fontSize = 12.sp,
            fontWeight = FontWeight.Medium
        )
        OutlinedTextField(
            modifier = Modifier.fillMaxWidth(),
            value = value,
            onValueChange = onValueChange,
            placeholder = { Text(placeholder) },
            singleLine = true,
            enabled = !isLoading,
            colors = colors,
            shape = RoundedCornerShape(10.dp),
            keyboardOptions = KeyboardOptions(keyboardType = keyboardType),
            visualTransformation = if (isPassword) {
                PasswordVisualTransformation()
            } else {
                VisualTransformation.None
            }
        )
    }
}

private fun validateRegistration(
    name: String,
    position: String,
    team: String,
    age: Int?,
    email: String,
    phone: String,
    password: String,
    confirmPassword: String
): String? = when {
    name.isBlank() || position.isBlank() || team.isBlank() ->
        "Enter your name, position, and team."
    age == null || age !in 5..100 ->
        "Enter an age between 5 and 100."
    !Patterns.EMAIL_ADDRESS.matcher(email.trim()).matches() ->
        "Enter a valid email address."
    phone.isBlank() -> "Enter a phone number."
    password.length < 8 -> "Password must contain at least 8 characters."
    password != confirmPassword -> "The passwords do not match."
    else -> null
}
