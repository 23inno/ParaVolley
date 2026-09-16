package com.paravolley.mobile.components

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
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
        shadowElevation = 12.dp,
        modifier = Modifier.fillMaxWidth()
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .navigationBarsPadding()
                .padding(horizontal = 8.dp, vertical = 6.dp),
            horizontalArrangement = Arrangement.SpaceAround,
            verticalAlignment = Alignment.CenterVertically
        ) {
            BottomNavItem(
                label = "Home",
                iconChar = "🏠",
                route = Routes.DASHBOARD,
                isSelected = selectedRoute == Routes.DASHBOARD,
                onClick = { onNavigate(Routes.DASHBOARD) }
            )
            BottomNavItem(
                label = "Events",
                iconChar = "📅",
                route = Routes.EVENTS,
                isSelected = selectedRoute == Routes.EVENTS,
                onClick = { onNavigate(Routes.EVENTS) }
            )
            BottomNavItem(
                label = "Scan QR",
                iconChar = "📷",
                route = Routes.SCANNER,
                isSelected = selectedRoute == Routes.SCANNER,
                isScanAction = true,
                onClick = { onNavigate(Routes.SCANNER) }
            )
            BottomNavItem(
                label = "Profile",
                iconChar = "👤",
                route = Routes.PROFILE,
                isSelected = selectedRoute == Routes.PROFILE,
                onClick = { onNavigate(Routes.PROFILE) }
            )
        }
    }
}

@Composable
private fun BottomNavItem(
    label: String,
    iconChar: String,
    route: String,
    isSelected: Boolean,
    isScanAction: Boolean = false,
    onClick: () -> Unit
) {
    Column(
        modifier = Modifier
            .clip(RoundedCornerShape(16.dp))
            .clickable(onClick = onClick)
            .padding(horizontal = 14.dp, vertical = 6.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Box(
            modifier = Modifier
                .clip(CircleShape)
                .background(
                    if (isScanAction) AppColors.Yellow
                    else if (isSelected) AppColors.LightGreen
                    else Color.Transparent
                )
                .padding(if (isScanAction) 8.dp else 4.dp),
            contentAlignment = Alignment.Center
        ) {
            Text(
                text = iconChar,
                fontSize = if (isScanAction) 20.sp else 18.sp
            )
        }

        Spacer(modifier = Modifier.height(2.dp))

        Text(
            text = label,
            fontSize = 11.sp,
            color = if (isSelected) AppColors.Green else AppColors.GreyText,
            fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Medium
        )
    }
}
