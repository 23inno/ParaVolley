package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.FilterList
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Tab
import androidx.compose.material3.TabRow
import androidx.compose.material3.Text
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
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.platform.LocalContext
import com.paravolley.mobile.components.AppBottomBar
import com.paravolley.mobile.components.EventCard
import com.paravolley.mobile.navigation.Routes
import com.paravolley.mobile.network.EventRegistrationResponse
import com.paravolley.mobile.network.EventResponse
import com.paravolley.mobile.network.EventsRepository
import com.paravolley.mobile.ui.theme.AppColors
import kotlinx.coroutines.launch

@Composable
fun EventsScreen(
    onNavigate: (String) -> Unit
) {
    val context = LocalContext.current
    val repository = remember { EventsRepository(context.applicationContext) }
    val coroutineScope = rememberCoroutineScope()

    var events by remember { mutableStateOf<List<EventResponse>>(emptyList()) }
    var registrations by remember { mutableStateOf<List<EventRegistrationResponse>>(emptyList()) }
    var selectedTab by rememberSaveable { mutableStateOf(0) }
    var selectedType by rememberSaveable { mutableStateOf<String?>(null) }
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
            .onFailure { errorMessage = it.message ?: "Could not load events." }

        registrationsResult
            .onSuccess { registrations = it }
            .onFailure {
                if (errorMessage == null) {
                    errorMessage = it.message ?: "Could not load registrations."
                }
            }
        isLoading = false
    }

    LaunchedEffect(Unit) { loadData() }

    val statusFiltered = events.filter { event ->
        when (selectedTab) {
            0 -> event.status.equals("Upcoming", ignoreCase = true)
            1 -> registrations.any { registration ->
                registration.eventId == event.id &&
                    registration.registrationStatus.equals("Registered", ignoreCase = true)
            }
            else -> !event.status.equals("Upcoming", ignoreCase = true)
        }
    }

    val displayedEvents = statusFiltered.filter { event ->
        selectedType == null || event.type.equals(selectedType, ignoreCase = true)
    }

    val availableTypes = statusFiltered.map { it.type }.filter { it.isNotBlank() }.distinct()

    Scaffold(
        containerColor = AppColors.LightBackground,
        bottomBar = {
            AppBottomBar(selectedRoute = Routes.EVENTS, onNavigate = onNavigate)
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
                    text = "Find events, training sessions and registrations",
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
                listOf("Upcoming", "Registered", "Past").forEachIndexed { index, title ->
                    Tab(
                        selected = selectedTab == index,
                        onClick = {
                            selectedTab = index
                            selectedType = null
                        },
                        text = {
                            Text(
                                text = title,
                                fontWeight = if (selectedTab == index) FontWeight.Bold else FontWeight.Medium,
                                color = if (selectedTab == index) AppColors.Green else AppColors.GreyText
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
                    contentPadding = PaddingValues(horizontal = 16.dp, vertical = 9.dp),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    item {
                        FilterChip(
                            selected = selectedType == null,
                            onClick = { selectedType = null },
                            label = { Text("All") },
                            leadingIcon = {
                                Icon(Icons.Filled.FilterList, contentDescription = null)
                            },
                            colors = eventFilterColors()
                        )
                    }
                    items(availableTypes) { type ->
                        FilterChip(
                            selected = selectedType == type,
                            onClick = { selectedType = if (selectedType == type) null else type },
                            label = { Text(type) },
                            leadingIcon = if (selectedType == type) {
                                { Icon(Icons.Filled.Check, contentDescription = null) }
                            } else null,
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
                isLoading -> Box(
                    modifier = Modifier.fillMaxSize(),
                    contentAlignment = Alignment.Center
                ) {
                    CircularProgressIndicator(color = AppColors.Green)
                }

                displayedEvents.isEmpty() -> Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(32.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = when {
                            selectedType != null -> "No $selectedType events are available in this section."
                            selectedTab == 0 -> "No upcoming events are available."
                            selectedTab == 1 -> "You have not registered for any events yet."
                            else -> "No past or closed events are available."
                        },
                        color = AppColors.GreyText
                    )
                }

                else -> LazyColumn(
                    modifier = Modifier.weight(1f),
                    contentPadding = PaddingValues(16.dp),
                    verticalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    items(displayedEvents, key = { it.id }) { event ->
                        val registration = registrations.firstOrNull { it.eventId == event.id }
                        val isRegistered = registration?.registrationStatus.equals("Registered", ignoreCase = true)
                        val isUpcoming = event.status.equals("Upcoming", ignoreCase = true)
                        val isBusy = busyEventId == event.id
                        val buttonText = when {
                            isBusy -> "Please wait..."
                            isUpcoming && isRegistered -> "Cancel Registration"
                            isUpcoming -> "Register"
                            event.status.equals("Cancelled", ignoreCase = true) -> "Cancelled"
                            else -> event.status
                        }

                        EventCard(
                            event = event,
                            registrationStatus = registration?.registrationStatus,
                            buttonText = buttonText,
                            buttonEnabled = isUpcoming && !isBusy,
                            onButtonClick = {
                                successMessage = null
                                errorMessage = null
                                busyEventId = event.id
                                coroutineScope.launch {
                                    val result = if (isRegistered) {
                                        repository.cancelRegistration(event.id)
                                    } else {
                                        repository.registerForEvent(event.id)
                                    }

                                    result
                                        .onSuccess { response ->
                                            successMessage = if (response.registrationStatus.equals("Registered", true)) {
                                                "Registration successful."
                                            } else {
                                                "Registration cancelled."
                                            }
                                            loadData()
                                        }
                                        .onFailure {
                                            errorMessage = it.message ?: "The event action failed."
                                        }
                                    busyEventId = null
                                }
                            }
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun eventFilterColors() = FilterChipDefaults.filterChipColors(
    containerColor = Color.White,
    labelColor = AppColors.GreyText,
    iconColor = AppColors.GreyText,
    selectedContainerColor = AppColors.LightGreen,
    selectedLabelColor = AppColors.Green,
    selectedLeadingIconColor = AppColors.Green
)
