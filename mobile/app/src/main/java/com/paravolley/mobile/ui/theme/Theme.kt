package com.paravolley.mobile.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable

private val LightColorScheme = lightColorScheme(
    primary = ParaGreenPrimary,
    onPrimary = ParaSurface,
    primaryContainer = ParaGreenLight,
    onPrimaryContainer = ParaGreenDark,
    secondary = ParaGoldSecondary,
    onSecondary = ParaTextDark,
    background = ParaBackground,
    surface = ParaSurface,
    onSurface = ParaTextDark,
    error = ParaError
)

@Composable
fun ParaVolleyTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = LightColorScheme,
        content = content
    )
}
