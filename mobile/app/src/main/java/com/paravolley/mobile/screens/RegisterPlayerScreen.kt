package com.paravolley.mobile.screens

import android.util.Patterns
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.rememberScrollState
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
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
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
        cursorColor = AppColors.DarkGreen,
        focusedLabelColor = AppColors.DarkGreen,
        unfocusedLabelColor = AppColors.GreyText,
        focusedBorderColor = AppColors.DarkGreen,
        unfocusedBorderColor = AppColors.GreyText
    )

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(AppColors.LightBackground)
            .safeDrawingPadding()
            .verticalScroll(rememberScrollState())
            .imePadding()
            .padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text(
            text = "Player Registration",
            color = AppColors.DarkGreen,
            fontWeight = FontWeight.Bold,
            fontSize = 26.sp
        )

        Text(
            text = "Submit your details for administrator approval.",
            color = AppColors.GreyText
        )

        Card(
            colors = CardDefaults.cardColors(containerColor = Color.White)
        ) {
            Column(
                modifier = Modifier.padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                RegistrationField("Full name", name, fieldColors, isLoading) { name = it }
                RegistrationField("Position", position, fieldColors, isLoading) { position = it }
                RegistrationField("Team", team, fieldColors, isLoading) { team = it }
                RegistrationField(
                    "Age",
                    age,
                    fieldColors,
                    isLoading,
                    KeyboardType.Number
                ) { age = it.filter(Char::isDigit) }
                RegistrationField(
                    "Email",
                    email,
                    fieldColors,
                    isLoading,
                    KeyboardType.Email
                ) { email = it }
                RegistrationField(
                    "Phone",
                    phone,
                    fieldColors,
                    isLoading,
                    KeyboardType.Phone
                ) { phone = it }
                RegistrationField(
                    "Disability (optional)",
                    disability,
                    fieldColors,
                    isLoading
                ) { disability = it }
                RegistrationField(
                    "Password (minimum 8 characters)",
                    password,
                    fieldColors,
                    isLoading,
                    KeyboardType.Password,
                    true
                ) { password = it }
                RegistrationField(
                    "Confirm password",
                    confirmPassword,
                    fieldColors,
                    isLoading,
                    KeyboardType.Password,
                    true
                ) { confirmPassword = it }

                message?.let {
                    Text(
                        text = it,
                        color = if (registrationComplete) {
                            AppColors.DarkGreen
                        } else {
                            Color.Red
                        }
                    )
                }

                Button(
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !isLoading && !registrationComplete,
                    colors = ButtonDefaults.buttonColors(
                        containerColor = AppColors.Yellow,
                        contentColor = AppColors.DarkText
                    ),
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
                        CircularProgressIndicator()
                    } else {
                        Text("Submit registration", fontWeight = FontWeight.Bold)
                    }
                }

                TextButton(
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !isLoading,
                    onClick = onBackToLogin
                ) {
                    Text(
                        if (registrationComplete) "Return to login" else "Back to login"
                    )
                }
            }
        }
    }
}

@Composable
private fun RegistrationField(
    label: String,
    value: String,
    colors: androidx.compose.material3.TextFieldColors,
    isLoading: Boolean,
    keyboardType: KeyboardType = KeyboardType.Text,
    isPassword: Boolean = false,
    onValueChange: (String) -> Unit
) {
    OutlinedTextField(
        modifier = Modifier.fillMaxWidth(),
        value = value,
        onValueChange = onValueChange,
        label = { Text(label) },
        singleLine = true,
        enabled = !isLoading,
        colors = colors,
        keyboardOptions = KeyboardOptions(keyboardType = keyboardType),
        visualTransformation = if (isPassword) {
            PasswordVisualTransformation()
        } else {
            androidx.compose.ui.text.input.VisualTransformation.None
        }
    )
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
