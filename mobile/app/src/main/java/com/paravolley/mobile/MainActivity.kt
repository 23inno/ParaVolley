package com.paravolley.mobile

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import com.paravolley.mobile.navigation.ParaVolleyApp
import com.paravolley.mobile.ui.theme.ParaVolleyMobileTheme

class MainActivity : ComponentActivity() {

    override fun onCreate(
        savedInstanceState: Bundle?
    ) {
        super.onCreate(savedInstanceState)

        enableEdgeToEdge()

        setContent {
            ParaVolleyMobileTheme {
                ParaVolleyApp()
            }
        }
    }
}