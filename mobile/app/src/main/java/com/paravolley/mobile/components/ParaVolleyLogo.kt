package com.paravolley.mobile.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.paravolley.mobile.ui.theme.AppColors

@Composable
fun ParaVolleyLogo(
    size: Dp = 110.dp,
    showText: Boolean = true
) {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Box(
            modifier = Modifier
                .size(size)
                .shadow(8.dp, RoundedCornerShape(26.dp))
                .clip(RoundedCornerShape(26.dp))
                .background(Color.White)
                .padding(8.dp),
            contentAlignment = Alignment.Center
        ) {
            Canvas(modifier = Modifier.fillMaxSize()) {
                val w = this.size.width
                val h = this.size.height
                val center = Offset(w / 2f, h / 2f)

                // Top-Left Brush Accents
                val brushGreen = Path().apply {
                    moveTo(0f, 0f)
                    quadraticBezierTo(w * 0.45f, 0f, w * 0.5f, h * 0.22f)
                    quadraticBezierTo(w * 0.2f, h * 0.35f, 0f, h * 0.35f)
                    close()
                }
                drawPath(brushGreen, color = AppColors.DarkGreen)

                val brushGold = Path().apply {
                    moveTo(0f, h * 0.25f)
                    quadraticBezierTo(w * 0.35f, h * 0.2f, w * 0.42f, h * 0.4f)
                    quadraticBezierTo(w * 0.15f, h * 0.5f, 0f, h * 0.5f)
                    close()
                }
                drawPath(brushGold, color = AppColors.Yellow)

                // Bottom-Right Brush Accents
                val brushGreenBottom = Path().apply {
                    moveTo(w, h)
                    quadraticBezierTo(w * 0.55f, h, w * 0.5f, h * 0.78f)
                    quadraticBezierTo(w * 0.8f, h * 0.65f, w, h * 0.65f)
                    close()
                }
                drawPath(brushGreenBottom, color = AppColors.DarkGreen)

                val brushGoldBottom = Path().apply {
                    moveTo(w, h * 0.75f)
                    quadraticBezierTo(w * 0.65f, h * 0.8f, w * 0.58f, h * 0.6f)
                    quadraticBezierTo(w * 0.85f, h * 0.5f, w, h * 0.5f)
                    close()
                }
                drawPath(brushGoldBottom, color = AppColors.Yellow)

                // 4-Star Athletes Graphic
                val starRadius = w * 0.18f
                drawCircle(color = AppColors.DarkGreen, radius = starRadius * 0.75f, center = Offset(center.x, center.y - starRadius))
                drawCircle(color = AppColors.Yellow, radius = starRadius * 0.75f, center = Offset(center.x + starRadius, center.y))
                drawCircle(color = AppColors.Green, radius = starRadius * 0.75f, center = Offset(center.x, center.y + starRadius))
                drawCircle(color = AppColors.Yellow, radius = starRadius * 0.75f, center = Offset(center.x - starRadius, center.y))

                // Center Hole
                drawCircle(color = Color.White, radius = starRadius * 0.5f, center = center)

                // Volleyball icon inside center
                drawCircle(
                    brush = Brush.linearGradient(listOf(AppColors.Green, AppColors.Yellow)),
                    radius = starRadius * 0.35f,
                    center = center,
                    style = Stroke(width = 3f)
                )
            }
        }

        if (showText) {
            Spacer(modifier = Modifier.height(10.dp))
            Text(
                text = "PARA",
                color = Color.White,
                fontSize = 24.sp,
                fontWeight = FontWeight.Black,
                letterSpacing = 2.sp
            )
            Text(
                text = "VOLLEY",
                color = Color.White,
                fontSize = 22.sp,
                fontWeight = FontWeight.ExtraBold,
                letterSpacing = 3.sp
            )
            Text(
                text = "MPUMALANGA",
                color = AppColors.Yellow,
                fontSize = 12.sp,
                fontWeight = FontWeight.Bold,
                letterSpacing = 2.5.sp
            )
        }
    }
}
