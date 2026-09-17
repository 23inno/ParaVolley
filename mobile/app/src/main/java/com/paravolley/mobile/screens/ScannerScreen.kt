package com.paravolley.mobile.screens

import android.Manifest
import android.content.pm.PackageManager
import android.view.ViewGroup
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.camera.core.Camera
import androidx.camera.core.CameraSelector
import androidx.camera.core.ExperimentalGetImage
import androidx.camera.core.ImageAnalysis
import androidx.camera.core.ImageProxy
import androidx.camera.core.Preview
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.FlashOff
import androidx.compose.material.icons.filled.FlashOn
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.rememberUpdatedState
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import androidx.lifecycle.LifecycleOwner
import com.google.mlkit.vision.barcode.BarcodeScanner
import com.google.mlkit.vision.barcode.BarcodeScannerOptions
import com.google.mlkit.vision.barcode.BarcodeScanning
import com.google.mlkit.vision.barcode.common.Barcode
import com.google.mlkit.vision.common.InputImage
import com.paravolley.mobile.network.QrAttendanceRepository
import com.paravolley.mobile.network.QrCheckInResponse
import com.paravolley.mobile.ui.theme.AppColors
import java.util.concurrent.Executors
import kotlinx.coroutines.launch

private val ScannerYellow = Color(0xFFFBBF24)

@Composable
fun ScannerScreen(onBack: () -> Unit) {
    val context = LocalContext.current
    val repository = remember { QrAttendanceRepository(context.applicationContext) }
    val coroutineScope = rememberCoroutineScope()

    var qrToken by remember { mutableStateOf("") }
    var isCheckingIn by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    var checkInResult by remember { mutableStateOf<QrCheckInResponse?>(null) }
    var torchEnabled by remember { mutableStateOf(false) }
    var cameraPermissionGranted by remember {
        mutableStateOf(
            ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA) ==
                PackageManager.PERMISSION_GRANTED
        )
    }

    val permissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { granted ->
        cameraPermissionGranted = granted
        if (!granted) {
            errorMessage =
                "Camera permission was denied. You can still enter the attendance token manually."
        }
    }

    LaunchedEffect(Unit) {
        if (!cameraPermissionGranted) permissionLauncher.launch(Manifest.permission.CAMERA)
    }

    fun submitCheckIn(token: String) {
        val cleanToken = token.trim()
        if (cleanToken.isBlank() || isCheckingIn) return

        isCheckingIn = true
        errorMessage = null
        checkInResult = null

        coroutineScope.launch {
            repository.checkIn(cleanToken)
                .onSuccess {
                    checkInResult = it
                    qrToken = ""
                }
                .onFailure {
                    errorMessage = it.message ?: "Check-in failed."
                }
            isCheckingIn = false
        }
    }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.Black)
            .safeDrawingPadding()
    ) {
        if (cameraPermissionGranted) {
            QrCameraPreview(
                modifier = Modifier.fillMaxSize(),
                enabled = !isCheckingIn && checkInResult == null,
                torchEnabled = torchEnabled,
                onQrCodeDetected = { token ->
                    qrToken = token
                    submitCheckIn(token)
                }
            )
        } else {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(Color(0xFF171A19))
            )
        }

        // Use one even tint over the camera instead of separate top/side/bottom blocks.
        // The previous four-block overlay did not line up with the vertically-offset
        // scanner frame on all screen sizes, which created visible dark bars/lines.
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(Color.Black.copy(alpha = 0.14f))
        )

        ScannerTopBar(
            onBack = onBack,
            torchEnabled = torchEnabled,
            cameraPermissionGranted = cameraPermissionGranted,
            onToggleTorch = { torchEnabled = !torchEnabled }
        )

        Column(
            modifier = Modifier
                .align(Alignment.Center)
                .fillMaxWidth()
                .padding(horizontal = 28.dp)
                .offset(y = (-56).dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            ScannerFrame(scanning = cameraPermissionGranted && !isCheckingIn)

            Spacer(Modifier.height(28.dp))

            Text(
                text = if (cameraPermissionGranted) {
                    "Align the QR code within the frame to check in"
                } else {
                    "Camera access is unavailable"
                },
                color = Color.White,
                textAlign = TextAlign.Center,
                fontSize = 16.sp,
                fontWeight = FontWeight.Medium
            )

            Spacer(Modifier.height(8.dp))

            Text(
                text = when {
                    isCheckingIn -> "Checking attendance…"
                    !cameraPermissionGranted -> "Use the manual attendance code below"
                    else -> "Make sure the QR code is well-lit and in focus"
                },
                color = if (isCheckingIn) ScannerYellow else Color.White.copy(alpha = 0.70f),
                textAlign = TextAlign.Center,
                fontSize = 13.sp
            )
        }

        ManualCheckInPanel(
            modifier = Modifier.align(Alignment.BottomCenter),
            qrToken = qrToken,
            isCheckingIn = isCheckingIn,
            checkInResult = checkInResult,
            errorMessage = errorMessage,
            onTokenChange = {
                qrToken = it
                errorMessage = null
                checkInResult = null
            },
            onSubmit = { submitCheckIn(qrToken) }
        )
    }
}

@Composable
private fun ScannerTopBar(
    onBack: () -> Unit,
    torchEnabled: Boolean,
    cameraPermissionGranted: Boolean,
    onToggleTorch: () -> Unit
) {
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 10.dp, vertical = 12.dp)
    ) {
        Surface(
            modifier = Modifier.align(Alignment.CenterStart),
            color = Color.Black.copy(alpha = 0.34f),
            shape = RoundedCornerShape(12.dp)
        ) {
            IconButton(onClick = onBack) {
                Icon(
                    imageVector = Icons.Filled.ArrowBack,
                    contentDescription = "Back",
                    tint = Color.White
                )
            }
        }

        Text(
            modifier = Modifier.align(Alignment.Center),
            text = "Scan to Check In",
            color = Color.White,
            fontWeight = FontWeight.SemiBold,
            fontSize = 18.sp
        )

        Surface(
            modifier = Modifier.align(Alignment.CenterEnd),
            color = Color.Black.copy(alpha = 0.34f),
            shape = RoundedCornerShape(12.dp)
        ) {
            IconButton(
                enabled = cameraPermissionGranted,
                onClick = onToggleTorch
            ) {
                Icon(
                    imageVector = if (torchEnabled) Icons.Filled.FlashOn else Icons.Filled.FlashOff,
                    contentDescription = "Toggle flash",
                    tint = if (torchEnabled) ScannerYellow else Color.White
                )
            }
        }
    }
}

@Composable
private fun ScannerFrame(scanning: Boolean) {
    val transition = rememberInfiniteTransition(label = "scanner-line")
    val scanProgress by transition.animateFloat(
        initialValue = 0f,
        targetValue = 1f,
        animationSpec = infiniteRepeatable(
            animation = tween(durationMillis = 1900),
            repeatMode = RepeatMode.Reverse
        ),
        label = "scanner-line-progress"
    )

    Box(
        modifier = Modifier
            .size(280.dp)
            .clip(RoundedCornerShape(22.dp))
            .border(2.dp, ScannerYellow.copy(alpha = 0.72f), RoundedCornerShape(22.dp))
    ) {
        if (scanning) {
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(2.dp)
                    .offset(y = (274.dp * scanProgress))
                    .background(ScannerYellow.copy(alpha = 0.92f))
            )
        }

        Box(
            modifier = Modifier
                .size(34.dp)
                .align(Alignment.Center)
                .border(2.dp, ScannerYellow.copy(alpha = 0.48f), CircleShape)
        )

        ScannerCorner(Modifier.align(Alignment.TopStart), top = true, start = true)
        ScannerCorner(Modifier.align(Alignment.TopEnd), top = true, start = false)
        ScannerCorner(Modifier.align(Alignment.BottomStart), top = false, start = true)
        ScannerCorner(Modifier.align(Alignment.BottomEnd), top = false, start = false)
    }
}

@Composable
private fun ScannerCorner(
    modifier: Modifier,
    top: Boolean,
    start: Boolean
) {
    val color = ScannerYellow
    Box(
        modifier = modifier
            .size(48.dp)
            .drawBehind {
                val stroke = 8.dp.toPx()
                val length = 34.dp.toPx()
                val half = stroke / 2f

                val x = if (start) half else size.width - half
                val y = if (top) half else size.height - half
                val horizontalEnd = if (start) x + length else x - length
                val verticalEnd = if (top) y + length else y - length

                drawLine(
                    color = color,
                    start = Offset(x, y),
                    end = Offset(horizontalEnd, y),
                    strokeWidth = stroke,
                    cap = StrokeCap.Round
                )
                drawLine(
                    color = color,
                    start = Offset(x, y),
                    end = Offset(x, verticalEnd),
                    strokeWidth = stroke,
                    cap = StrokeCap.Round
                )
            }
    )
}

@Composable
private fun ManualCheckInPanel(
    modifier: Modifier,
    qrToken: String,
    isCheckingIn: Boolean,
    checkInResult: QrCheckInResponse?,
    errorMessage: String?,
    onTokenChange: (String) -> Unit,
    onSubmit: () -> Unit
) {
    Column(
        modifier = modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 14.dp),
        verticalArrangement = Arrangement.spacedBy(9.dp)
    ) {
        checkInResult?.let { result ->
            Surface(
                modifier = Modifier.fillMaxWidth(),
                color = Color.White,
                shape = RoundedCornerShape(14.dp)
            ) {
                Column(
                    modifier = Modifier.padding(15.dp),
                    verticalArrangement = Arrangement.spacedBy(3.dp)
                ) {
                    Text(
                        "Check-in successful",
                        color = AppColors.Green,
                        fontWeight = FontWeight.Bold,
                        fontSize = 17.sp
                    )
                    Text(result.playerName, color = AppColors.DarkText, fontWeight = FontWeight.SemiBold)
                    Text(result.eventTitle, color = AppColors.DarkText)
                    Text(
                        "${result.eventDate} • ${result.eventTime}",
                        color = AppColors.GreyText,
                        fontSize = 12.sp
                    )
                    Text(result.eventLocation, color = AppColors.GreyText, fontSize = 12.sp)
                }
            }
        }

        errorMessage?.let { message ->
            Surface(
                modifier = Modifier.fillMaxWidth(),
                color = Color(0xFF3B1717).copy(alpha = 0.94f),
                shape = RoundedCornerShape(12.dp)
            ) {
                Text(
                    modifier = Modifier.padding(12.dp),
                    text = message,
                    color = Color(0xFFFFB4AB),
                    textAlign = TextAlign.Center,
                    fontSize = 12.sp
                )
            }
        }

        Surface(
            modifier = Modifier.fillMaxWidth(),
            color = Color.Black.copy(alpha = 0.62f),
            shape = RoundedCornerShape(16.dp),
            border = androidx.compose.foundation.BorderStroke(
                1.dp,
                Color.White.copy(alpha = 0.18f)
            )
        ) {
            Column(
                modifier = Modifier.padding(14.dp),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                Text(
                    text = "Can't scan the QR code?",
                    color = Color.White,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 14.sp
                )
                Text(
                    text = "Enter the attendance token manually to check in.",
                    color = Color.White.copy(alpha = 0.70f),
                    fontSize = 12.sp
                )

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    OutlinedTextField(
                        modifier = Modifier.weight(1f),
                        value = qrToken,
                        onValueChange = onTokenChange,
                        placeholder = { Text("Attendance token") },
                        singleLine = true,
                        shape = RoundedCornerShape(10.dp),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedTextColor = Color.White,
                            unfocusedTextColor = Color.White,
                            cursorColor = ScannerYellow,
                            focusedPlaceholderColor = Color.White.copy(alpha = 0.48f),
                            unfocusedPlaceholderColor = Color.White.copy(alpha = 0.48f),
                            focusedBorderColor = ScannerYellow,
                            unfocusedBorderColor = Color.White.copy(alpha = 0.38f),
                            focusedContainerColor = Color.Black.copy(alpha = 0.28f),
                            unfocusedContainerColor = Color.Black.copy(alpha = 0.28f)
                        )
                    )

                    Spacer(Modifier.width(8.dp))

                    Button(
                        enabled = !isCheckingIn && qrToken.isNotBlank(),
                        onClick = onSubmit,
                        colors = ButtonDefaults.buttonColors(
                            containerColor = ScannerYellow,
                            contentColor = AppColors.DarkText,
                            disabledContainerColor = ScannerYellow.copy(alpha = 0.35f),
                            disabledContentColor = Color.White.copy(alpha = 0.50f)
                        ),
                        shape = RoundedCornerShape(10.dp)
                    ) {
                        if (isCheckingIn) {
                            CircularProgressIndicator(
                                modifier = Modifier.size(20.dp),
                                color = AppColors.DarkText,
                                strokeWidth = 2.dp
                            )
                        } else {
                            Text("Check In", fontWeight = FontWeight.Bold)
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun QrCameraPreview(
    modifier: Modifier = Modifier,
    enabled: Boolean,
    torchEnabled: Boolean,
    onQrCodeDetected: (String) -> Unit
) {
    val context = LocalContext.current
    val lifecycleOwner = context as LifecycleOwner
    val scanningEnabled by rememberUpdatedState(enabled)
    val currentOnDetected by rememberUpdatedState(onQrCodeDetected)
    val cameraExecutor = remember { Executors.newSingleThreadExecutor() }
    val barcodeScanner = remember {
        BarcodeScanning.getClient(
            BarcodeScannerOptions.Builder()
                .setBarcodeFormats(Barcode.FORMAT_QR_CODE)
                .build()
        )
    }
    var boundCamera by remember { mutableStateOf<Camera?>(null) }
    var lastToken by remember { mutableStateOf<String?>(null) }
    var lastScanAt by remember { mutableStateOf(0L) }

    LaunchedEffect(torchEnabled, boundCamera) {
        boundCamera?.cameraControl?.enableTorch(torchEnabled)
    }

    DisposableEffect(Unit) {
        onDispose {
            barcodeScanner.close()
            cameraExecutor.shutdown()
        }
    }

    AndroidView(
        modifier = modifier,
        factory = { previewContext ->
            val previewView = PreviewView(previewContext).apply {
                layoutParams = ViewGroup.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    ViewGroup.LayoutParams.MATCH_PARENT
                )
                implementationMode = PreviewView.ImplementationMode.COMPATIBLE
                scaleType = PreviewView.ScaleType.FILL_CENTER
            }
            val providerFuture = ProcessCameraProvider.getInstance(previewContext)

            providerFuture.addListener({
                val cameraProvider = providerFuture.get()
                val preview = Preview.Builder().build().also {
                    it.surfaceProvider = previewView.surfaceProvider
                }
                val analysis = ImageAnalysis.Builder()
                    .setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST)
                    .build()

                analysis.setAnalyzer(cameraExecutor) { imageProxy ->
                    analyzeQrImage(imageProxy, barcodeScanner) { token ->
                        val now = System.currentTimeMillis()
                        if (
                            scanningEnabled &&
                            (token != lastToken || now - lastScanAt > 3000)
                        ) {
                            lastToken = token
                            lastScanAt = now
                            previewView.post { currentOnDetected(token) }
                        }
                    }
                }

                cameraProvider.unbindAll()
                boundCamera = cameraProvider.bindToLifecycle(
                    lifecycleOwner,
                    CameraSelector.DEFAULT_BACK_CAMERA,
                    preview,
                    analysis
                )
            }, ContextCompat.getMainExecutor(previewContext))

            previewView
        }
    )
}

@androidx.annotation.OptIn(ExperimentalGetImage::class)
private fun analyzeQrImage(
    imageProxy: ImageProxy,
    scanner: BarcodeScanner,
    onDetected: (String) -> Unit
) {
    val mediaImage = imageProxy.image
    if (mediaImage == null) {
        imageProxy.close()
        return
    }

    val inputImage = InputImage.fromMediaImage(
        mediaImage,
        imageProxy.imageInfo.rotationDegrees
    )

    scanner.process(inputImage)
        .addOnSuccessListener { barcodes ->
            barcodes.firstNotNullOfOrNull { it.rawValue }
                ?.takeIf(String::isNotBlank)
                ?.let(onDetected)
        }
        .addOnCompleteListener { imageProxy.close() }
}
