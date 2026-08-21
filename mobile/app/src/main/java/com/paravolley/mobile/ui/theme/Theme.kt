package com.paravolley.mobile.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable

private val ParaVolleyLightColorScheme = lightColorScheme(
    primary = ParaGreenPrimary,
    onPrimary = ParaSurface,
    primaryContainer = ParaGreenLight,
    onPrimaryContainer = ParaGreenDark,
    secondary = ParaGoldSecondary,
    onSecondary = ParaTextDark,
    secondaryContainer = ParaWarningBg,
    onSecondaryContainer = ParaWarningText,
    background = ParaBackground,
    onBackground = ParaTextDark,
    surface = ParaSurface,
    onSurface = ParaTextDark,
    surfaceVariant = ParaGreenLight,
    onSurfaceVariant = ParaTextMuted,
    outline = ParaBorder,
    error = ParaError,
    onError = ParaSurface
)

@Composable
fun ParaVolleyMobileTheme(
    content: @Composable () -> Unit
) {
    MaterialTheme(
        colorScheme = ParaVolleyLightColorScheme,
        typography = Typography,
        content = content
    )
}
