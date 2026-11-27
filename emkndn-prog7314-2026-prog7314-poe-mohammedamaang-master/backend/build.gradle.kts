plugins {
    kotlin("jvm") version "1.9.24"
    kotlin("plugin.serialization") version "1.9.24"
    id("io.ktor.plugin") version "2.3.12"
    application
}

group = "com.familylinklive"
version = "3.0.0"

application {
    mainClass.set("com.familylinklive.api.ApplicationKt")
}

repositories {
    mavenCentral()
}

dependencies {
    implementation("io.ktor:ktor-server-core-jvm")
    implementation("io.ktor:ktor-server-netty-jvm")
    implementation("io.ktor:ktor-server-content-negotiation-jvm")
    implementation("io.ktor:ktor-serialization-kotlinx-json-jvm")
    implementation("io.ktor:ktor-server-cors-jvm")
    implementation("io.ktor:ktor-server-call-logging-jvm")
    implementation("io.ktor:ktor-server-status-pages-jvm")
    implementation("io.ktor:ktor-server-auth-jvm")
    implementation("io.ktor:ktor-server-auth-jwt-jvm")

    // Part 3: HTTP client used to upload profile photos to Azure Blob Storage
    implementation("io.ktor:ktor-client-core-jvm")
    implementation("io.ktor:ktor-client-cio-jvm")

    // Verifies Google/Firebase-issued ID tokens sent by the Android client.
    implementation("com.google.firebase:firebase-admin:9.3.0")

    // Issues our own application JWT after Firebase verification succeeds.
    implementation("com.auth0:java-jwt:4.4.0")

    // Persistence: Exposed ORM over a file-based H2 database.
    implementation("org.jetbrains.exposed:exposed-core:0.52.0")
    implementation("org.jetbrains.exposed:exposed-dao:0.52.0")
    implementation("org.jetbrains.exposed:exposed-jdbc:0.52.0")
    implementation("com.h2database:h2:2.2.224")
    // Part 3: optional persistent PostgreSQL (see DatabaseFactory.connectPostgres)
    implementation("org.postgresql:postgresql:42.7.3")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-core:1.8.1")

    implementation("ch.qos.logback:logback-classic:1.5.6")

    testImplementation("io.ktor:ktor-server-test-host-jvm")
    testImplementation(kotlin("test"))
}

kotlin {
    jvmToolchain(17)
}
