package com.paravolley.mobile.screens

import android.graphics.BitmapFactory
import android.util.Patterns
import android.widget.Toast
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.CameraAlt
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.Email
import androidx.compose.material.icons.filled.Groups
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.Logout
import androidx.compose.material.icons.filled.Phone
import androidx.compose.material.icons.filled.Save
import androidx.compose.material.icons.filled.SportsVolleyball
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.components.AppBottomBar
import com.paravolley.mobile.navigation.Routes
import com.paravolley.mobile.network.AttendanceRepository
import com.paravolley.mobile.network.AttendanceResponse
import com.paravolley.mobile.network.PlayerProfileResponse
import com.paravolley.mobile.network.PlayerRepository
import com.paravolley.mobile.network.SessionManager
import com.paravolley.mobile.ui.theme.AppColors
import java.time.LocalDate
import java.time.LocalTime
import java.time.format.DateTimeFormatter
import java.util.Locale
import kotlinx.coroutines.launch

private val ProfileGreen = Color(0xFF1A5F3F)
private val ProfileYellow = Color(0xFFFBBF24)
private val ProfileBackground = Color(0xFFF9FAFB)
private val ProfileBorder = Color(0xFFE5E7EB)
private val ProfileMuted = Color(0xFF6B7280)
private val ProfileText = Color(0xFF111827)

@Composable
fun ProfileScreen(
    onNavigate: (String) -> Unit,
    onLogout: () -> Unit
) {
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    val playerRepository = remember { PlayerRepository(context.applicationContext) }
    val attendanceRepository = remember { AttendanceRepository(context.applicationContext) }
    val sessionManager = remember { SessionManager(context.applicationContext) }

    var player by remember { mutableStateOf<PlayerProfileResponse?>(null) }
    var attendance by remember { mutableStateOf<List<AttendanceResponse>>(emptyList()) }
    var profilePhoto by remember { mutableStateOf<ImageBitmap?>(null) }
    var isLoading by remember { mutableStateOf(true) }
    var isSaving by remember { mutableStateOf(false) }
    var isPhotoUploading by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    var actionMessage by remember { mutableStateOf<String?>(null) }

    var isEditing by remember { mutableStateOf(false) }
    var draftAge by remember { mutableStateOf("") }
    var draftEmail by remember { mutableStateOf("") }
    var draftPhone by remember { mutableStateOf("") }
    var draftEmergencyName by remember { mutableStateOf("") }
    var draftEmergencyPhone by remember { mutableStateOf("") }

    fun startEditing() {
        val current = player ?: return
        draftAge = current.age.toString()
        draftEmail = current.email
        draftPhone = current.phone
        draftEmergencyName = current.emergencyContactName
        draftEmergencyPhone = current.emergencyContactPhone
        actionMessage = null
        isEditing = true
    }

    fun cancelEditing() {
        isEditing = false
        actionMessage = null
    }

    val photoPicker = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.GetContent()
    ) { uri ->
        if (uri != null) {
            val preview = runCatching {
                context.contentResolver.openInputStream(uri)
                    ?.use { it.readBytes() }
                    ?.toImageBitmapOrNull()
            }.getOrNull()

            if (preview != null) {
                profilePhoto = preview
            }

            scope.launch {
                isPhotoUploading = true
                actionMessage = null

                playerRepository.uploadProfilePhoto(uri)
                    .onSuccess { updated ->
                        player = updated
                        playerRepository.getProfilePhoto()
                            .onSuccess { bytes ->
                                bytes?.toImageBitmapOrNull()?.let { savedPhoto ->
                                    profilePhoto = savedPhoto
                                }
                            }

                        Toast.makeText(
                            context,
                            "Profile photo updated.",
                            Toast.LENGTH_SHORT
                        ).show()
                    }
                    .onFailure { failure ->
                        actionMessage =
                            "The photo preview is shown, but it could not be saved. " +
                                (failure.message ?: "Please try again.")
                    }

                isPhotoUploading = false
            }
        }
    }

    LaunchedEffect(Unit) {
        isLoading = true
        errorMessage = null

        val playerResult = playerRepository.getProfile()
        val attendanceResult = attendanceRepository.getMyAttendance()

        playerResult
            .onSuccess { loaded ->
                player = loaded

                if (loaded.hasProfilePhoto) {
                    playerRepository.getProfilePhoto()
                        .onSuccess { bytes ->
                            profilePhoto = bytes?.toImageBitmapOrNull()
                        }
                }
            }
            .onFailure {
                errorMessage = it.message ?: "Could not load profile."
            }

        attendanceResult
            .onSuccess { attendance = it }
            .onFailure {
                if (errorMessage == null) {
                    errorMessage = it.message ?: "Could not load attendance."
                }
            }

        isLoading = false
    }

    Scaffold(
        containerColor = ProfileBackground,
        bottomBar = {
            AppBottomBar(
                selectedRoute = Routes.PROFILE,
                onNavigate = onNavigate
            )
        }
    ) { innerPadding ->
        when {
            isLoading -> Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(innerPadding),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator(color = ProfileGreen)
            }

            player == null -> Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(innerPadding)
                    .padding(24.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.Center
            ) {
                Text(
                    text = errorMessage ?: "Could not load the player profile.",
                    color = AppColors.Error,
                    textAlign = TextAlign.Center
                )

                Spacer(Modifier.size(14.dp))

                OutlinedButton(
                    onClick = {
                        sessionManager.clearSession()
                        onLogout()
                    }
                ) {
                    Text("Return to Login")
                }
            }

            else -> ProfileContent(
                player = player!!,
                attendance = attendance,
                attendanceError = if (attendance.isEmpty()) errorMessage else null,
                innerPadding = innerPadding,
                profilePhoto = profilePhoto,
                isPhotoUploading = isPhotoUploading,
                isEditing = isEditing,
                isSaving = isSaving,
                draftAge = draftAge,
                draftEmail = draftEmail,
                draftPhone = draftPhone,
                draftEmergencyName = draftEmergencyName,
                draftEmergencyPhone = draftEmergencyPhone,
                actionMessage = actionMessage,
                onEdit = ::startEditing,
                onCancelEdit = ::cancelEditing,
                onAgeChange = { draftAge = it.filter(Char::isDigit).take(3) },
                onEmailChange = { draftEmail = it },
                onPhoneChange = { draftPhone = it },
                onEmergencyNameChange = { draftEmergencyName = it.take(120) },
                onEmergencyPhoneChange = { draftEmergencyPhone = it.take(30) },
                onPickPhoto = {
                    if (!isPhotoUploading) {
                        photoPicker.launch("image/*")
                    }
                },
                onSave = {
                    val current = player ?: return@ProfileContent
                    val age = draftAge.toIntOrNull()
                    val email = draftEmail.trim()
                    val phone = draftPhone.trim()
                    val emergencyName = draftEmergencyName.trim()
                    val emergencyPhone = draftEmergencyPhone.trim()

                    val validationMessage = when {
                        age == null || age !in 5..100 ->
                            "Enter an age between 5 and 100."

                        email.isBlank() ||
                            !Patterns.EMAIL_ADDRESS.matcher(email).matches() ->
                            "Enter a valid email address."

                        phone.isBlank() ->
                            "Enter a phone number."

                        emergencyName.isBlank() ->
                            "Enter the emergency contact's name."

                        emergencyPhone.isBlank() ->
                            "Enter the emergency contact's phone number."

                        else -> null
                    }

                    if (validationMessage != null) {
                        actionMessage = validationMessage
                        return@ProfileContent
                    }

                    val emailChanged = !email.equals(
                        current.email,
                        ignoreCase = true
                    )

                    scope.launch {
                        isSaving = true
                        actionMessage = null

                        playerRepository.updateProfile(
                            age = age!!,
                            email = email,
                            phone = phone,
                            emergencyContactName = emergencyName,
                            emergencyContactPhone = emergencyPhone
                        )
                            .onSuccess { updated ->
                                player = updated
                                isEditing = false

                                if (emailChanged) {
                                    Toast.makeText(
                                        context,
                                        "Email updated. Sign in again with your new email.",
                                        Toast.LENGTH_LONG
                                    ).show()
                                    sessionManager.clearSession()
                                    onLogout()
                                } else {
                                    Toast.makeText(
                                        context,
                                        "Profile updated.",
                                        Toast.LENGTH_SHORT
                                    ).show()
                                }
                            }
                            .onFailure { failure ->
                                actionMessage = failure.message
                                    ?: "Could not save the profile."
                            }

                        isSaving = false
                    }
                },
                onLogout = {
                    sessionManager.clearSession()
                    onLogout()
                }
            )
        }
    }
}

@Composable
private fun ProfileContent(
    player: PlayerProfileResponse,
    attendance: List<AttendanceResponse>,
    attendanceError: String?,
    innerPadding: PaddingValues,
    profilePhoto: ImageBitmap?,
    isPhotoUploading: Boolean,
    isEditing: Boolean,
    isSaving: Boolean,
    draftAge: String,
    draftEmail: String,
    draftPhone: String,
    draftEmergencyName: String,
    draftEmergencyPhone: String,
    actionMessage: String?,
    onEdit: () -> Unit,
    onCancelEdit: () -> Unit,
    onAgeChange: (String) -> Unit,
    onEmailChange: (String) -> Unit,
    onPhoneChange: (String) -> Unit,
    onEmergencyNameChange: (String) -> Unit,
    onEmergencyPhoneChange: (String) -> Unit,
    onPickPhoto: () -> Unit,
    onSave: () -> Unit,
    onLogout: () -> Unit
) {
    val initials = player.name
        .trim()
        .split(Regex("\\s+"))
        .filter(String::isNotBlank)
        .take(2)
        .mapNotNull { it.firstOrNull()?.uppercase() }
        .joinToString("")
        .ifBlank { "PV" }

    val total = attendance.size
    val present = attendance.count { it.status.equals("Present", true) }
    val rate = if (total == 0) {
        0.0
    } else {
        present.toDouble() / total.toDouble() * 100.0
    }

    val joinedDate = formatProfileDate(player.joinedDate.orEmpty())
        .ifBlank { "Not recorded" }

    LazyColumn(
        modifier = Modifier
            .fillMaxSize()
            .padding(innerPadding),
        contentPadding = PaddingValues(bottom = 28.dp)
    ) {
        item {
            ProfileHeader(
                player = player,
                initials = initials,
                profilePhoto = profilePhoto,
                isPhotoUploading = isPhotoUploading,
                isEditing = isEditing,
                onEdit = onEdit,
                onCancelEdit = onCancelEdit,
                onPickPhoto = onPickPhoto
            )
        }

        item {
            ProfileStatsRow(
                joinedDate = joinedDate,
                matches = player.matches,
                attendanceRate = rate
            )
        }

        if (!actionMessage.isNullOrBlank()) {
            item {
                Surface(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp, vertical = 8.dp),
                    color = AppColors.Error.copy(alpha = 0.08f),
                    shape = RoundedCornerShape(10.dp),
                    border = BorderStroke(
                        1.dp,
                        AppColors.Error.copy(alpha = 0.20f)
                    )
                ) {
                    Text(
                        modifier = Modifier.padding(12.dp),
                        text = actionMessage,
                        color = AppColors.Error,
                        fontSize = 13.sp
                    )
                }
            }
        }

        item {
            ProfileInfoCard(
                title = "Personal Information",
                rows = listOf(
                    ProfileRow(
                        icon = Icons.Filled.CalendarMonth,
                        label = "Age",
                        value = "${player.age} years old",
                        editable = true,
                        editValue = draftAge,
                        keyboardType = KeyboardType.Number,
                        onValueChange = onAgeChange
                    ),
                    ProfileRow(
                        Icons.Filled.SportsVolleyball,
                        "Position",
                        player.position
                    ),
                    ProfileRow(
                        Icons.Filled.Groups,
                        "Team",
                        player.team
                    ),
                    ProfileRow(
                        Icons.Filled.Info,
                        "Classification",
                        player.disability
                    ),
                    ProfileRow(
                        Icons.Filled.CheckCircle,
                        "Status",
                        player.status
                    )
                ),
                isEditing = isEditing
            )
        }

        item {
            ProfileInfoCard(
                title = "Contact Information",
                rows = listOf(
                    ProfileRow(
                        icon = Icons.Filled.Email,
                        label = "Email",
                        value = player.email,
                        editable = true,
                        editValue = draftEmail,
                        keyboardType = KeyboardType.Email,
                        onValueChange = onEmailChange
                    ),
                    ProfileRow(
                        icon = Icons.Filled.Phone,
                        label = "Phone",
                        value = player.phone,
                        editable = true,
                        editValue = draftPhone,
                        keyboardType = KeyboardType.Phone,
                        onValueChange = onPhoneChange
                    )
                ),
                isEditing = isEditing
            )
        }

        item {
            ProfileInfoCard(
                title = "Emergency Contact Information",
                rows = listOf(
                    ProfileRow(
                        icon = Icons.Filled.Info,
                        label = "Contact Name",
                        value = player.emergencyContactName,
                        editable = true,
                        editValue = draftEmergencyName,
                        keyboardType = KeyboardType.Text,
                        onValueChange = onEmergencyNameChange
                    ),
                    ProfileRow(
                        icon = Icons.Filled.Phone,
                        label = "Contact Phone",
                        value = player.emergencyContactPhone,
                        editable = true,
                        editValue = draftEmergencyPhone,
                        keyboardType = KeyboardType.Phone,
                        onValueChange = onEmergencyPhoneChange
                    )
                ),
                isEditing = isEditing
            )
        }

        if (isEditing) {
            item {
                Column(
                    modifier = Modifier.padding(
                        horizontal = 16.dp,
                        vertical = 8.dp
                    ),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    Text(
                        text = "If you change your email, the new email becomes your login email and you will be signed out after saving.",
                        color = ProfileMuted,
                        fontSize = 12.sp
                    )

                    Button(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onSave,
                        enabled = !isSaving,
                        colors = ButtonDefaults.buttonColors(
                            containerColor = ProfileGreen,
                            contentColor = Color.White
                        ),
                        shape = RoundedCornerShape(12.dp),
                        contentPadding = PaddingValues(vertical = 13.dp)
                    ) {
                        if (isSaving) {
                            CircularProgressIndicator(
                                modifier = Modifier.size(19.dp),
                                color = Color.White,
                                strokeWidth = 2.dp
                            )
                        } else {
                            Icon(
                                Icons.Filled.Save,
                                contentDescription = null,
                                modifier = Modifier.size(19.dp)
                            )
                        }

                        Spacer(Modifier.width(8.dp))

                        Text(
                            if (isSaving) "Saving..." else "Save Changes",
                            fontWeight = FontWeight.SemiBold
                        )
                    }

                    OutlinedButton(
                        modifier = Modifier.fillMaxWidth(),
                        onClick = onCancelEdit,
                        enabled = !isSaving,
                        shape = RoundedCornerShape(12.dp),
                        contentPadding = PaddingValues(vertical = 12.dp)
                    ) {
                        Icon(
                            Icons.Filled.Close,
                            contentDescription = null,
                            modifier = Modifier.size(18.dp)
                        )
                        Spacer(Modifier.width(8.dp))
                        Text("Cancel")
                    }
                }
            }
        }

        item {
            SectionTitle("Attendance History")
        }

        when {
            attendanceError != null && attendance.isEmpty() -> item {
                EmptyAttendanceCard(attendanceError)
            }

            attendance.isEmpty() -> item {
                EmptyAttendanceCard(
                    "No attendance records are available yet."
                )
            }

            else -> items(
                items = attendance,
                key = { "attendance-${it.id}" }
            ) { record ->
                AttendanceCard(record)
            }
        }

        item {
            Button(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 18.dp),
                onClick = onLogout,
                colors = ButtonDefaults.buttonColors(
                    containerColor = Color.White,
                    contentColor = AppColors.Error
                ),
                border = BorderStroke(
                    1.dp,
                    AppColors.Error.copy(alpha = 0.28f)
                ),
                shape = RoundedCornerShape(12.dp),
                contentPadding = PaddingValues(vertical = 13.dp)
            ) {
                Icon(
                    Icons.Filled.Logout,
                    contentDescription = null,
                    modifier = Modifier.size(19.dp)
                )
                Spacer(Modifier.width(8.dp))
                Text("Logout", fontWeight = FontWeight.SemiBold)
            }
        }
    }
}

@Composable
private fun ProfileHeader(
    player: PlayerProfileResponse,
    initials: String,
    profilePhoto: ImageBitmap?,
    isPhotoUploading: Boolean,
    isEditing: Boolean,
    onEdit: () -> Unit,
    onCancelEdit: () -> Unit,
    onPickPhoto: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .background(ProfileGreen)
            .padding(horizontal = 16.dp, vertical = 14.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Box(modifier = Modifier.fillMaxWidth()) {
            Text(
                modifier = Modifier.align(Alignment.Center),
                text = "My Profile",
                color = Color.White,
                fontSize = 19.sp,
                fontWeight = FontWeight.SemiBold
            )

            IconButton(
                modifier = Modifier.align(Alignment.CenterEnd),
                onClick = if (isEditing) onCancelEdit else onEdit
            ) {
                Icon(
                    imageVector = if (isEditing) {
                        Icons.Filled.Close
                    } else {
                        Icons.Filled.Edit
                    },
                    contentDescription = if (isEditing) {
                        "Cancel editing"
                    } else {
                        "Edit profile"
                    },
                    tint = Color.White,
                    modifier = Modifier.size(21.dp)
                )
            }
        }

        Spacer(Modifier.size(14.dp))

        Box(
            modifier = Modifier
                .size(98.dp)
                .clickable(
                    enabled = !isPhotoUploading,
                    onClick = onPickPhoto
                ),
            contentAlignment = Alignment.Center
        ) {
            Box(
                modifier = Modifier
                    .size(94.dp)
                    .background(
                        Color.White.copy(alpha = 0.14f),
                        CircleShape
                    ),
                contentAlignment = Alignment.Center
            ) {
                Box(
                    modifier = Modifier
                        .size(82.dp)
                        .clip(CircleShape)
                        .background(Color.White),
                    contentAlignment = Alignment.Center
                ) {
                    if (profilePhoto != null) {
                        Image(
                            bitmap = profilePhoto,
                            contentDescription = "Profile photo",
                            modifier = Modifier.fillMaxSize(),
                            contentScale = ContentScale.Crop
                        )
                    } else {
                        Text(
                            text = initials,
                            color = ProfileGreen,
                            fontWeight = FontWeight.Bold,
                            fontSize = 28.sp
                        )
                    }

                    if (isPhotoUploading) {
                        Box(
                            modifier = Modifier
                                .fillMaxSize()
                                .background(
                                    Color.Black.copy(alpha = 0.28f)
                                ),
                            contentAlignment = Alignment.Center
                        ) {
                            CircularProgressIndicator(
                                modifier = Modifier.size(28.dp),
                                color = Color.White,
                                strokeWidth = 2.dp
                            )
                        }
                    }
                }
            }

            Box(
                modifier = Modifier
                    .align(Alignment.BottomEnd)
                    .size(30.dp)
                    .background(ProfileYellow, CircleShape),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    Icons.Filled.CameraAlt,
                    contentDescription = "Upload profile photo",
                    tint = ProfileText,
                    modifier = Modifier.size(17.dp)
                )
            }
        }

        Text(
            modifier = Modifier.padding(top = 5.dp),
            text = "Tap the photo to change it",
            color = Color.White.copy(alpha = 0.70f),
            fontSize = 10.sp
        )

        Spacer(Modifier.size(7.dp))

        Text(
            text = player.name,
            color = Color.White,
            fontWeight = FontWeight.SemiBold,
            fontSize = 21.sp,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis
        )

        Text(
            text = listOf(player.position, player.team)
                .filter { it.isNotBlank() }
                .joinToString(" • "),
            color = Color.White.copy(alpha = 0.80f),
            fontSize = 13.sp
        )
    }
}

@Composable
private fun ProfileStatsRow(
    joinedDate: String,
    matches: Int,
    attendanceRate: Double
) {
    Surface(
        modifier = Modifier.fillMaxWidth(),
        color = Color.White,
        shadowElevation = 1.dp
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(vertical = 15.dp),
            horizontalArrangement = Arrangement.SpaceEvenly
        ) {
            ProfileStat(
                label = "Joined",
                value = joinedDate,
                modifier = Modifier.weight(1f)
            )
            ProfileStat(
                label = "Matches",
                value = matches.toString(),
                modifier = Modifier.weight(1f)
            )
            ProfileStat(
                label = "Attendance Rate",
                value = String.format(
                    Locale.getDefault(),
                    "%.0f%%",
                    attendanceRate
                ),
                modifier = Modifier.weight(1f)
            )
        }
    }
}

@Composable
private fun ProfileStat(
    label: String,
    value: String,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier.padding(horizontal = 4.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            text = value,
            color = ProfileGreen,
            fontWeight = FontWeight.SemiBold,
            fontSize = if (label == "Joined") 13.sp else 19.sp,
            textAlign = TextAlign.Center,
            maxLines = 2,
            overflow = TextOverflow.Ellipsis
        )
        Text(
            text = label,
            color = ProfileMuted,
            fontSize = 10.sp,
            textAlign = TextAlign.Center,
            maxLines = 2
        )
    }
}

private data class ProfileRow(
    val icon: ImageVector,
    val label: String,
    val value: String,
    val editable: Boolean = false,
    val editValue: String = "",
    val keyboardType: KeyboardType = KeyboardType.Text,
    val onValueChange: (String) -> Unit = {}
)

@Composable
private fun ProfileInfoCard(
    title: String,
    rows: List<ProfileRow>,
    isEditing: Boolean
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 8.dp),
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.cardColors(
            containerColor = Color.White
        ),
        border = BorderStroke(1.dp, ProfileBorder),
        elevation = CardDefaults.cardElevation(
            defaultElevation = 1.dp
        )
    ) {
        Column(
            modifier = Modifier.padding(17.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            Text(
                text = title,
                color = ProfileGreen,
                fontWeight = FontWeight.SemiBold,
                fontSize = 16.sp
            )

            rows.forEach { row ->
                ProfileInfoRow(
                    row = row,
                    isEditing = isEditing
                )
            }
        }
    }
}

@Composable
private fun ProfileInfoRow(
    row: ProfileRow,
    isEditing: Boolean
) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Box(
            modifier = Modifier
                .size(40.dp)
                .background(
                    Color(0xFFF0F7F3),
                    RoundedCornerShape(10.dp)
                ),
            contentAlignment = Alignment.Center
        ) {
            Icon(
                imageVector = row.icon,
                contentDescription = null,
                tint = ProfileGreen,
                modifier = Modifier.size(20.dp)
            )
        }

        Spacer(Modifier.width(12.dp))

        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = row.label,
                color = ProfileMuted,
                fontSize = 11.sp
            )

            if (isEditing && row.editable) {
                OutlinedTextField(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(top = 3.dp),
                    value = row.editValue,
                    onValueChange = row.onValueChange,
                    singleLine = true,
                    keyboardOptions = KeyboardOptions(
                        keyboardType = row.keyboardType
                    ),
                    shape = RoundedCornerShape(9.dp),
                    textStyle = androidx.compose.ui.text.TextStyle(
                        fontSize = 14.sp,
                        color = ProfileText
                    )
                )
            } else {
                Text(
                    text = row.value.ifBlank { "—" },
                    color = ProfileText,
                    fontWeight = FontWeight.Medium,
                    fontSize = 14.sp,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )
            }
        }
    }
}

@Composable
private fun SectionTitle(title: String) {
    Text(
        modifier = Modifier.padding(
            start = 16.dp,
            end = 16.dp,
            top = 16.dp,
            bottom = 7.dp
        ),
        text = title,
        color = ProfileText,
        fontWeight = FontWeight.SemiBold,
        fontSize = 17.sp
    )
}

@Composable
private fun EmptyAttendanceCard(message: String) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 5.dp),
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(
            containerColor = Color.White
        ),
        border = BorderStroke(1.dp, ProfileBorder)
    ) {
        Text(
            modifier = Modifier.padding(16.dp),
            text = message,
            color = ProfileMuted,
            fontSize = 13.sp
        )
    }
}

@Composable
private fun AttendanceCard(attendance: AttendanceResponse) {
    val present = attendance.status.equals("Present", true)

    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 5.dp),
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(
            containerColor = Color.White
        ),
        border = BorderStroke(1.dp, ProfileBorder),
        elevation = CardDefaults.cardElevation(
            defaultElevation = 1.dp
        )
    ) {
        Row(
            modifier = Modifier.padding(14.dp),
            verticalAlignment = Alignment.Top
        ) {
            Box(
                modifier = Modifier
                    .size(40.dp)
                    .background(
                        if (present) {
                            Color(0xFFF0F7F3)
                        } else {
                            AppColors.Error.copy(alpha = 0.08f)
                        },
                        CircleShape
                    ),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    Icons.Filled.CheckCircle,
                    contentDescription = null,
                    tint = if (present) {
                        ProfileGreen
                    } else {
                        AppColors.Error
                    },
                    modifier = Modifier.size(20.dp)
                )
            }

            Spacer(Modifier.width(12.dp))

            Column(
                modifier = Modifier.weight(1f),
                verticalArrangement = Arrangement.spacedBy(3.dp)
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text(
                        modifier = Modifier.weight(1f),
                        text = attendance.eventTitle,
                        color = ProfileText,
                        fontWeight = FontWeight.SemiBold,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis
                    )
                    Text(
                        text = attendance.status,
                        color = if (present) {
                            ProfileGreen
                        } else {
                            AppColors.Error
                        },
                        fontWeight = FontWeight.SemiBold,
                        fontSize = 11.sp
                    )
                }

                Text(
                    text = listOf(
                        formatProfileDate(attendance.eventDate),
                        formatProfileTime(attendance.eventTime)
                    )
                        .filter { it.isNotBlank() }
                        .joinToString(" • "),
                    color = ProfileMuted,
                    fontSize = 12.sp
                )

                Text(
                    text = attendance.eventLocation,
                    color = ProfileMuted,
                    fontSize = 12.sp,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis
                )
            }
        }
    }
}

private fun ByteArray.toImageBitmapOrNull(): ImageBitmap? {
    if (isEmpty()) return null

    return try {
        BitmapFactory.decodeByteArray(this, 0, size)
            ?.asImageBitmap()
    } catch (_: Exception) {
        null
    }
}

private fun formatProfileDate(raw: String): String {
    val value = raw.trim()
    if (value.isBlank()) return ""

    return try {
        val isoDate = if (value.length >= 10) {
            value.substring(0, 10)
        } else {
            value
        }

        LocalDate.parse(isoDate)
            .format(
                DateTimeFormatter.ofPattern(
                    "dd MMM yyyy",
                    Locale.getDefault()
                )
            )
    } catch (_: Exception) {
        value
    }
}

private fun formatProfileTime(raw: String): String {
    val value = raw.trim()
    if (value.isBlank()) return ""

    val candidate = value
        .substringBefore(" - ")
        .substringBefore("–")
        .trim()

    return try {
        val time = when {
            candidate.length >= 8 && candidate[2] == ':' ->
                LocalTime.parse(candidate.take(8))

            candidate.length >= 5 && candidate[2] == ':' ->
                LocalTime.parse(candidate.take(5))

            else -> return value
        }

        time.format(
            DateTimeFormatter.ofPattern(
                "HH:mm",
                Locale.getDefault()
            )
        )
    } catch (_: Exception) {
        value
    }
}
