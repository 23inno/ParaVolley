package com.paravolley.mobile.screens

import android.Manifest
import android.content.pm.PackageManager
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.camera.core.CameraSelector
import androidx.camera.core.ExperimentalGetImage
import androidx.camera.core.ImageAnalysis
import androidx.camera.core.ImageProxy
import androidx.camera.core.Preview
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberUpdatedState
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
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

@Composable
fun ScannerScreen(onBack: () -> Unit) {
    val context = LocalContext.current
    val repository = remember {
        QrAttendanceRepository(context.applicationContext)
    }
    val coroutineScope = rememberCoroutineScope()
    var qrToken by remember { mutableStateOf("") }
    var isCheckingIn by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    var checkInResult by remember { mutableStateOf<QrCheckInResponse?>(null) }
    var cameraPermissionGranted by remember {
        mutableStateOf(
            ContextCompat.checkSelfPermission(
                context,
                Manifest.permission.CAMERA
            ) == PackageManager.PERMISSION_GRANTED
        )
    }

    val permissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { granted ->
        cameraPermissionGranted = granted
        if (!granted) {
            errorMessage = "Camera permission was denied. You can still enter the QR token manually."
        }
    }

    LaunchedEffect(Unit) {
        if (!cameraPermissionGranted) {
            permissionLauncher.launch(Manifest.permission.CAMERA)
        }
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
            .background(Color(0xFF111714))
            .safeDrawingPadding()
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(20.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            Button(
                modifier = Modifier.align(Alignment.Start),
                onClick = onBack,
                colors = ButtonDefaults.buttonColors(
                    containerColor = AppColors.Yellow,
                    contentColor = AppColors.DarkText
                )
            ) { Text("Back") }

            Text(
                text = "QR Attendance",
                color = Color.White,
                fontWeight = FontWeight.Bold,
                fontSize = 26.sp
            )

            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(280.dp)
                    .border(5.dp, AppColors.Yellow),
                contentAlignment = Alignment.Center
            ) {
                if (cameraPermissionGranted) {
                    QrCameraPreview(
                        enabled = !isCheckingIn && checkInResult == null,
                        onQrCodeDetected = { detectedToken ->
                            qrToken = detectedToken
                            submitCheckIn(detectedToken)
                        }
                    )
                } else {
                    Text(
                        text = "Camera access is unavailable.\nUse manual token entry below.",
                        color = Color.White,
                        textAlign = TextAlign.Center
                    )
                }
            }

            Text(
                text = "Point the camera at an active attendance QR code, or enter its token manually.",
                color = Color.White,
                textAlign = TextAlign.Center
            )

            OutlinedTextField(
                modifier = Modifier.fillMaxWidth(),
                value = qrToken,
                onValueChange = {
                    qrToken = it
                    errorMessage = null
                    checkInResult = null
                },
                label = { Text("QR attendance token") },
                singleLine = true,
                colors = OutlinedTextFieldDefaults.colors(
                    focusedTextColor = Color.White,
                    unfocusedTextColor = Color.White,
                    cursorColor = AppColors.Yellow,
                    focusedLabelColor = AppColors.Yellow,
                    unfocusedLabelColor = Color.White,
                    focusedBorderColor = AppColors.Yellow,
                    unfocusedBorderColor = Color.White
                )
            )

            Button(
                modifier = Modifier.fillMaxWidth(),
                enabled = !isCheckingIn && qrToken.isNotBlank(),
                onClick = { submitCheckIn(qrToken) },
                colors = ButtonDefaults.buttonColors(
                    containerColor = AppColors.Yellow,
                    contentColor = AppColors.DarkText
                )
            ) {
                if (isCheckingIn) {
                    CircularProgressIndicator()
                } else {
                    Text("Check In", fontWeight = FontWeight.Bold)
                }
            }

            errorMessage?.let {
                Text(
                    text = it,
                    color = Color(0xFFFF8A80),
                    textAlign = TextAlign.Center,
                    fontWeight = FontWeight.Medium
                )
            }

            checkInResult?.let { result ->
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .background(Color.White)
                        .padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(5.dp)
                ) {
                    Text(
                        "Check-in successful",
                        color = AppColors.DarkGreen,
                        fontWeight = FontWeight.Bold,
                        fontSize = 18.sp
                    )
                    Text(result.playerName, fontWeight = FontWeight.Medium)
                    Text(result.eventTitle)
                    Text("${result.eventDate} • ${result.eventTime}")
                    Text(result.eventLocation)
                    Text(
                        "Attendance: ${result.status}",
                        color = AppColors.Green,
                        fontWeight = FontWeight.Bold
                    )
                }
            }
        }
    }
}

@Composable
private fun QrCameraPreview(
    enabled: Boolean,
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
    var lastToken by remember { mutableStateOf<String?>(null) }
    var lastScanAt by remember { mutableStateOf(0L) }

    DisposableEffect(Unit) {
        onDispose {
            barcodeScanner.close()
            cameraExecutor.shutdown()
        }
    }

    AndroidView(
        modifier = Modifier.fillMaxSize(),
        factory = { previewContext ->
            val previewView = PreviewView(previewContext)
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
                    analyzeQrImage(
                        imageProxy,
                        barcodeScanner
                    ) { token ->
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
                cameraProvider.bindToLifecycle(
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
