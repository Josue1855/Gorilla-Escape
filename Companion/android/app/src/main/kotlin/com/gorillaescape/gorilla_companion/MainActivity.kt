package com.gorillaescape.gorilla_companion

import android.content.pm.PackageManager
import android.os.Build
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterActivity() {
    private var pendingLan: MethodChannel.Result? = null
    private val lanPermission = "android.permission.ACCESS_LOCAL_NETWORK"

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, "gorilla_escape/local_discovery")
            .setMethodCallHandler { call, result ->
                if (call.method != "requestLocalNetwork") {
                    result.notImplemented()
                } else if (Build.VERSION.SDK_INT < 37 || applicationInfo.targetSdkVersion < 37 ||
                    checkSelfPermission(lanPermission) == PackageManager.PERMISSION_GRANTED) {
                    result.success(true)
                } else if (pendingLan != null) {
                    result.success(false)
                } else {
                    pendingLan = result
                    requestPermissions(arrayOf(lanPermission), 8674)
                }
            }
    }

    override fun onRequestPermissionsResult(requestCode: Int, permissions: Array<out String>, grantResults: IntArray) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        if (requestCode == 8674) {
            pendingLan?.success(grantResults.isNotEmpty() && grantResults[0] == PackageManager.PERMISSION_GRANTED)
            pendingLan = null
        }
    }

    override fun onDestroy() {
        pendingLan?.success(false)
        pendingLan = null
        super.onDestroy()
    }
}
