package com.familylinklive.api

import io.ktor.client.request.*
import io.ktor.http.*
import io.ktor.server.testing.*
import kotlin.test.Test
import kotlin.test.assertEquals

/**
 * Smoke test run automatically by GitHub Actions (.github/workflows/backend-ci.yml)
 * on every push that touches backend/. Confirms the Ktor application module
 * wires up correctly and the server responds.
 */
class ApplicationTest {

    @Test
    fun `root endpoint responds with 200 OK`() = testApplication {
        application { module() }
        val response = client.get("/")
        assertEquals(HttpStatusCode.OK, response.status)
    }

    @Test
    fun `login without a token body is rejected`() = testApplication {
        application { module() }
        val response = client.post("/auth/login") {
            contentType(ContentType.Application.Json)
            setBody("{}")
        }
        assertEquals(HttpStatusCode.BadRequest, response.status)
    }

    @Test
    fun `protected route rejects requests without a JWT`() = testApplication {
        application { module() }
        val response = client.get("/places")
        assertEquals(HttpStatusCode.Unauthorized, response.status)
    }
}
