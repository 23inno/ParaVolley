package com.paravolley.mobile.components

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.QrCodeScanner
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
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
        modifier = Modifier.fillMaxWidth(),
        color = Color.White,
        shadowElevation = 12.dp
    ) {
        Column(
            modifier = Modifier.navigationBarsPadding()
        ) {
            HorizontalDivider(color = AppColors.Border)
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(80.dp)
                    .padding(horizontal = 8.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                BottomNavItem(
                    modifier = Modifier.weight(1f),
                    label = "Home",
                    icon = Icons.Filled.Home,
                    selected = selectedRoute == Routes.DASHBOARD,
                    onClick = { onNavigate(Routes.DASHBOARD) }
                )
                BottomNavItem(
                    modifier = Modifier.weight(1f),
                    label = "Events",
                    icon = Icons.Filled.CalendarMonth,
                    selected = selectedRoute == Routes.EVENTS,
                    onClick = { onNavigate(Routes.EVENTS) }
                )
                BottomNavItem(
                    modifier = Modifier.weight(1f),
                    label = "Scan",
                    icon = Icons.Filled.QrCodeScanner,
                    selected = selectedRoute == Routes.SCANNER,
                    onClick = { onNavigate(Routes.SCANNER) }
                )
                BottomNavItem(
                    modifier = Modifier.weight(1f),
                    label = "Profile",
                    icon = Icons.Filled.Person,
                    selected = selectedRoute == Routes.PROFILE,
                    onClick = { onNavigate(Routes.PROFILE) }
                )
            }
        }
    }
}

@Composable
private fun BottomNavItem(
    modifier: Modifier,
    label: String,
    icon: ImageVector,
    selected: Boolean,
    onClick: () -> Unit
) {
    val color = if (selected) AppColors.Green else Color(0xFF9CA3AF)

    Column(
        modifier = modifier
            .clickable(onClick = onClick)
            .padding(vertical = 9.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Icon(
            imageVector = icon,
            contentDescription = label,
            tint = color,
            modifier = Modifier.size(24.dp)
        )
        Spacer(modifier = Modifier.height(3.dp))
        Text(
            text = label,
            color = color,
            fontSize = 11.sp,
            fontWeight = if (selected) FontWeight.SemiBold else FontWeight.Normal
        )
    }
}
