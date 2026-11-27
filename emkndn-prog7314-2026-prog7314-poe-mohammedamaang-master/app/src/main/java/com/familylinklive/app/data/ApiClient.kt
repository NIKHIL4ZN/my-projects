package com.familylinklive.app.data

import android.content.Context
import com.familylinklive.app.BuildConfig
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

/**
 * Builds a singleton Retrofit client pointed at BuildConfig.API_BASE_URL
 * (configured via local.properties, see README Step 3/6). Every request is
 * automatically tagged with the app's JWT once the user has logged in.
 */
object ApiClient {

    @Volatile private var service: ApiService? = null

    fun getService(context: Context): ApiService =
        service ?: synchronized(this) {
            service ?: build(context.applicationContext).also { service = it }
        }

    private fun build(context: Context): ApiService {
        val session = SessionManager.getInstance(context)

        /*
        Title: Interceptors - adding an Authorization header to every request
        Author(s): Square, Inc.
        Date: 2023
        Version: OkHttp 4.12.0
        Type: Documentation
        Availability: https://square.github.io/okhttp/features/interceptors/
        */
        val authInterceptor = Interceptor { chain ->
            val requestBuilder = chain.request().newBuilder()
            session.token?.let { requestBuilder.addHeader("Authorization", "Bearer $it") }
            chain.proceed(requestBuilder.build())
        }

        val logging = HttpLoggingInterceptor().apply {
            level = if (BuildConfig.DEBUG) HttpLoggingInterceptor.Level.BODY
                    else HttpLoggingInterceptor.Level.NONE
        }

        val client = OkHttpClient.Builder()
            .addInterceptor(authInterceptor)
            .authenticator(TokenAuthenticator(context))
            .addInterceptor(logging)
            .build()

        /*
        Title: Retrofit - a type-safe HTTP client for Android and Java
        Author(s): Square, Inc.
        Date: 2023
        Version: Retrofit 2.11.0
        Type: Documentation
        Availability: https://square.github.io/retrofit/
        */
        return Retrofit.Builder()
            .baseUrl(BuildConfig.API_BASE_URL)
            .client(client)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(ApiService::class.java)
    }
}
