package com.paravolley.mobile.screens

import android.Manifest
import android.content.pm.PackageManager
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
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.FlashOff
import androidx.compose.material.icons.filled.FlashOn
import androidx.compose.material.icons.filled.QrCodeScanner
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
    val repository = remember { QrAttendanceRepository(context.applicationContext) }
    val coroutineScope = rememberCoroutineScope()

    var qrToken by remember { mutableStateOf("") }
    var isCheckingIn by remember { mutableStateOf(false) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    var checkInResult by remember { mutableStateOf<QrCheckInResponse?>(null) }
    var torchEnabled by remember { mutableStateOf(false) }
    var cameraPermissionGranted by remember {
        mutableStateOf(
            ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED
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
                    .background(Color(0xFF161B19))
            )
        }

        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(Color.Black.copy(alpha = 0.35f))
        )

        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 8.dp, vertical = 8.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Surface(color = Color.Black.copy(alpha = 0.35f), shape = CircleShape) {
                IconButton(onClick = onBack) {
                    Icon(Icons.Filled.ArrowBack, contentDescription = "Back", tint = Color.White)
                }
            }
            Text(
                text = "Scan to Check In",
                color = Color.White,
                fontWeight = FontWeight.SemiBold,
                fontSize = 18.sp
            )
            Surface(color = Color.Black.copy(alpha = 0.35f), shape = CircleShape) {
                IconButton(
                    enabled = cameraPermissionGranted,
                    onClick = { torchEnabled = !torchEnabled }
                ) {
                    Icon(
                        imageVector = if (torchEnabled) Icons.Filled.FlashOn else Icons.Filled.FlashOff,
                        contentDescription = "Toggle flash",
                        tint = if (torchEnabled) AppColors.Yellow else Color.White
                    )
                }
            }
        }

        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(horizontal = 24.dp, vertical = 72.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            Box(
                modifier = Modifier
                    .size(280.dp)
                    .clip(RoundedCornerShape(22.dp))
                    .border(4.dp, AppColors.Yellow, RoundedCornerShape(22.dp)),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Filled.QrCodeScanner,
                    contentDescription = null,
                    tint = AppColors.Yellow.copy(alpha = 0.35f),
                    modifier = Modifier.size(42.dp)
                )
            }
            Spacer(Modifier.height(22.dp))
            Text(
                text = if (cameraPermissionGranted) {
                    "Align the QR code within the frame to check in"
                } else {
                    "Camera access is unavailable. Enter the attendance token below."
                },
                color = Color.White,
                textAlign = TextAlign.Center,
                fontSize = 15.sp
            )
            Spacer(Modifier.height(8.dp))
            Text(
                text = if (isCheckingIn) "Checking attendance…" else "Make sure the QR code is clear and well lit",
                color = if (isCheckingIn) AppColors.Yellow else Color.White.copy(alpha = 0.65f),
                textAlign = TextAlign.Center,
                fontSize = 12.sp
            )
        }

        Column(
            modifier = Modifier
                .align(Alignment.BottomCenter)
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
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
                        Text("Check-in successful", color = AppColors.Green, fontWeight = FontWeight.Bold, fontSize = 17.sp)
                        Text(result.playerName, color = AppColors.DarkText, fontWeight = FontWeight.SemiBold)
                        Text(result.eventTitle, color = AppColors.DarkText)
                        Text("${result.eventDate} • ${result.eventTime}", color = AppColors.GreyText, fontSize = 12.sp)
                        Text(result.eventLocation, color = AppColors.GreyText, fontSize = 12.sp)
                    }
                }
            }

            errorMessage?.let { message ->
                Surface(
                    modifier = Modifier.fillMaxWidth(),
                    color = Color(0xFF3B1717).copy(alpha = 0.92f),
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

            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                OutlinedTextField(
                    modifier = Modifier.weight(1f),
                    value = qrToken,
                    onValueChange = {
                        qrToken = it
                        errorMessage = null
                        checkInResult = null
                    },
                    label = { Text("Manual token") },
                    singleLine = true,
                    shape = RoundedCornerShape(10.dp),
                    colors = OutlinedTextFieldDefaults.colors(
                        focusedTextColor = Color.White,
                        unfocusedTextColor = Color.White,
                        cursorColor = AppColors.Yellow,
                        focusedLabelColor = AppColors.Yellow,
                        unfocusedLabelColor = Color.White.copy(alpha = 0.7f),
                        focusedBorderColor = AppColors.Yellow,
                        unfocusedBorderColor = Color.White.copy(alpha = 0.45f),
                        focusedContainerColor = Color.Black.copy(alpha = 0.45f),
                        unfocusedContainerColor = Color.Black.copy(alpha = 0.45f)
                    )
                )
                Spacer(Modifier.width(8.dp))
                Button(
                    enabled = !isCheckingIn && qrToken.isNotBlank(),
                    onClick = { submitCheckIn(qrToken) },
                    colors = ButtonDefaults.buttonColors(
                        containerColor = AppColors.Yellow,
                        contentColor = AppColors.DarkText
                    ),
                    shape = RoundedCornerShape(10.dp)
                ) {
                    if (isCheckingIn) {
                        CircularProgressIndicator(modifier = Modifier.size(20.dp), color = AppColors.DarkText, strokeWidth = 2.dp)
                    } else {
                        Text("Check In", fontWeight = FontWeight.Bold)
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
                        if (scanningEnabled && (token != lastToken || now - lastScanAt > 3000)) {
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

    val inputImage = InputImage.fromMediaImage(mediaImage, imageProxy.imageInfo.rotationDegrees)
    scanner.process(inputImage)
        .addOnSuccessListener { barcodes ->
            barcodes.firstNotNullOfOrNull { it.rawValue }
                ?.takeIf(String::isNotBlank)
                ?.let(onDetected)
        }
        .addOnCompleteListener { imageProxy.close() }
}
