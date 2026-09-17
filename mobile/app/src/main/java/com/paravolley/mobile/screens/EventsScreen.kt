package com.paravolley.mobile.screens

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.FilterList
import androidx.compose.material.icons.filled.Groups
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Tab
import androidx.compose.material3.TabRow
import androidx.compose.material3.Text
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
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
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.components.AppBottomBar
import com.paravolley.mobile.components.EventCard
import com.paravolley.mobile.navigation.Routes
import com.paravolley.mobile.network.EventRegistrationResponse
import com.paravolley.mobile.network.EventResponse
import com.paravolley.mobile.network.EventsRepository
import com.paravolley.mobile.ui.theme.AppColors
import java.time.LocalDate
import java.time.LocalTime
import java.time.format.DateTimeFormatter
import java.util.Locale
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun EventsScreen(
    onNavigate: (String) -> Unit
) {
    val context = LocalContext.current
    val repository = remember {
        EventsRepository(context.applicationContext)
    }
    val coroutineScope = rememberCoroutineScope()

    var events by remember {
        mutableStateOf<List<EventResponse>>(emptyList())
    }
    var registrations by remember {
        mutableStateOf<List<EventRegistrationResponse>>(emptyList())
    }
    var selectedTab by rememberSaveable { mutableStateOf(0) }
    var selectedType by rememberSaveable { mutableStateOf<String?>(null) }
    var selectedEvent by remember { mutableStateOf<EventResponse?>(null) }
    var isLoading by remember { mutableStateOf(true) }
    var busyEventId by remember { mutableStateOf<Int?>(null) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    var successMessage by remember { mutableStateOf<String?>(null) }

    suspend fun loadData() {
        isLoading = true
        errorMessage = null

        val eventsResult = repository.getEvents()
        val registrationsResult = repository.getMyRegistrations()

        eventsResult
            .onSuccess { events = it }
            .onFailure {
                errorMessage = it.message ?: "Could not load events."
            }

        registrationsResult
            .onSuccess { registrations = it }
            .onFailure {
                if (errorMessage == null) {
                    errorMessage =
                        it.message ?: "Could not load registrations."
                }
            }

        isLoading = false
    }

    LaunchedEffect(Unit) {
        loadData()
    }

    val statusFiltered = events.filter { event ->
        when (selectedTab) {
            0 -> event.status.equals("Upcoming", ignoreCase = true)
            1 -> registrations.any { registration ->
                registration.eventId == event.id &&
                    registration.registrationStatus.equals(
                        "Registered",
                        ignoreCase = true
                    )
            }
            else -> !event.status.equals("Upcoming", ignoreCase = true)
        }
    }

    val displayedEvents = statusFiltered.filter { event ->
        selectedType == null ||
            event.type.equals(selectedType, ignoreCase = true)
    }

    val availableTypes = statusFiltered
        .map { it.type }
        .filter { it.isNotBlank() }
        .distinct()

    Scaffold(
        containerColor = AppColors.LightBackground,
        bottomBar = {
            AppBottomBar(
                selectedRoute = Routes.EVENTS,
                onNavigate = onNavigate
            )
        }
    ) { innerPadding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
        ) {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(AppColors.Green)
                    .padding(horizontal = 20.dp, vertical = 22.dp)
            ) {
                Text(
                    text = "Events",
                    color = Color.White,
                    fontWeight = FontWeight.Bold,
                    fontSize = 25.sp
                )
                Text(
                    modifier = Modifier.padding(top = 3.dp),
                    text = "View event information and manage your registrations",
                    color = Color.White.copy(alpha = 0.78f),
                    fontSize = 13.sp
                )
            }

            TabRow(
                selectedTabIndex = selectedTab,
                containerColor = Color.White,
                contentColor = AppColors.Green,
                divider = { }
            ) {
                listOf("Upcoming", "Registered", "Past")
                    .forEachIndexed { index, title ->
                        Tab(
                            selected = selectedTab == index,
                            onClick = {
                                selectedTab = index
                                selectedType = null
                            },
                            text = {
                                Text(
                                    text = title,
                                    fontWeight =
                                        if (selectedTab == index) {
                                            FontWeight.Bold
                                        } else {
                                            FontWeight.Medium
                                        },
                                    color =
                                        if (selectedTab == index) {
                                            AppColors.Green
                                        } else {
                                            AppColors.GreyText
                                        }
                                )
                            }
                        )
                    }
            }

            if (availableTypes.isNotEmpty()) {
                LazyRow(
                    modifier = Modifier
                        .fillMaxWidth()
                        .background(Color.White),
                    contentPadding = PaddingValues(
                        horizontal = 16.dp,
                        vertical = 9.dp
                    ),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    item {
                        FilterChip(
                            selected = selectedType == null,
                            onClick = { selectedType = null },
                            label = { Text("All") },
                            leadingIcon = {
                                Icon(
                                    Icons.Filled.FilterList,
                                    contentDescription = null
                                )
                            },
                            colors = eventFilterColors()
                        )
                    }

                    items(availableTypes) { type ->
                        FilterChip(
                            selected = selectedType == type,
                            onClick = {
                                selectedType =
                                    if (selectedType == type) null else type
                            },
                            label = { Text(type) },
                            leadingIcon =
                                if (selectedType == type) {
                                    {
                                        Icon(
                                            Icons.Filled.Check,
                                            contentDescription = null
                                        )
                                    }
                                } else {
                                    null
                                },
                            colors = eventFilterColors()
                        )
                    }
                }
            }

            successMessage?.let { message ->
                Surface(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp, vertical = 8.dp),
                    color = AppColors.LightGreen,
                    shape = RoundedCornerShape(10.dp)
                ) {
                    Text(
                        modifier = Modifier.padding(11.dp),
                        text = message,
                        color = AppColors.Green,
                        fontWeight = FontWeight.SemiBold,
                        fontSize = 13.sp
                    )
                }
            }

            errorMessage?.let { message ->
                Surface(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp, vertical = 8.dp),
                    color = AppColors.Error.copy(alpha = 0.08f),
                    shape = RoundedCornerShape(10.dp)
                ) {
                    Text(
                        modifier = Modifier.padding(11.dp),
                        text = message,
                        color = AppColors.Error,
                        fontSize = 13.sp
                    )
                }
            }

            when {
                isLoading -> {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        CircularProgressIndicator(color = AppColors.Green)
                    }
                }

                displayedEvents.isEmpty() -> {
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(32.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            text = when {
                                selectedType != null ->
                                    "No $selectedType events are available in this section."
                                selectedTab == 0 ->
                                    "No upcoming events are available."
                                selectedTab == 1 ->
                                    "You have not registered for any events yet."
                                else ->
                                    "No past or closed events are available."
                            },
                            color = AppColors.GreyText
                        )
                    }
                }

                else -> {
                    LazyColumn(
                        modifier = Modifier.weight(1f),
                        contentPadding = PaddingValues(16.dp),
                        verticalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        items(
                            items = displayedEvents,
                            key = { "event-${it.id}" }
                        ) { event ->
                            val registration = registrations
                                .firstOrNull { it.eventId == event.id }

                            EventCard(
                                event = event,
                                registrationStatus =
                                    registration?.registrationStatus,
                                formattedDate = formatEventDate(event.date),
                                formattedTime = formatEventTime(event.time),
                                onViewEvent = {
                                    successMessage = null
                                    errorMessage = null
                                    selectedEvent = event
                                }
                            )
                        }
                    }
                }
            }
        }
    }

    selectedEvent?.let { event ->
        val registration = registrations
            .firstOrNull { it.eventId == event.id }
        val isRegistered = registration
            ?.registrationStatus
            .equals("Registered", ignoreCase = true)
        val isUpcoming = event.status.equals(
            "Upcoming",
            ignoreCase = true
        )
        val isBusy = busyEventId == event.id

        EventDetailsSheet(
            event = event,
            registrationStatus = registration?.registrationStatus,
            isBusy = isBusy,
            onDismiss = {
                if (!isBusy) {
                    selectedEvent = null
                }
            },
            onRegistrationAction = {
                successMessage = null
                errorMessage = null
                busyEventId = event.id

                coroutineScope.launch {
                    val result =
                        if (isRegistered) {
                            repository.cancelRegistration(event.id)
                        } else {
                            repository.registerForEvent(event.id)
                        }

                    result
                        .onSuccess { response ->
                            successMessage =
                                if (
                                    response.registrationStatus.equals(
                                        "Registered",
                                        ignoreCase = true
                                    )
                                ) {
                                    "Registration successful. The web event register has been updated."
                                } else {
                                    "Registration cancelled. The web event register has been updated."
                                }

                            loadData()
                            selectedEvent = events
                                .firstOrNull { it.id == event.id }
                                ?: event
                        }
                        .onFailure {
                            errorMessage =
                                it.message ?: "The event action failed."
                        }

                    busyEventId = null
                }
            },
            registrationActionEnabled = isUpcoming && !isBusy
        )
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun EventDetailsSheet(
    event: EventResponse,
    registrationStatus: String?,
    isBusy: Boolean,
    onDismiss: () -> Unit,
    onRegistrationAction: () -> Unit,
    registrationActionEnabled: Boolean
) {
    val sheetState = rememberModalBottomSheetState(
        skipPartiallyExpanded = true
    )
    val isRegistered = registrationStatus.equals(
        "Registered",
        ignoreCase = true
    )
    val isUpcoming = event.status.equals(
        "Upcoming",
        ignoreCase = true
    )

    ModalBottomSheet(
        onDismissRequest = onDismiss,
        sheetState = sheetState,
        containerColor = Color.White,
        shape = RoundedCornerShape(
            topStart = 22.dp,
            topEnd = 22.dp
        )
    ) {
        LazyColumn(
            modifier = Modifier.fillMaxWidth(),
            contentPadding = PaddingValues(
                start = 22.dp,
                end = 22.dp,
                bottom = 30.dp
            ),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            item {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.Top
                ) {
                    Column(modifier = Modifier.weight(1f)) {
                        Row(
                            horizontalArrangement = Arrangement.spacedBy(7.dp)
                        ) {
                            DetailChip(
                                text = event.type,
                                background = AppColors.LightGreen,
                                foreground = AppColors.Green
                            )
                            DetailChip(
                                text =
                                    if (isRegistered) {
                                        "Registered"
                                    } else {
                                        event.status
                                    },
                                background =
                                    if (isRegistered) {
                                        AppColors.WarningBackground
                                    } else {
                                        Color(0xFFF3F4F6)
                                    },
                                foreground =
                                    if (isRegistered) {
                                        AppColors.WarningText
                                    } else {
                                        AppColors.GreyText
                                    }
                            )
                        }

                        Spacer(Modifier.height(10.dp))

                        Text(
                            text = event.title,
                            color = AppColors.DarkText,
                            fontWeight = FontWeight.Bold,
                            fontSize = 21.sp
                        )
                    }

                    Spacer(Modifier.width(8.dp))

                    IconButton(
                        enabled = !isBusy,
                        onClick = onDismiss
                    ) {
                        Icon(
                            imageVector = Icons.Filled.Close,
                            contentDescription = "Close event details",
                            tint = AppColors.GreyText
                        )
                    }
                }
            }

            item {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .background(
                            color = Color(0xFFF9FAFB),
                            shape = RoundedCornerShape(14.dp)
                        )
                        .padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(14.dp)
                ) {
                    DetailRow(
                        icon = Icons.Filled.CalendarMonth,
                        label = "Date",
                        value = formatEventDate(event.date)
                    )
                    DetailRow(
                        icon = Icons.Filled.Schedule,
                        label = "Time",
                        value = formatEventTime(event.time)
                    )
                    DetailRow(
                        icon = Icons.Filled.LocationOn,
                        label = "Venue",
                        value = event.location
                    )
                    DetailRow(
                        icon = Icons.Filled.Groups,
                        label = "Participants",
                        value = "${event.participants}"
                    )
                }
            }

            item {
                Column(
                    verticalArrangement = Arrangement.spacedBy(6.dp)
                ) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Icon(
                            imageVector = Icons.Filled.Info,
                            contentDescription = null,
                            tint = AppColors.Green,
                            modifier = Modifier.size(18.dp)
                        )
                        Spacer(Modifier.width(7.dp))
                        Text(
                            text = "Event details",
                            color = AppColors.DarkText,
                            fontWeight = FontWeight.Bold,
                            fontSize = 15.sp
                        )
                    }

                    Text(
                        text = event.description.ifBlank {
                            "No additional event description has been provided by ParaVolley administration."
                        },
                        color = AppColors.GreyText,
                        fontSize = 14.sp,
                        lineHeight = 21.sp
                    )
                }
            }

            if (registrationStatus != null) {
                item {
                    Surface(
                        modifier = Modifier.fillMaxWidth(),
                        color =
                            if (isRegistered) {
                                AppColors.LightGreen
                            } else {
                                Color(0xFFF3F4F6)
                            },
                        shape = RoundedCornerShape(12.dp)
                    ) {
                        Text(
                            modifier = Modifier.padding(13.dp),
                            text = "Registration status: $registrationStatus",
                            color =
                                if (isRegistered) {
                                    AppColors.Green
                                } else {
                                    AppColors.GreyText
                                },
                            fontWeight = FontWeight.SemiBold,
                            fontSize = 13.sp
                        )
                    }
                }
            }

            item {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    OutlinedButton(
                        modifier = Modifier
                            .weight(1f)
                            .height(48.dp),
                        enabled = !isBusy,
                        onClick = onDismiss,
                        border = BorderStroke(
                            1.dp,
                            AppColors.Border
                        ),
                        shape = RoundedCornerShape(10.dp)
                    ) {
                        Text(
                            text = "Close",
                            color = AppColors.GreyText,
                            fontWeight = FontWeight.SemiBold
                        )
                    }

                    if (isUpcoming) {
                        Button(
                            modifier = Modifier
                                .weight(1f)
                                .height(48.dp),
                            enabled = registrationActionEnabled,
                            onClick = onRegistrationAction,
                            colors = ButtonDefaults.buttonColors(
                                containerColor =
                                    if (isRegistered) {
                                        Color.White
                                    } else {
                                        AppColors.Yellow
                                    },
                                contentColor =
                                    if (isRegistered) {
                                        AppColors.Green
                                    } else {
                                        AppColors.DarkText
                                    },
                                disabledContainerColor = Color(0xFFF3F4F6),
                                disabledContentColor = AppColors.GreyText
                            ),
                            border =
                                if (isRegistered) {
                                    BorderStroke(1.dp, AppColors.Green)
                                } else {
                                    null
                                },
                            shape = RoundedCornerShape(10.dp)
                        ) {
                            if (isBusy) {
                                CircularProgressIndicator(
                                    modifier = Modifier.size(18.dp),
                                    strokeWidth = 2.dp,
                                    color = AppColors.Green
                                )
                            } else {
                                Text(
                                    text =
                                        if (isRegistered) {
                                            "Cancel Registration"
                                        } else {
                                            "Register"
                                        },
                                    fontWeight = FontWeight.Bold,
                                    maxLines = 1,
                                    overflow = TextOverflow.Ellipsis
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun DetailRow(
    icon: ImageVector,
    label: String,
    value: String
) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Box(
            modifier = Modifier
                .size(36.dp)
                .background(
                    AppColors.LightGreen,
                    RoundedCornerShape(9.dp)
                ),
            contentAlignment = Alignment.Center
        ) {
            Icon(
                imageVector = icon,
                contentDescription = null,
                tint = AppColors.Green,
                modifier = Modifier.size(18.dp)
            )
        }

        Spacer(Modifier.width(12.dp))

        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = label,
                color = AppColors.GreyText,
                fontSize = 11.sp
            )
            Text(
                text = value,
                color = AppColors.DarkText,
                fontWeight = FontWeight.Medium,
                fontSize = 13.sp
            )
        }
    }
}

@Composable
private fun DetailChip(
    text: String,
    background: Color,
    foreground: Color
) {
    Surface(
        color = background,
        contentColor = foreground,
        shape = RoundedCornerShape(999.dp)
    ) {
        Text(
            text = text,
            modifier = Modifier.padding(
                horizontal = 9.dp,
                vertical = 4.dp
            ),
            fontSize = 10.sp,
            fontWeight = FontWeight.SemiBold
        )
    }
}

private fun formatEventDate(rawDate: String): String {
    val trimmed = rawDate.trim()
    if (trimmed.isBlank()) return "Date TBC"

    val dateOnly = trimmed
        .substringBefore("T")
        .substringBefore(" ")

    return runCatching {
        LocalDate
            .parse(dateOnly, DateTimeFormatter.ISO_LOCAL_DATE)
            .format(
                DateTimeFormatter.ofPattern(
                    "dd MMM yyyy",
                    Locale.getDefault()
                )
            )
    }.getOrDefault(trimmed)
}

private fun formatEventTime(rawTime: String): String {
    val trimmed = rawTime.trim()
    if (trimmed.isBlank()) return "Time TBC"

    return runCatching {
        LocalTime
            .parse(trimmed, DateTimeFormatter.ISO_LOCAL_TIME)
            .format(
                DateTimeFormatter.ofPattern(
                    "HH:mm",
                    Locale.getDefault()
                )
            )
    }.getOrDefault(trimmed)
}

@Composable
private fun eventFilterColors() =
    FilterChipDefaults.filterChipColors(
        containerColor = Color.White,
        labelColor = AppColors.GreyText,
        iconColor = AppColors.GreyText,
        selectedContainerColor = AppColors.LightGreen,
        selectedLabelColor = AppColors.Green,
        selectedLeadingIconColor = AppColors.Green
    )
