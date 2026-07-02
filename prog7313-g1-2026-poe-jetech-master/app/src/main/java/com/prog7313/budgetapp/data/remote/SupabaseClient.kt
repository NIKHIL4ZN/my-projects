package com.prog7313.budgetapp.data.remote

import android.util.Log
import com.prog7313.budgetapp.BuildConfig
import com.prog7313.budgetapp.data.model.*
import com.google.gson.ExclusionStrategy
import com.google.gson.FieldAttributes
import com.google.gson.FieldNamingPolicy
import com.google.gson.GsonBuilder
import com.google.gson.annotations.SerializedName
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Response
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.http.*
import java.util.concurrent.TimeUnit

private const val TAG = "SupabaseClient"

// ─────────────────────────────────────────────────────────────────────────────
//  Session — stores the JWT after login
// ─────────────────────────────────────────────────────────────────────────────

object SupabaseSession {
    var accessToken: String = ""
    var userId: String = ""
    var userEmail: String = ""
    val isLoggedIn: Boolean get() = accessToken.isNotEmpty()
    fun clear() { accessToken = ""; userId = ""; userEmail = "" }
}

/*
Title: Android MVVM — Session management without SDK
Author(s): Philipp Lackner
Date: 2023
Version: N/A
Type: Online Tutorial
Availability: https://www.youtube.com/c/PhilippLackner
*/

// ─────────────────────────────────────────────────────────────────────────────
//  Auth DTOs
// ─────────────────────────────────────────────────────────────────────────────

data class AuthRequest(val email: String, val password: String)

data class AuthResponse(
    @SerializedName("access_token")  val accessToken: String? = null,
    @SerializedName("refresh_token") val refreshToken: String? = null,
    val user: UserDto? = null,
    val error: String? = null,
    @SerializedName("error_description") val errorDescription: String? = null
)

data class UserDto(
    val id: String = "",
    val email: String = ""
)

/*
Title: Supabase Auth REST API reference
Author(s): Supabase
Date: 2024
Version: v1
Type: API Documentation
Availability: https://supabase.com/docs/reference/javascript/auth-signup
*/

// ─────────────────────────────────────────────────────────────────────────────
//  Partial-update request bodies
// ─────────────────────────────────────────────────────────────────────────────

data class SavingsAmountUpdate(val currentAmount: Double)
data class ActiveUpdate(val isActive: Boolean)

/*
Title: Supabase PostgREST — PATCH (partial update)
Author(s): Supabase
Date: 2024
Version: v1
Type: API Documentation
Availability: https://supabase.com/docs/guides/api/rest/using-filters
*/

// ─────────────────────────────────────────────────────────────────────────────
//  Retrofit service — Auth endpoints
// ─────────────────────────────────────────────────────────────────────────────

interface SupabaseAuthApi {
    @POST("auth/v1/signup")
    suspend fun signUp(@Body body: AuthRequest): Response<AuthResponse>

    @POST("auth/v1/token")
    suspend fun signIn(
        @Query("grant_type") grantType: String = "password",
        @Body body: AuthRequest
    ): Response<AuthResponse>

    @POST("auth/v1/logout")
    suspend fun signOut(): Response<Void>
}

/*
Title: Retrofit — A type-safe HTTP client for Android and Java
Author(s): Square Inc.
Date: 2024
Version: 2.11.0
Type: Library Documentation
Availability: https://square.github.io/retrofit/
*/

// ─────────────────────────────────────────────────────────────────────────────
//  Retrofit service — PostgREST database endpoints
// ─────────────────────────────────────────────────────────────────────────────

interface SupabaseDbApi {

    // ── Categories ─────────────────────────────────────────────────────────────
    @GET("rest/v1/categories")
    suspend fun getCategories(
        @Query("user_id") userIdFilter: String,
        @Query("order")   order: String = "name.asc"
    ): Response<List<Category>>

    @POST("rest/v1/categories")
    suspend fun createCategory(
        @Body body: Category,
        @Header("Prefer") prefer: String = "return=representation"
    ): Response<List<Category>>

    @DELETE("rest/v1/categories")
    suspend fun deleteCategory(
        @Query("id")      idFilter: String,
        @Query("user_id") userIdFilter: String
    ): Response<Void>

    // ── Expenses ────────────────────────────────────────────────────────────────
    @GET("rest/v1/expenses")
    suspend fun getExpenses(
        @Query("user_id") userIdFilter: String,
        @Query("date")    dateFilters: List<String>? = null,
        @Query("order")   order: String = "date.desc"
    ): Response<List<Expense>>

    @POST("rest/v1/expenses")
    suspend fun createExpense(
        @Body body: Expense,
        @Header("Prefer") prefer: String = "return=representation"
    ): Response<List<Expense>>

    @PATCH("rest/v1/expenses")
    suspend fun updateExpense(
        @Query("id")      idFilter: String,
        @Body body: Expense,
        @Header("Prefer") prefer: String = "return=representation"
    ): Response<List<Expense>>

    @DELETE("rest/v1/expenses")
    suspend fun deleteExpense(
        @Query("id")      idFilter: String,
        @Query("user_id") userIdFilter: String
    ): Response<Void>

    // ── Budget goals ────────────────────────────────────────────────────────────
    @GET("rest/v1/budget_goals")
    suspend fun getBudgetGoals(
        @Query("user_id") userIdFilter: String,
        @Query("month")   monthFilter: String? = null,
        @Query("year")    yearFilter: String? = null
    ): Response<List<BudgetGoal>>

    @POST("rest/v1/budget_goals")
    suspend fun upsertBudgetGoal(
        @Body body: BudgetGoal,
        // on_conflict tells PostgREST which UNIQUE constraint to match on.
        // Without this parameter, PostgREST attempts a plain INSERT and
        // fails with a 409 Conflict when a row for the same month/year exists.
        @Query("on_conflict") onConflict: String = "user_id,month,year",
        @Header("Prefer") prefer: String = "return=representation,resolution=merge-duplicates"
    ): Response<List<BudgetGoal>>

    // ── Savings goals ────────────────────────────────────────────────────────────
    @GET("rest/v1/savings_goals")
    suspend fun getSavingsGoals(
        @Query("user_id") userIdFilter: String,
        @Query("order")   order: String = "created_at.desc"
    ): Response<List<SavingsGoal>>

    @POST("rest/v1/savings_goals")
    suspend fun createSavingsGoal(
        @Body body: SavingsGoal,
        @Header("Prefer") prefer: String = "return=representation"
    ): Response<List<SavingsGoal>>

    @PATCH("rest/v1/savings_goals")
    suspend fun updateSavingsGoal(
        @Query("id")      idFilter: String,
        @Query("user_id") userIdFilter: String,
        @Body body: SavingsAmountUpdate,
        @Header("Prefer") prefer: String = "return=representation"
    ): Response<List<SavingsGoal>>

    @DELETE("rest/v1/savings_goals")
    suspend fun deleteSavingsGoal(
        @Query("id")      idFilter: String,
        @Query("user_id") userIdFilter: String
    ): Response<Void>

    // ── Recurring transactions ────────────────────────────────────────────────────
    @GET("rest/v1/recurring_transactions")
    suspend fun getRecurringTransactions(
        @Query("user_id") userIdFilter: String,
        @Query("order")   order: String = "name.asc"
    ): Response<List<RecurringTransaction>>

    @POST("rest/v1/recurring_transactions")
    suspend fun createRecurring(
        @Body body: RecurringTransaction,
        @Header("Prefer") prefer: String = "return=representation"
    ): Response<List<RecurringTransaction>>

    @PATCH("rest/v1/recurring_transactions")
    suspend fun updateRecurring(
        @Query("id")      idFilter: String,
        @Query("user_id") userIdFilter: String,
        @Body body: ActiveUpdate,
        @Header("Prefer") prefer: String = "return=representation"
    ): Response<List<RecurringTransaction>>

    @DELETE("rest/v1/recurring_transactions")
    suspend fun deleteRecurring(
        @Query("id")      idFilter: String,
        @Query("user_id") userIdFilter: String
    ): Response<Void>
}

/*
Title: Supabase PostgREST API — Filtering, ordering and CRUD
Author(s): Supabase
Date: 2024
Version: v1
Type: API Documentation
Availability: https://supabase.com/docs/guides/api/rest/using-filters
*/

/*
Title: OkHttp — Interceptors
Author(s): Square Inc.
Date: 2024
Version: 4.12.0
Type: Library Documentation
Availability: https://square.github.io/okhttp/features/interceptors/
*/

// ─────────────────────────────────────────────────────────────────────────────
//  Gson — automatic snake_case field naming
// ─────────────────────────────────────────────────────────────────────────────

private val gson by lazy {
    GsonBuilder()
        .setFieldNamingPolicy(FieldNamingPolicy.LOWER_CASE_WITH_UNDERSCORES)
        .addSerializationExclusionStrategy(object : ExclusionStrategy {
            private val skip = setOf("id", "createdAt")
            override fun shouldSkipField(f: FieldAttributes) = f.name in skip
            override fun shouldSkipClass(clazz: Class<*>) = false
        })
        .create()
}

/*
Title: Gson — FieldNamingPolicy and ExclusionStrategy
Author(s): Google
Date: 2024
Version: 2.10.1
Type: Library Documentation
Availability: https://github.com/google/gson/blob/main/UserGuide.md
*/

// ─────────────────────────────────────────────────────────────────────────────
//  OkHttp client — injects apikey + Bearer token on every request
// ─────────────────────────────────────────────────────────────────────────────

val sharedOkHttpClient: OkHttpClient by lazy {
    OkHttpClient.Builder()
        .addInterceptor { chain ->
            val token = SupabaseSession.accessToken.ifEmpty { BuildConfig.SUPABASE_ANON_KEY }
            val req = chain.request().newBuilder()
                .addHeader("apikey",        BuildConfig.SUPABASE_ANON_KEY)
                .addHeader("Authorization", "Bearer $token")
                .addHeader("Content-Type",  "application/json")
                .build()
            chain.proceed(req)
        }
        .addInterceptor(HttpLoggingInterceptor().apply {
            level = HttpLoggingInterceptor.Level.BASIC
        })
        .connectTimeout(30, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .build()
}

/*
Title: OkHttp — Adding headers to every request using an interceptor
Author(s): Square Inc.
Date: 2024
Version: 4.12.0
Type: Library Documentation
Availability: https://square.github.io/okhttp/recipes/#adding-headers-to-every-request-kt
*/

// ─────────────────────────────────────────────────────────────────────────────
//  Retrofit singleton
// ─────────────────────────────────────────────────────────────────────────────

private val retrofit: Retrofit by lazy {
    Retrofit.Builder()
        .baseUrl("${BuildConfig.SUPABASE_URL}/")
        .client(sharedOkHttpClient)
        .addConverterFactory(GsonConverterFactory.create(gson))
        .build()
}

/*
Title: Retrofit — Singleton pattern for shared HTTP client
Author(s): Vlad Sonkin
Date: 2022
Version: N/A
Type: Online Article
Availability: https://vladsonkin.com/android-networking-with-retrofit/
*/

val supabaseAuthApi: SupabaseAuthApi by lazy { retrofit.create(SupabaseAuthApi::class.java) }
val supabaseDbApi:   SupabaseDbApi   by lazy { retrofit.create(SupabaseDbApi::class.java) }

// ─────────────────────────────────────────────────────────────────────────────
//  Storage — OkHttp direct binary upload
// ─────────────────────────────────────────────────────────────────────────────

suspend fun uploadToStorage(bucket: String, path: String, imageBytes: ByteArray): Result<String> {
    return withContext(Dispatchers.IO) {
        try {
            val url = "${BuildConfig.SUPABASE_URL}/storage/v1/object/$bucket/$path"
            val body = imageBytes.toRequestBody("image/jpeg".toMediaType())
            val request = Request.Builder()
                .url(url)
                .addHeader("apikey", BuildConfig.SUPABASE_ANON_KEY)
                .addHeader("Authorization", "Bearer ${SupabaseSession.accessToken}")
                .addHeader("x-upsert", "true")
                .post(body)
                .build()
            val response = sharedOkHttpClient.newCall(request).execute()
            if (response.isSuccessful) {
                val publicUrl = "${BuildConfig.SUPABASE_URL}/storage/v1/object/public/$bucket/$path"
                Log.i(TAG, "Uploaded: $publicUrl")
                Result.success(publicUrl)
            } else {
                Result.failure(Exception("Upload failed: ${response.code} ${response.message}"))
            }
        } catch (e: Exception) {
            Log.e(TAG, "uploadToStorage failed", e)
            Result.failure(e)
        }
    }
}

/*
Title: Supabase Storage REST API — Upload object
Author(s): Supabase
Date: 2024
Version: v1
Type: API Documentation
Availability: https://supabase.com/docs/reference/javascript/storage-from-upload
*/

/*
Title: Kotlin Coroutines — withContext and Dispatchers.IO
Author(s): JetBrains
Date: 2024
Version: 1.8.1
Type: Documentation
Availability: https://kotlinlang.org/docs/coroutines-guide.html
*/