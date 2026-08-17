package com.paravolley.mobile.components

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.navigation.Routes
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun AppBottomBar(
    selectedRoute: String,
    onNavigate: (String) -> Unit
) {
    Surface(
        color = Color.White,
        shadowElevation = 8.dp
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(
                    horizontal = 4.dp,
                    vertical = 6.dp
                ),
            horizontalArrangement =
                Arrangement.SpaceEvenly
        ) {
            BottomBarItem(
                modifier = Modifier.weight(1f),
                label = "Home",
                symbol = "⌂",
                route = Routes.DASHBOARD,
                selectedRoute = selectedRoute,
                onNavigate = onNavigate
            )

            BottomBarItem(
                modifier = Modifier.weight(1f),
                label = "Events",
                symbol = "▤",
                route = Routes.EVENTS,
                selectedRoute = selectedRoute,
                onNavigate = onNavigate
            )

            BottomBarItem(
                modifier = Modifier.weight(1f),
                label = "Scan",
                symbol = "▣",
                route = Routes.SCANNER,
                selectedRoute = selectedRoute,
                onNavigate = onNavigate
            )

            BottomBarItem(
                modifier = Modifier.weight(1f),
                label = "Profile",
                symbol = "●",
                route = Routes.PROFILE,
                selectedRoute = selectedRoute,
                onNavigate = onNavigate
            )
        }
    }
}

@Composable
private fun BottomBarItem(
    modifier: Modifier,
    label: String,
    symbol: String,
    route: String,
    selectedRoute: String,
    onNavigate: (String) -> Unit
) {
    val selected = selectedRoute == route

    TextButton(
        modifier = modifier.height(64.dp),
        onClick = {
            onNavigate(route)
        },
        colors = ButtonDefaults.textButtonColors(
            containerColor =
                if (selected) {
                    AppColors.Yellow
                } else {
                    Color.Transparent
                },
            contentColor = AppColors.DarkGreen
        )
    ) {
        Column(
            horizontalAlignment =
                Alignment.CenterHorizontally
        ) {
            Text(
                text = symbol,
                fontSize = 18.sp
            )

            Text(
                text = label,
                fontSize = 12.sp,
                fontWeight =
                    if (selected) {
                        FontWeight.Bold
                    } else {
                        FontWeight.Normal
                    }
            )
        }
    }
}