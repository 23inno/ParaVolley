package com.paravolley.mobile.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.paravolley.mobile.screens.DashboardScreen
import com.paravolley.mobile.screens.EventsScreen
import com.paravolley.mobile.screens.LoginScreen
import com.paravolley.mobile.screens.NotificationsScreen
import com.paravolley.mobile.screens.ProfileScreen
import com.paravolley.mobile.screens.ScannerScreen

@Composable
fun ParaVolleyApp() {
    val navController = rememberNavController()

    val navigateFromBottomBar: (String) -> Unit = { route ->
        navController.navigate(route) {
            popUpTo(Routes.DASHBOARD) {
                saveState = true
            }

            launchSingleTop = true
            restoreState = true
        }
    }

    NavHost(
        navController = navController,
        startDestination = Routes.LOGIN
    ) {
        composable(Routes.LOGIN) {
            LoginScreen(
                onLoginSuccessful = {
                    navController.navigate(
                        Routes.DASHBOARD
                    ) {
                        popUpTo(Routes.LOGIN) {
                            inclusive = true
                        }

                        launchSingleTop = true
                    }
                }
            )
        }

        composable(Routes.DASHBOARD) {
            DashboardScreen(
                onNavigate = navigateFromBottomBar,
                onOpenNotifications = {
                    navController.navigate(
                        Routes.NOTIFICATIONS
                    ) {
                        launchSingleTop = true
                    }
                }
            )
        }

        composable(Routes.EVENTS) {
            EventsScreen(
                onNavigate = navigateFromBottomBar
            )
        }

        composable(Routes.NOTIFICATIONS) {
            NotificationsScreen(
                onBack = {
                    navController.popBackStack()
                }
            )
        }

        composable(Routes.PROFILE) {
            ProfileScreen(
                onNavigate = navigateFromBottomBar,
                onLogout = {
                    navController.navigate(
                        Routes.LOGIN
                    ) {
                        popUpTo(Routes.DASHBOARD) {
                            inclusive = true
                        }

                        launchSingleTop = true
                    }
                }
            )
        }

        composable(Routes.SCANNER) {
            ScannerScreen(
                onBack = {
                    navController.popBackStack()
                }
            )
        }
    }
}