package com.familylinklive.api.models

import com.familylinklive.api.db.Users
import org.jetbrains.exposed.sql.ResultRow

/** Single place that turns a Users row into the API's UserDto. */
fun ResultRow.toUserDto() = UserDto(
    id = this[Users.id],
    displayName = this[Users.displayName],
    email = this[Users.email],
    photoUrl = this[Users.photoUrl],
    language = this[Users.language],
    shareLocation = this[Users.shareLocation],
    biometricEnabled = this[Users.biometricEnabled]
)
