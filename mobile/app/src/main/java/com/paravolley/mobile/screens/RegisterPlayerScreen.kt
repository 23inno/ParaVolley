package com.paravolley.mobile.screens

import android.app.DatePickerDialog
import android.util.Patterns
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
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
import androidx.compose.material.icons.filled.DateRange
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CheckboxDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
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
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.network.AuthRepository
import com.paravolley.mobile.network.RegisterPlayerRequest
import com.paravolley.mobile.ui.theme.AppColors
import java.time.LocalDate
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale
import kotlinx.coroutines.launch

@Composable
fun RegisterPlayerScreen(
    onBackToLogin: () -> Unit
) {
    var fullName by rememberSaveable { mutableStateOf("") }
    var email by rememberSaveable { mutableStateOf("") }
    var phone by rememberSaveable { mutableStateOf("") }
    var dateOfBirth by rememberSaveable { mutableStateOf("") }
    var province by rememberSaveable { mutableStateOf("Mpumalanga") }
    var town by rememberSaveable { mutableStateOf("") }
    var experienceLevel by rememberSaveable { mutableStateOf("") }
    var preferredPosition by rememberSaveable { mutableStateOf("") }
    var classification by rememberSaveable { mutableStateOf("") }
    var emergencyContactName by rememberSaveable { mutableStateOf("") }
    var emergencyContactPhone by rememberSaveable { mutableStateOf("") }
    var medicalNotes by rememberSaveable { mutableStateOf("") }
    var consent by rememberSaveable { mutableStateOf(false) }

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
            IconButton(
                onClick = onBackToLogin,
                enabled = !isLoading
            ) {
                Icon(
                    Icons.Filled.ArrowBack,
                    contentDescription = "Back",
                    tint = Color.White
                )
            }

            Spacer(Modifier.width(4.dp))

            Column {
                Text(
                    text = "Player Application",
                    color = Color.White,
                    fontWeight = FontWeight.Bold,
                    fontSize = 21.sp
                )
                Text(
                    text = "Apply to join ParaVolley Mpumalanga",
                    color = Color.White.copy(alpha = 0.78f),
                    fontSize = 12.sp
                )
            }
        }

        Column(
            modifier = Modifier.padding(
                horizontal = 20.dp,
                vertical = 22.dp
            ),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text(
                text = "Personal Information",
                color = AppColors.DarkText,
                fontWeight = FontWeight.Bold,
                fontSize = 18.sp
            )

            Text(
                text = "This form matches the player application used on the ParaVolley website. Your application will be sent to Player Applications for review.",
                color = AppColors.GreyText,
                fontSize = 13.sp
            )

            RegistrationField(
                label = "Full Name *",
                value = fullName,
                colors = fieldColors,
                isLoading = isLoading
            ) { fullName = it }

            RegistrationField(
                label = "Email Address *",
                value = email,
                colors = fieldColors,
                isLoading = isLoading,
                keyboardType = KeyboardType.Email
            ) { email = it }

            RegistrationField(
                label = "Phone Number *",
                value = phone,
                colors = fieldColors,
                isLoading = isLoading,
                keyboardType = KeyboardType.Phone
            ) { phone = it }

            DateOfBirthField(
                value = dateOfBirth,
                isLoading = isLoading,
                onDateSelected = {
                    dateOfBirth = it
                    message = null
                }
            )

            RegistrationField(
                label = "Province",
                value = province,
                colors = fieldColors,
                isLoading = isLoading
            ) { province = it.take(100) }

            RegistrationField(
                label = "Town / City",
                value = town,
                colors = fieldColors,
                isLoading = isLoading
            ) { town = it.take(100) }

            Spacer(Modifier.height(6.dp))

            Text(
                text = "Player Information",
                color = AppColors.DarkText,
                fontWeight = FontWeight.Bold,
                fontSize = 18.sp
            )

            Text(
                text = "Experience examples: Beginner, Intermediate, Experienced, Competitive Athlete.",
                color = AppColors.GreyText,
                fontSize = 12.sp
            )

            RegistrationField(
                label = "Experience Level",
                value = experienceLevel,
                colors = fieldColors,
                isLoading = isLoading
            ) { experienceLevel = it.take(50) }

            Text(
                text = "Position examples: Setter, Outside Hitter, Middle Blocker, Opposite Hitter, Libero, Not Sure Yet.",
                color = AppColors.GreyText,
                fontSize = 12.sp
            )

            RegistrationField(
                label = "Preferred Position",
                value = preferredPosition,
                colors = fieldColors,
                isLoading = isLoading
            ) { preferredPosition = it.take(50) }

            RegistrationField(
                label = "Disability Classification *",
                value = classification,
                colors = fieldColors,
                isLoading = isLoading
            ) { classification = it.take(500) }

            Spacer(Modifier.height(6.dp))

            Text(
                text = "Emergency Contact",
                color = AppColors.DarkText,
                fontWeight = FontWeight.Bold,
                fontSize = 18.sp
            )

            RegistrationField(
                label = "Contact Name",
                value = emergencyContactName,
                colors = fieldColors,
                isLoading = isLoading
            ) { emergencyContactName = it.take(120) }

            RegistrationField(
                label = "Contact Number",
                value = emergencyContactPhone,
                colors = fieldColors,
                isLoading = isLoading,
                keyboardType = KeyboardType.Phone
            ) { emergencyContactPhone = it.take(30) }

            RegistrationField(
                label = "Additional Support Information",
                value = medicalNotes,
                colors = fieldColors,
                isLoading = isLoading,
                singleLine = false,
                minLines = 3
            ) { medicalNotes = it.take(1000) }

            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.Top
            ) {
                Checkbox(
                    checked = consent,
                    onCheckedChange = {
                        consent = it
                        message = null
                    },
                    enabled = !isLoading,
                    colors = CheckboxDefaults.colors(
                        checkedColor = AppColors.Green
                    )
                )

                Text(
                    modifier = Modifier
                        .weight(1f)
                        .padding(top = 12.dp),
                    text = "I confirm that the information I provided is correct and I agree that ParaVolley Mpumalanga may contact me regarding my application.",
                    color = AppColors.DarkText,
                    fontSize = 13.sp
                )
            }

            message?.let {
                Surface(
                    modifier = Modifier.fillMaxWidth(),
                    color =
                        if (registrationComplete) {
                            AppColors.LightGreen
                        } else {
                            AppColors.Error.copy(alpha = 0.08f)
                        },
                    shape = RoundedCornerShape(10.dp)
                ) {
                    Text(
                        modifier = Modifier.padding(12.dp),
                        text = it,
                        color =
                            if (registrationComplete) {
                                AppColors.Green
                            } else {
                                AppColors.Error
                            },
                        fontSize = 13.sp
                    )
                }
            }

            Button(
                modifier = Modifier.fillMaxWidth(),
                enabled = !isLoading && !registrationComplete,
                colors = ButtonDefaults.buttonColors(
                    containerColor = AppColors.Yellow,
                    contentColor = AppColors.DarkText
                ),
                shape = RoundedCornerShape(10.dp),
                onClick = {
                    val validationMessage = validateApplication(
                        fullName = fullName,
                        email = email,
                        phone = phone,
                        dateOfBirth = dateOfBirth,
                        classification = classification,
                        consent = consent
                    )

                    if (validationMessage != null) {
                        message = validationMessage
                        return@Button
                    }

                    isLoading = true
                    message = null

                    scope.launch {
                        repository.registerPlayer(
                            RegisterPlayerRequest(
                                fullName = fullName.trim(),
                                email = email.trim(),
                                phone = phone.trim(),
                                dateOfBirth = dateOfBirth.trim(),
                                province = province
                                    .trim()
                                    .ifBlank { null },
                                town = town
                                    .trim()
                                    .ifBlank { null },
                                experienceLevel = experienceLevel
                                    .trim()
                                    .ifBlank { null },
                                preferredPosition = preferredPosition
                                    .trim()
                                    .ifBlank { null },
                                classification = classification.trim(),
                                emergencyContactName = emergencyContactName
                                    .trim()
                                    .ifBlank { null },
                                emergencyContactPhone = emergencyContactPhone
                                    .trim()
                                    .ifBlank { null },
                                medicalNotes = medicalNotes
                                    .trim()
                                    .ifBlank { null },
                                consent = true
                            )
                        )
                            .onSuccess {
                                registrationComplete = true
                                message = it.message
                            }
                            .onFailure {
                                message =
                                    it.message
                                        ?: "The player application could not be submitted."
                            }

                        isLoading = false
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
                        "Submit Player Application",
                        fontWeight = FontWeight.Bold
                    )
                }
            }

            TextButton(
                modifier = Modifier.fillMaxWidth(),
                enabled = !isLoading,
                onClick = onBackToLogin
            ) {
                Text(
                    text =
                        if (registrationComplete) {
                            "Return to Login"
                        } else {
                            "Already registered? Back to Login"
                        },
                    color = AppColors.Green,
                    fontWeight = FontWeight.SemiBold
                )
            }
        }
    }
}

@Composable
private fun DateOfBirthField(
    value: String,
    isLoading: Boolean,
    onDateSelected: (String) -> Unit
) {
    val context = LocalContext.current
    val today = remember { LocalDate.now() }
    val earliestAllowedDate = remember(today) {
        today.minusYears(100)
    }
    val latestAllowedDate = remember(today) {
        today.minusYears(5)
    }

    val selectedDate = remember(value) {
        runCatching {
            LocalDate.parse(
                value,
                DateTimeFormatter.ISO_LOCAL_DATE
            )
        }.getOrNull()
    }

    val displayDate = selectedDate?.format(
        DateTimeFormatter.ofPattern(
            "dd MMM yyyy",
            Locale.getDefault()
        )
    ) ?: "Select date of birth"

    OutlinedButton(
        modifier = Modifier.fillMaxWidth(),
        enabled = !isLoading,
        shape = RoundedCornerShape(10.dp),
        colors = ButtonDefaults.outlinedButtonColors(
            contentColor = AppColors.DarkText
        ),
        onClick = {
            val initialDate =
                selectedDate ?: today.minusYears(18)

            DatePickerDialog(
                context,
                { _, year, month, dayOfMonth ->
                    val chosenDate = LocalDate.of(
                        year,
                        month + 1,
                        dayOfMonth
                    )

                    onDateSelected(
                        chosenDate.format(
                            DateTimeFormatter.ISO_LOCAL_DATE
                        )
                    )
                },
                initialDate.year,
                initialDate.monthValue - 1,
                initialDate.dayOfMonth
            ).apply {
                datePicker.minDate =
                    earliestAllowedDate
                        .atStartOfDay(ZoneId.systemDefault())
                        .toInstant()
                        .toEpochMilli()

                datePicker.maxDate =
                    latestAllowedDate
                        .atStartOfDay(ZoneId.systemDefault())
                        .toInstant()
                        .toEpochMilli()
            }.show()
        }
    ) {
        Icon(
            imageVector = Icons.Filled.DateRange,
            contentDescription = null,
            tint = AppColors.Green
        )

        Spacer(Modifier.width(10.dp))

        Column(
            modifier = Modifier.weight(1f),
            horizontalAlignment = Alignment.Start
        ) {
            Text(
                text = "Date of Birth *",
                color = AppColors.GreyText,
                fontSize = 11.sp
            )
            Text(
                text = displayDate,
                color =
                    if (selectedDate == null) {
                        AppColors.GreyText
                    } else {
                        AppColors.DarkText
                    },
                fontSize = 15.sp,
                fontWeight =
                    if (selectedDate == null) {
                        FontWeight.Normal
                    } else {
                        FontWeight.Medium
                    }
            )
        }

        Text(
            text = "Choose",
            color = AppColors.Green,
            fontWeight = FontWeight.SemiBold,
            fontSize = 13.sp
        )
    }

    Text(
        modifier = Modifier.padding(start = 4.dp),
        text = "Tap to choose your date. No typing is required.",
        color = AppColors.GreyText,
        fontSize = 11.sp
    )
}

@Composable
private fun RegistrationField(
    label: String,
    value: String,
    colors: androidx.compose.material3.TextFieldColors,
    isLoading: Boolean,
    keyboardType: KeyboardType = KeyboardType.Text,
    singleLine: Boolean = true,
    minLines: Int = 1,
    onValueChange: (String) -> Unit
) {
    OutlinedTextField(
        modifier = Modifier.fillMaxWidth(),
        value = value,
        onValueChange = {
            onValueChange(it)
        },
        label = { Text(label) },
        singleLine = singleLine,
        minLines = minLines,
        enabled = !isLoading,
        colors = colors,
        shape = RoundedCornerShape(10.dp),
        keyboardOptions = KeyboardOptions(
            keyboardType = keyboardType
        )
    )
}

private fun validateApplication(
    fullName: String,
    email: String,
    phone: String,
    dateOfBirth: String,
    classification: String,
    consent: Boolean
): String? {
    if (fullName.isBlank()) {
        return "Enter your full name."
    }

    if (!Patterns.EMAIL_ADDRESS
            .matcher(email.trim())
            .matches()
    ) {
        return "Enter a valid email address."
    }

    if (phone.isBlank()) {
        return "Enter a phone number."
    }

    val parsedDate = runCatching {
        LocalDate.parse(
            dateOfBirth.trim(),
            DateTimeFormatter.ISO_LOCAL_DATE
        )
    }.getOrNull()
        ?: return "Choose your date of birth."

    val today = LocalDate.now()

    if (parsedDate.isAfter(today)) {
        return "Date of birth cannot be in the future."
    }

    var age = today.year - parsedDate.year
    if (parsedDate.isAfter(today.minusYears(age.toLong()))) {
        age--
    }

    if (age !in 5..100) {
        return "Player age must be between 5 and 100 years."
    }

    if (classification.isBlank()) {
        return "Enter your disability classification."
    }

    if (!consent) {
        return "Confirm the consent statement before submitting."
    }

    return null
}
