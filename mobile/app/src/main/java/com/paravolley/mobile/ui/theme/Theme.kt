package com.paravolley.mobile.ui.theme

import android.app.Activity
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.SideEffect
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalView
import androidx.core.view.WindowCompat

private val LightColorScheme = lightColorScheme(
    primary = AppColors.Green,
    onPrimary = Color.White,
    primaryContainer = AppColors.LightGreen,
    onPrimaryContainer = AppColors.DarkGreen,
    secondary = AppColors.Yellow,
    onSecondary = AppColors.DarkText,
    secondaryContainer = AppColors.LightYellow,
    onSecondaryContainer = AppColors.DarkText,
    background = AppColors.LightBackground,
    onBackground = AppColors.DarkText,
    surface = AppColors.Surface,
    onSurface = AppColors.DarkText,
    surfaceVariant = AppColors.LightBackground,
    onSurfaceVariant = AppColors.GreyText,
    outline = AppColors.Border
)

private val DarkColorScheme = darkColorScheme(
    primary = AppColors.Yellow,
    onPrimary = AppColors.DarkText,
    primaryContainer = AppColors.DarkGreen,
    onPrimaryContainer = Color.White,
    secondary = AppColors.Green,
    onSecondary = Color.White,
    background = Color(0xFF121816),
    onBackground = Color.White,
    surface = Color(0xFF1E2623),
    onSurface = Color.White
)

@Composable
fun ParaVolleyMobileTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit
) {
    val colorScheme = if (darkTheme) DarkColorScheme else LightColorScheme
    val view = LocalView.current

    if (!view.isInEditMode) {
        SideEffect {
            val window = (view.context as Activity).window
            window.statusBarColor = AppColors.DarkGreen.toArgb()
            WindowCompat.getInsetsController(window, view).isAppearanceLightStatusBars = false
        }
    }

    MaterialTheme(
        colorScheme = colorScheme,
        typography = Typography,
        content = content
    )
}
