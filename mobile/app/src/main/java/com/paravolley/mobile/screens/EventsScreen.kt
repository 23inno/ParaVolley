package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Scaffold
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
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
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

    val repository = remember {
        EventsRepository(
            context.applicationContext
        )
    }

    val coroutineScope =
        rememberCoroutineScope()

    var events by remember {
        mutableStateOf<List<EventResponse>>(
            emptyList()
        )
    }

    var registrations by remember {
        mutableStateOf<List<EventRegistrationResponse>>(
            emptyList()
        )
    }

    var showPastEvents by rememberSaveable {
        mutableStateOf(false)
    }

    var isLoading by remember {
        mutableStateOf(true)
    }

    var busyEventId by remember {
        mutableStateOf<Int?>(null)
    }

    var errorMessage by remember {
        mutableStateOf<String?>(null)
    }

    var successMessage by remember {
        mutableStateOf<String?>(null)
    }

    suspend fun loadData() {
        isLoading = true
        errorMessage = null

        val eventsResult =
            repository.getEvents()

        val registrationsResult =
            repository.getMyRegistrations()

        eventsResult
            .onSuccess { response ->
                events = response
            }
            .onFailure { exception ->
                errorMessage =
                    exception.message
                        ?: "Could not load events."
            }

        registrationsResult
            .onSuccess { response ->
                registrations = response
            }
            .onFailure { exception ->
                if (errorMessage == null) {
                    errorMessage =
                        exception.message
                            ?: "Could not load registrations."
                }
            }

        isLoading = false
    }

    LaunchedEffect(Unit) {
        loadData()
    }

    val displayedEvents =
        events.filter { event ->

            val isUpcoming =
                event.status.equals(
                    "Upcoming",
                    ignoreCase = true
                )

            if (showPastEvents) {
                !isUpcoming
            } else {
                isUpcoming
            }
        }

    Scaffold(
        containerColor =
            AppColors.LightBackground,
        bottomBar = {
            AppBottomBar(
                selectedRoute =
                    Routes.EVENTS,
                onNavigate =
                    onNavigate
            )
        }
    ) { innerPadding ->

        Column(
            modifier =
                Modifier.padding(
                    innerPadding
                )
        ) {
            Column(
                modifier =
                    Modifier
                        .fillMaxWidth()
                        .background(
                            AppColors.DarkGreen
                        )
                        .padding(20.dp)
            ) {
                Text(
                    text =
                        "Events & Training",
                    color =
                        Color.White,
                    fontWeight =
                        FontWeight.Bold,
                    fontSize =
                        25.sp
                )
            }

            Row(
                modifier =
                    Modifier
                        .fillMaxWidth()
                        .padding(16.dp),
                horizontalArrangement =
                    Arrangement.spacedBy(
                        12.dp
                    )
            ) {
                Button(
                    modifier =
                        Modifier.weight(1f),
                    onClick = {
                        showPastEvents = false
                    },
                    colors =
                        ButtonDefaults.buttonColors(
                            containerColor =
                                if (!showPastEvents) {
                                    AppColors.DarkGreen
                                } else {
                                    Color.Gray
                                }
                        )
                ) {
                    Text("Upcoming")
                }

                Button(
                    modifier =
                        Modifier.weight(1f),
                    onClick = {
                        showPastEvents = true
                    },
                    colors =
                        ButtonDefaults.buttonColors(
                            containerColor =
                                if (showPastEvents) {
                                    AppColors.DarkGreen
                                } else {
                                    Color.Gray
                                }
                        )
                ) {
                    Text("Past / Closed")
                }
            }

            successMessage?.let { message ->
                Text(
                    modifier =
                        Modifier
                            .fillMaxWidth()
                            .padding(
                                horizontal = 16.dp,
                                vertical = 8.dp
                            ),
                    text = message,
                    color =
                        AppColors.DarkGreen,
                    fontWeight =
                        FontWeight.Medium
                )
            }

            errorMessage?.let { message ->
                Text(
                    modifier =
                        Modifier
                            .fillMaxWidth()
                            .padding(
                                horizontal = 16.dp,
                                vertical = 8.dp
                            ),
                    text = message,
                    color = Color.Red
                )
            }

            if (isLoading) {
                Box(
                    modifier =
                        Modifier
                            .fillMaxWidth()
                            .padding(40.dp),
                    contentAlignment =
                        Alignment.Center
                ) {
                    CircularProgressIndicator()
                }
            } else if (
                displayedEvents.isEmpty()
            ) {
                Box(
                    modifier =
                        Modifier
                            .fillMaxWidth()
                            .padding(32.dp),
                    contentAlignment =
                        Alignment.Center
                ) {
                    Text(
                        text =
                            if (showPastEvents) {
                                "No past or closed events are available."
                            } else {
                                "No upcoming events are available."
                            },
                        color =
                            AppColors.GreyText
                    )
                }
            } else {
                LazyColumn(
                    modifier =
                        Modifier.weight(1f),
                    contentPadding =
                        PaddingValues(
                            start = 16.dp,
                            end = 16.dp,
                            bottom = 24.dp
                        ),
                    verticalArrangement =
                        Arrangement.spacedBy(
                            12.dp
                        )
                ) {
                    items(
                        items = displayedEvents,
                        key = { event ->
                            event.id
                        }
                    ) { event ->

                        val registration =
                            registrations
                                .firstOrNull { item ->
                                    item.eventId ==
                                        event.id
                                }

                        val isRegistered =
                            registration
                                ?.registrationStatus
                                ?.equals(
                                    "Registered",
                                    ignoreCase = true
                                ) == true

                        val isUpcoming =
                            event.status.equals(
                                "Upcoming",
                                ignoreCase = true
                            )

                        val isBusy =
                            busyEventId ==
                                event.id

                        val buttonText =
                            when {
                                isBusy ->
                                    "Please wait..."

                                isUpcoming &&
                                    isRegistered ->
                                    "Cancel Registration"

                                isUpcoming ->
                                    "Register"

                                event.status.equals(
                                    "Cancelled",
                                    ignoreCase = true
                                ) ->
                                    "Cancelled"

                                else ->
                                    event.status
                            }

                        EventCard(
                            event = event,
                            registrationStatus =
                                registration
                                    ?.registrationStatus,
                            buttonText =
                                buttonText,
                            buttonEnabled =
                                isUpcoming &&
                                    !isBusy,
                            onButtonClick = {
                                successMessage = null
                                errorMessage = null

                                busyEventId =
                                    event.id

                                coroutineScope.launch {
                                    val result =
                                        if (
                                            isRegistered
                                        ) {
                                            repository
                                                .cancelRegistration(
                                                    event.id
                                                )
                                        } else {
                                            repository
                                                .registerForEvent(
                                                    event.id
                                                )
                                        }

                                    result
                                        .onSuccess {
                                            response ->

                                            successMessage =
                                                if (
                                                    response
                                                        .registrationStatus
                                                        .equals(
                                                            "Registered",
                                                            ignoreCase =
                                                                true
                                                        )
                                                ) {
                                                    "Registration successful."
                                                } else {
                                                    "Registration cancelled."
                                                }

                                            loadData()
                                        }
                                        .onFailure {
                                            exception ->

                                            errorMessage =
                                                exception
                                                    .message
                                                    ?: "The event action failed."
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