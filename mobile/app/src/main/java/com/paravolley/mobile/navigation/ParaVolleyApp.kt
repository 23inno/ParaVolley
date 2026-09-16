package com.paravolley.mobile.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.paravolley.mobile.screens.*

@Composable
fun ParaVolleyApp() {
    val navController = rememberNavController()

    NavHost(
        navController = navController,
        startDestination = Routes.LOGIN
    ) {
        composable(Routes.LOGIN) {
            LoginScreen(
                onLoginSuccessful = {
                    navController.navigate(Routes.DASHBOARD) {
                        popUpTo(Routes.LOGIN) { inclusive = true }
                    }
                }
            )
        }

        composable(Routes.DASHBOARD) {
            DashboardScreen(
                onNavigate = { route ->
                    if (route != Routes.DASHBOARD) {
                        navController.navigate(route) {
                            popUpTo(Routes.DASHBOARD)
                            launchSingleTop = true
                        }
                    }
                },
                onOpenNotifications = {
                    navController.navigate(Routes.NOTIFICATIONS)
                }
            )
        }

        composable(Routes.EVENTS) {
            EventsScreen(
                onNavigate = { route ->
                    if (route != Routes.EVENTS) {
                        navController.navigate(route) {
                            popUpTo(Routes.DASHBOARD)
                            launchSingleTop = true
                        }
                    }
                }
            )
        }

        composable(Routes.SCANNER) {
            ScannerScreen(
                onBack = { navController.popBackStack() }
            )
        }

        composable(Routes.NOTIFICATIONS) {
            NotificationsScreen(
                onBack = { navController.popBackStack() }
            )
        }

        composable(Routes.PROFILE) {
            ProfileScreen(
                onNavigate = { route ->
                    if (route != Routes.PROFILE) {
                        navController.navigate(route) {
                            popUpTo(Routes.DASHBOARD)
                            launchSingleTop = true
                        }
                    }
                },
                onLogout = {
                    navController.navigate(Routes.LOGIN) {
                        popUpTo(0) { inclusive = true }
                    }
                }
            )
        }
    }
}
