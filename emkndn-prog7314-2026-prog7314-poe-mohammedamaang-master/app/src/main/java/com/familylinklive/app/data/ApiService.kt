package com.familylinklive.app.data

import com.familylinklive.app.data.models.*
import okhttp3.RequestBody
import retrofit2.Response
import retrofit2.http.*

/**
 * Retrofit definition of the FamilyLink Live REST API (see backend/ module).
 * Matches the endpoint spec from the Part 1 Planning and Design document,
 * Section 5.2, reduced to what the Part 2 prototype implements end-to-end.
 */
interface ApiService {

    @POST("auth/login")
    suspend fun login(@Body request: LoginRequest): LoginResponse

    @PATCH("users/me/settings")
    suspend fun updateSettings(@Body request: SettingsRequest): UserDto

    @POST("location/update")
    suspend fun updateLocation(@Body request: LocationUpdateRequest): LocationUpdateResponse

    @GET("map/contacts")
    suspend fun getContactLocations(): ContactLocationsResponse

    @POST("places")
    suspend fun createPlace(@Body request: PlaceRequest): PlaceDto

    @GET("places")
    suspend fun getPlaces(): PlacesResponse

    @PATCH("places/{id}")
    suspend fun updatePlace(@Path("id") id: String, @Body request: PlaceRequest): PlaceDto

    @DELETE("places/{id}")
    suspend fun deletePlace(@Path("id") id: String): Response<Unit>

    @POST("chat/send")
    suspend fun sendMessage(@Body request: SendMessageRequest): MessageDto

    @GET("chat/messages")
    suspend fun getMessages(@Query("contactId") contactId: String): MessagesResponse

    @POST("contacts/invite")
    suspend fun inviteContact(@Body request: InviteRequest): Response<ContactDto>

    @GET("contacts")
    suspend fun getContacts(): ContactsResponse

    @POST("contacts/{id}/accept")
    suspend fun acceptContact(@Path("id") id: String): Response<Unit>

    @DELETE("contacts/{id}")
    suspend fun removeContact(@Path("id") id: String): Response<Unit>

    // ---- Part 3 ----

    @POST("users/me/fcm-token")
    suspend fun registerFcmToken(@Body request: FcmTokenRequest): Response<Unit>

    @DELETE("users/me/fcm-token")
    suspend fun unregisterFcmToken(@Query("token") token: String): Response<Unit>

    @POST("users/me/photo")
    suspend fun uploadPhoto(@Body body: RequestBody): UserDto

    @POST("sos/trigger")
    suspend fun triggerSos(@Body request: SosRequest): SosResponse

    @DELETE("users/me")
    suspend fun deleteAccount(): Response<Unit>
}
