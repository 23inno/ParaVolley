package com.paravolley.mobile.navigation

import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.key
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.paravolley.mobile.network.SessionEvents
import com.paravolley.mobile.network.SessionManager
import com.paravolley.mobile.notifications.NotificationRouter
import com.paravolley.mobile.screens.DashboardScreen
import com.paravolley.mobile.screens.EventsScreen
import com.paravolley.mobile.screens.LoginScreen
import com.paravolley.mobile.screens.NotificationsScreen
import com.paravolley.mobile.screens.ProfileScreen
import com.paravolley.mobile.screens.RegisterPlayerScreen
import com.paravolley.mobile.screens.ResultsScreen
import com.paravolley.mobile.screens.UpcomingMatchesScreen
import com.paravolley.mobile.screens.ScannerScreen
import kotlinx.coroutines.delay

@Composable
fun ParaVolleyApp(
    notificationRoute: String? = null,
    onNotificationRouteConsumed: () -> Unit = {}
) {
    val navController = rememberNavController()
    val context = LocalContext.current
    val sessionManager = SessionManager(
        context.applicationContext
    )
    val startDestination =
        if (sessionManager.hasValidPlayerSession()) {
            Routes.DASHBOARD
        } else {
            Routes.LOGIN
        }

    LaunchedEffect(Unit) {
        SessionEvents.sessionExpired.collect {
            navController.navigate(Routes.LOGIN) {
                popUpTo(0)
                launchSingleTop = true
            }
        }
    }

    LaunchedEffect(notificationRoute) {
        val destination = notificationRoute

        if (
            NotificationRouter.isSupportedDestination(
                destination
            ) &&
            sessionManager.hasValidPlayerSession()
        ) {
            navController.navigate(destination!!) {
                launchSingleTop = true
            }

            onNotificationRouteConsumed()
        }
    }

    val navigateFromBottomBar: (String) -> Unit = { route ->
        if (route == Routes.DASHBOARD) {
            val returnedToDashboard =
                navController.popBackStack(
                    route = Routes.DASHBOARD,
                    inclusive = false
                )

            if (!returnedToDashboard &&
                navController.currentDestination?.route != Routes.DASHBOARD
            ) {
                navController.navigate(Routes.DASHBOARD) {
                    launchSingleTop = true
                }
            }
        } else {
            navController.navigate(route) {
                popUpTo(Routes.DASHBOARD) {
                    inclusive = false
                }

                launchSingleTop = true
            }
        }
    }

    NavHost(
        navController = navController,
        startDestination = startDestination
    ) {
        composable(Routes.LOGIN) {
            LoginScreen(
                onLoginSuccessful = {
                    val pendingDestination =
                        notificationRoute
                            ?.takeIf {
                                NotificationRouter
                                    .isSupportedDestination(
                                        it
                                    )
                            }

                    navController.navigate(
                        Routes.DASHBOARD
                    ) {
                        popUpTo(Routes.LOGIN) {
                            inclusive = true
                        }

                        launchSingleTop = true
                    }

                    if (
                        pendingDestination != null &&
                        pendingDestination != Routes.DASHBOARD
                    ) {
                        navController.navigate(
                            pendingDestination
                        ) {
                            launchSingleTop = true
                        }
                    }

                    if (pendingDestination != null) {
                        onNotificationRouteConsumed()
                    }
                },
                onRegister = {
                    navController.navigate(Routes.REGISTER)
                }
            )
        }

        composable(Routes.REGISTER) {
            RegisterPlayerScreen(
                onBackToLogin = {
                    navController.popBackStack()
                }
            )
        }

        composable(Routes.DASHBOARD) { backStackEntry ->
            RefreshableDestination(
                lifecycle = backStackEntry.lifecycle
            ) {
                DashboardScreen(
                    onNavigate = navigateFromBottomBar,
                    onOpenNotifications = { announcementId ->
                        val route = announcementId
                            ?.let { "${Routes.NOTIFICATIONS}?announcementId=$it" }
                            ?: Routes.NOTIFICATIONS

                        navController.navigate(route) {
                            launchSingleTop = true
                        }
                    }
                )
            }
        }

        composable(Routes.EVENTS) { backStackEntry ->
            RefreshableDestination(
                lifecycle = backStackEntry.lifecycle
            ) {
                EventsScreen(
                    onNavigate = navigateFromBottomBar
                )
            }
        }

        composable(Routes.RESULTS) { backStackEntry ->
            RefreshableDestination(
                lifecycle = backStackEntry.lifecycle
            ) {
                ResultsScreen(
                    onNavigate = navigateFromBottomBar
                )
            }
        }

        composable(Routes.UPCOMING_MATCHES) { backStackEntry ->
            RefreshableDestination(
                lifecycle = backStackEntry.lifecycle
            ) {
                UpcomingMatchesScreen(
                    onNavigate = navigateFromBottomBar
                )
            }
        }

        composable(
            route = "${Routes.NOTIFICATIONS}?announcementId={announcementId}",
            arguments = listOf(
                navArgument("announcementId") {
                    type = NavType.IntType
                    defaultValue = -1
                }
            )
        ) { backStackEntry ->
            val initialAnnouncementId = backStackEntry.arguments
                ?.getInt("announcementId")
                ?.takeIf { it >= 0 }

            RefreshableDestination(
                lifecycle = backStackEntry.lifecycle
            ) {
                NotificationsScreen(
                    initialAnnouncementId = initialAnnouncementId,
                    onBack = {
                        navController.popBackStack()
                    }
                )
            }
        }

        composable(Routes.PROFILE) { backStackEntry ->
            RefreshableDestination(
                lifecycle = backStackEntry.lifecycle,
                refreshOnResume = false
            ) {
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

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun RefreshableDestination(
    lifecycle: Lifecycle,
    refreshOnResume: Boolean = true,
    content: @Composable () -> Unit
) {
    var refreshGeneration by remember {
        mutableIntStateOf(0)
    }

    var isRefreshing by remember {
        mutableStateOf(false)
    }

    var hasResumedOnce by remember(lifecycle) {
        mutableStateOf(false)
    }

    DisposableEffect(lifecycle, refreshOnResume) {
        if (!refreshOnResume) {
            onDispose { }
        } else {
            val observer = LifecycleEventObserver { _, event ->
                if (event == Lifecycle.Event.ON_RESUME) {
                    if (hasResumedOnce) {
                        refreshGeneration++
                    } else {
                        hasResumedOnce = true
                    }
                }
            }

            lifecycle.addObserver(observer)

            onDispose {
                lifecycle.removeObserver(observer)
            }
        }
    }

    LaunchedEffect(refreshGeneration) {
        if (isRefreshing) {
            delay(350)
            isRefreshing = false
        }
    }

    PullToRefreshBox(
        isRefreshing = isRefreshing,
        onRefresh = {
            isRefreshing = true
            refreshGeneration++
        },
        modifier = Modifier.fillMaxSize()
    ) {
        key(refreshGeneration) {
            content()
        }
    }
}
