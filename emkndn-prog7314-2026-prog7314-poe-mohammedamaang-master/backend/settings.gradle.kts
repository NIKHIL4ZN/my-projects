plugins {
    // Lets Gradle automatically download a matching JDK (here, 17) when
    // no locally installed toolchain satisfies build.gradle.kts's
    // `kotlin { jvmToolchain(17) }` requirement - fixes
    // "NoToolchainAvailableException" without needing a system-wide
    // Java install at all.
    id("org.gradle.toolchains.foojay-resolver-convention") version "0.8.0"
}

rootProject.name = "familylink-api"
