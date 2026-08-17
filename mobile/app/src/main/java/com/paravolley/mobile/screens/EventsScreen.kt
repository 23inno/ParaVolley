package com.paravolley.mobile.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.components.AppBottomBar
import com.paravolley.mobile.components.EventCard
import com.paravolley.mobile.data.FakePlayerRepository
import com.paravolley.mobile.navigation.Routes
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun EventsScreen(
    onNavigate: (String) -> Unit
) {
    var showPastEvents by rememberSaveable {
        mutableStateOf(false)
    }

    val registeredEventIds = remember {
        mutableStateListOf<Int>()
    }

    val displayedEvents =
        FakePlayerRepository.events.filter {
            it.isPast == showPastEvents
        }

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
            modifier = Modifier.padding(innerPadding)
        ) {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(AppColors.DarkGreen)
                    .padding(20.dp)
            ) {
                Text(
                    text = "Events & Training",
                    color = Color.White,
                    fontWeight = FontWeight.Bold,
                    fontSize = 25.sp
                )
            }

            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(16.dp),
                horizontalArrangement =
                    Arrangement.spacedBy(12.dp)
            ) {
                Button(
                    modifier = Modifier.weight(1f),
                    onClick = {
                        showPastEvents = false
                    },
                    colors = ButtonDefaults.buttonColors(
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
                    modifier = Modifier.weight(1f),
                    onClick = {
                        showPastEvents = true
                    },
                    colors = ButtonDefaults.buttonColors(
                        containerColor =
                            if (showPastEvents) {
                                AppColors.DarkGreen
                            } else {
                                Color.Gray
                            }
                    )
                ) {
                    Text("Past")
                }
            }

            LazyColumn(
                modifier = Modifier.weight(1f),
                contentPadding = PaddingValues(
                    start = 16.dp,
                    end = 16.dp,
                    bottom = 24.dp
                ),
                verticalArrangement =
                    Arrangement.spacedBy(12.dp)
            ) {
                items(displayedEvents) { event ->
                    val registered =
                        event.isRegistered ||
                                registeredEventIds.contains(
                                    event.id
                                )

                    val displayedEvent =
                        if (
                            registered &&
                            !event.isPast
                        ) {
                            event.copy(
                                status = "Registered"
                            )
                        } else {
                            event
                        }

                    val buttonText = when {
                        event.isPast ->
                            "View details"

                        registered ->
                            "Registered"

                        else ->
                            "Register"
                    }

                    EventCard(
                        event = displayedEvent,
                        buttonText = buttonText,
                        onButtonClick = {
                            if (
                                !event.isPast &&
                                !registered
                            ) {
                                registeredEventIds.add(
                                    event.id
                                )
                            }
                        }
                    )
                }
            }
        }
    }
}