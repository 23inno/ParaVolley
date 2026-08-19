package com.paravolley.mobile.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.components.EventCard
import com.paravolley.mobile.network.models.EventDto
import com.paravolley.mobile.ui.theme.*

@Composable
fun EventsScreen(
    events: List<EventDto>,
    registeredEventIds: Set<Int>,
    onToggleRegistration: (Int, Boolean) -> Unit
) {
    var selectedTab by remember { mutableIntStateOf(0) }
    val tabs = listOf("Upcoming", "Registered")

    Column(modifier = Modifier.fillMaxSize()) {
        Text(
            text = "Events & Training",
            style = MaterialTheme.typography.titleLarge,
            fontWeight = FontWeight.Bold,
            color = ParaGreenPrimary,
            modifier = Modifier.padding(horizontal = 20.dp, vertical = 16.dp)
        )

        TabRow(
            selectedTabIndex = selectedTab,
            containerColor = ParaSurface,
            contentColor = ParaGreenPrimary
        ) {
            tabs.forEachIndexed { index, title ->
                Tab(
                    selected = selectedTab == index,
                    onClick = { selectedTab = index },
                    text = { Text(title, fontWeight = FontWeight.SemiBold, fontSize = 14.sp) }
                )
            }
        }

        val filteredEvents = if (selectedTab == 0) {
            events
        } else {
            events.filter { registeredEventIds.contains(it.id) }
        }

        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            if (filteredEvents.isEmpty()) {
                item {
                    Text(
                        text = if (selectedTab == 0) "No events available." else "You haven't registered for any events yet.",
                        color = ParaTextMuted,
                        modifier = Modifier.padding(16.dp)
                    )
                }
            } else {
                items(filteredEvents) { event ->
                    val isReg = registeredEventIds.contains(event.id)
                    EventCard(
                        event = event,
                        isRegistered = isReg,
                        onActionClick = { onToggleRegistration(event.id, isReg) }
                    )
                }
            }
        }
    }
}
