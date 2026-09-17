package com.paravolley.mobile.screens

import android.util.Patterns
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
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
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
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
        focusedLabelColor = AppColors.Green,
        unfocusedLabelColor = AppColors.GreyText,
        focusedBorderColor = AppColors.Green,
        unfocusedBorderColor = AppColors.Border,
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
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(AppColors.Green)
                .padding(horizontal = 8.dp, vertical = 12.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = onBackToLogin, enabled = !isLoading) {
                Icon(Icons.Filled.ArrowBack, contentDescription = "Back", tint = Color.White)
            }
            Spacer(Modifier.width(4.dp))
            Column {
                Text(
                    text = "Player Registration",
                    color = Color.White,
                    fontWeight = FontWeight.Bold,
                    fontSize = 21.sp
                )
                Text(
                    text = "Create your ParaVolley player account",
                    color = Color.White.copy(alpha = 0.78f),
                    fontSize = 12.sp
                )
            }
        }

        Column(
            modifier = Modifier.padding(horizontal = 20.dp, vertical = 22.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text(
                text = "Personal & player details",
                color = AppColors.DarkText,
                fontWeight = FontWeight.Bold,
                fontSize = 18.sp
            )
            Text(
                text = "Your registration is submitted for administrator approval before mobile access is activated.",
                color = AppColors.GreyText,
                fontSize = 13.sp
            )

            RegistrationField("Full name", name, fieldColors, isLoading) { name = it }
            RegistrationField("Position", position, fieldColors, isLoading) { position = it }
            RegistrationField("Team", team, fieldColors, isLoading) { team = it }
            RegistrationField("Age", age, fieldColors, isLoading, KeyboardType.Number) {
                age = it.filter(Char::isDigit)
            }
            RegistrationField("Email", email, fieldColors, isLoading, KeyboardType.Email) { email = it }
            RegistrationField("Phone", phone, fieldColors, isLoading, KeyboardType.Phone) { phone = it }
            RegistrationField("Disability / classification (optional)", disability, fieldColors, isLoading) { disability = it }
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
                Surface(
                    modifier = Modifier.fillMaxWidth(),
                    color = if (registrationComplete) AppColors.LightGreen else AppColors.Error.copy(alpha = 0.08f),
                    shape = RoundedCornerShape(10.dp)
                ) {
                    Text(
                        modifier = Modifier.padding(12.dp),
                        text = it,
                        color = if (registrationComplete) AppColors.Green else AppColors.Error,
                        fontSize = 13.sp
                    )
                }
            }

            Button(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = 4.dp),
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
                    Text("Submit Registration", fontWeight = FontWeight.Bold)
                }
            }

            TextButton(
                modifier = Modifier.fillMaxWidth(),
                enabled = !isLoading,
                onClick = onBackToLogin
            ) {
                Text(
                    text = if (registrationComplete) "Return to Login" else "Already registered? Back to Login",
                    color = AppColors.Green,
                    fontWeight = FontWeight.SemiBold
                )
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
        shape = RoundedCornerShape(10.dp),
        keyboardOptions = KeyboardOptions(keyboardType = keyboardType),
        visualTransformation = if (isPassword) PasswordVisualTransformation() else VisualTransformation.None
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
