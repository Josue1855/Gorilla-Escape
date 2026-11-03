import java.util.Properties
import java.io.FileInputStream

plugins {
    id("com.android.application")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

val keystoreProperties = Properties()
val keystorePropertiesFile = rootProject.file("key.properties")
if (keystorePropertiesFile.exists()) {
    keystoreProperties.load(FileInputStream(keystorePropertiesFile))
}

android {
    namespace = "com.gorillaescape.gorilla_companion"
    compileSdk = flutter.compileSdkVersion
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    defaultConfig {
        applicationId = "com.gorillaescape.gorilla_companion"
        minSdk = flutter.minSdkVersion
        targetSdk = flutter.targetSdkVersion
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    signingConfigs {
        create("release") {
            val keyPath = keystoreProperties.getProperty("storeFile") ?: System.getenv("GORILLA_KEYSTORE_FILE")
            if (keyPath != null) {
                val keyFile = file(keyPath)
                if (keyFile.exists()) {
                    storeFile = keyFile
                    storePassword = keystoreProperties.getProperty("storePassword") ?: System.getenv("GORILLA_KEYSTORE_PASSWORD")
                    keyAlias = keystoreProperties.getProperty("keyAlias") ?: System.getenv("GORILLA_KEY_ALIAS")
                    keyPassword = keystoreProperties.getProperty("keyPassword") ?: System.getenv("GORILLA_KEY_PASSWORD")
                }
            }
        }
    }

    buildTypes {
        release {
            signingConfig = signingConfigs.getByName("release")
        }
    }
}

kotlin {
    compilerOptions {
        jvmTarget = org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17
    }
}

flutter {
    source = "../.."
}
