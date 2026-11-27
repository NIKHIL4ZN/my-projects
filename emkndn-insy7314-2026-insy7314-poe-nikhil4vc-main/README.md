## JETECH Group
# HustleHub+

HustleHub+ is a secure full-stack marketplace application designed to connect clients with freelancers who offer skills and services.

The application is implemented using a MERN-based architecture consisting of a React frontend, Node.js/Express backend and MongoDB database.

Part 1 established the secure backend foundation of the application, including authentication, JWT security, password hashing, validation, HTTPS, rate limiting and role-based authorisation.

Part 2 expanded the system into a functional service marketplace. Freelancers can create and manage gigs, while clients can browse services and submit booking requests. The completed application adds freelancer acceptance and rejection of requests, transaction recording after acceptance, completed-income reporting and estimated tax calculations.

The final Portfolio of Evidence (POE) version also includes automated backend and frontend testing, browser-based Playwright end-to-end testing, Docker containerisation, ESLint static analysis and a GitHub Actions continuous integration (CI) workflow triggered automatically by pushes and pull requests to `main`, with optional manual execution. All four CI jobs passed in the verified push-triggered run. The final verified test results were 52 Jest, 58 Vitest and 6 Playwright tests (116 total).

**Part 1 Demo:**  

https://youtu.be/svdTfxsZrgs

**Part 2 Demo:**  

https://www.youtube.com/watch?v=AxKjbuH51vk

**Part 3 / Final POE Demo:**

https://www.youtube.com/watch?v=AeMAHIQ4aOM

---

# 1. System Overview

## Purpose

HustleHub+ provides a secure online marketplace where freelancers can advertise services and clients can browse and book those services.

The system includes authentication, role-based access control, gig management, booking request approval, transaction recording, freelancer income tracking and an estimated income tax calculation.

## User Roles

HustleHub+ supports three roles:

### Client

Clients can:

- Register and log in
- Browse available gigs
- View gig information
- Submit booking requests
- View their own bookings and booking statuses
- See when they have already booked a gig
- Access only client-authorised functionality

### Freelancer

Freelancers can:

- Register and log in
- Access a freelancer dashboard
- Create gigs
- View their own gigs
- Edit gigs
- Delete gigs
- View bookings made against their gigs
- Accept or reject pending booking requests
- View completed-transaction income and estimated tax information

### Administrator

The application also includes an administrator role for privileged user and role management, including the freelancer promotion workflow documented in the Part 2 API tests. Administrator functions are protected by backend authorisation.

---

# 2. Core Application Features

The final POE application provides the following functionality:

- User registration
- User login
- JWT authentication
- Role-based access control
- Client and freelancer interfaces
- MongoDB database storage
- Gig creation
- Gig viewing
- Gig editing
- Gig deletion
- Marketplace browsing
- Pending booking request creation
- Client booking history and status tracking
- Freelancer booking acceptance and rejection
- Transaction creation following booking acceptance
- Income reporting based on completed transactions
- Estimated income tax calculation (15%)
- Duplicate active-booking prevention
- HTTPS communication
- Request validation
- Password hashing
- Rate limiting
- Helmet security headers
- Content Security Policy
- CORS configuration
- Controlled error handling
- Backend automated tests
- Frontend automated tests
- Postman/Newman API testing
- Playwright end-to-end browser testing
- ESLint static analysis
- Docker and Docker Compose
- GitHub Actions continuous integration

---

# 3. Technology Stack

## Frontend

- React
- Vite
- React Router
- Axios
- JavaScript
- HTML
- CSS

## Backend

- Node.js
- Express.js
- JavaScript
- CommonJS modules

## Database

- MongoDB
- MongoDB Atlas
- Mongoose

## Security

- `bcryptjs`
- `jsonwebtoken`
- `helmet`
- `express-rate-limit`
- `express-validator`
- HTTPS/TLS
- Content Security Policy
- CORS
- JWT-based authentication
- Role-based authorisation

## Testing

- Jest
- Supertest
- Vitest
- React Testing Library
- Postman
- Newman
- Playwright (Chromium)
- ESLint

## Containerisation and CI

- Docker
- Docker Compose
- GitHub Actions

## Development Tools

- Visual Studio Code
- Nodemon
- MongoDB Atlas
- Postman
- OpenSSL
- Git
- GitHub

---

# 4. System Architecture
<img width="898" height="771" alt="Screenshot 2026-09-01 134035" src="https://github.com/user-attachments/assets/40d354db-0274-47af-83e8-b863785c35f9" />
<img width="901" height="696" alt="Screenshot 2026-09-01 134054" src="https://github.com/user-attachments/assets/c517e828-db11-4d52-a763-6ad05accf651" />

HustleHub+ follows a client-server architecture.

```text
                        React Frontend
                              |
                              | HTTPS
                              v
                    +-------------------+
                    | Express REST API  |
                    +-------------------+
                              |
              +---------------+---------------+
              |               |               |
              v               v               v
          Helmet/CSP     Rate Limiting     CORS
                              |
                              v
                         API Routes
                              |
                              v
                     Input Validation
                              |
                              v
                   JWT Authentication
                              |
                              v
                    Role Authorisation
                              |
                              v
                         Controllers
                              |
                              v
                          Mongoose
                              |
                              v
                          MongoDB
```

The React frontend communicates with the Express backend through HTTPS API requests.

The Express backend performs security checks before requests reach the application controllers.

Persistent application data is stored in MongoDB.

---

# 5. Project Structure

```text
HustleHub/
│
├── backend/
│   ├── certificates/
│   ├── data/
│   ├── src/
│   │   ├── config/
│   │   ├── controllers/
│   │   ├── middleware/
│   │   ├── models/
│   │   ├── routes/
│   │   ├── validators/
│   │   └── app.js
│   ├── tests/
│   ├── .env.example
│   ├── nodemon.json
│   ├── package.json
│   └── server.js
│
├── frontend/
│   ├── public/
│   ├── src/
│   │   ├── api/
│   │   ├── components/
│   │   ├── context/
│   │   ├── pages/
│   │   │   ├── client/
│   │   │   └── freelancer/
│   │   ├── App.jsx
│   │   └── main.jsx
│   ├── e2e/
│   ├── playwright.config.js
│   ├── package.json
│   └── vite.config.js
│
├── postman/
│   └── HustleHub-Part2.postman_collection.json
│
├── docs/
├── .github/
│   └── workflows/
│       └── ci.yml
├── docker-compose.yml
├── .gitignore
└── README.md
```

---

# 6. Backend Architecture

## `server.js`

The backend entry point.

It:

- Loads environment variables
- Connects to MongoDB
- Loads the HTTPS certificate
- Creates the HTTPS server
- Starts the API server

The development API runs on:

```text
https://localhost:5000
```

## `src/app.js`

Configures the Express application.

It includes:

- Helmet
- Content Security Policy
- CORS
- JSON parsing
- General rate limiting
- API routes
- Health endpoint
- 404 handling
- Global error handling

## `src/controllers/`

Contains application logic for:

- Authentication
- Gig management
- Booking management
- User/role management

## `src/models/`

Contains Mongoose models for:

- User
- Gig
- Booking
- Transaction

## `src/middleware/`

Contains reusable middleware for:

- JWT authentication
- Role-based authorisation
- Request validation

## `src/validators/`

Contains validation rules for incoming API requests.

## `src/config/`

Contains MongoDB and environment configuration.

---

# 7. Frontend Architecture

The frontend is implemented using React and Vite.

React Router is used to provide separate pages for authenticated client and freelancer functionality.

Important frontend areas include:

```text
src/
├── api/
│   └── api.js
├── components/
│   ├── Navbar
│   ├── ProtectedRoute
│   └── RoleRoute
├── context/
│   └── authentication context
└── pages/
    ├── client/
    └── freelancer/
```

## Authentication State

Authentication tokens are stored using:

```javascript
sessionStorage
```

This allows separate browser tabs to maintain separate authenticated sessions which allows multiple users to login at the same time.

Axios automatically includes the JWT with authenticated API requests.

---

# 8. Authentication

HustleHub+ uses JWT-based authentication.

## Registration

```http
POST /api/auth/register
```

Example:

```json
{
  "name": "John Smith",
  "email": "john.smith@example.com",
  "password": "SecurePass123!"
}
```

Successful registration creates an account.

Passwords are never returned in API responses.

## Login

```http
POST /api/auth/login
```

Example:

```json
{
  "email": "john.smith@example.com",
  "password": "SecurePass123!"
}
```

Successful login returns a JWT and user information.

## Current User

```http
GET /api/auth/me
```

Requires:

```http
Authorization: Bearer <JWT>
```

Requests without a valid JWT are rejected with:

```text
401 Unauthorized
```

---

# 9. JWT Security

JWTs contain authenticated user information including:

- User ID
- Email
- Role

Example payload of token:

```json
{
  "sub": "user-id",
  "email": "john.smith@example.com",
  "role": "client"
}
```

The signing secret is stored in an environment variable rather than source code.

Tokens expire after the configured period.

The default expiration period is:

```text
1h
```

Protected requests are verified before access is permitted.

---

# 10. Password Security

Passwords are protected using `bcryptjs`.

The application uses a bcrypt cost factor of:

```text
12
```

Registration flow:

```text
Plain-text Password
        |
        v
   bcrypt.hash()
        |
        v
    Password Hash
        |
        v
      MongoDB
```

During authentication:

```javascript
bcrypt.compare(password, storedPasswordHash)
```

is used to verify the supplied password.

Plain-text passwords are never stored.

---

# 11. Role-Based Access Control

The backend does not rely only on frontend restrictions.

Role-based access is enforced server-side.

For example:

```text
Client
    → Client endpoints         Allowed
    → Freelancer endpoints     403 Forbidden
```

```text
Freelancer
    → Freelancer endpoints     Allowed
    → Client-only actions      403 Forbidden
```

The system therefore distinguishes between:

- **Authentication** — verifying who the user is
- **Authorisation** — determining what the user is allowed to do

Attempting to access an endpoint using the wrong role results in:

```http
403 Forbidden
```

---

# 12. Gig Management

Freelancers can manage the services they offer.

## Create Gig

```http
POST /api/gigs
```

Example:

```json
{
  "title": "Website Development",
  "description": "Professional responsive website development service.",
  "category": "Web Development",
  "price": 1500
}
```

Only freelancers are permitted to create gigs.

## Browse Gigs

```http
GET /api/gigs
```

Clients can browse available gigs.

## View Gig

```http
GET /api/gigs/:id
```

## Freelancer Gigs

```http
GET /api/gigs/mine
```

Allows a freelancer to retrieve their own gigs.

Freelancers can also edit and delete their own gigs.

Ownership and role checks prevent users from managing gigs that do not belong to them.

---

# 13. Booking System

Clients submit requests for gigs offered by freelancers. A new booking is **pending** until the freelancer responds; a request does not immediately generate a transaction.

## Create Booking Request

```http
POST /api/bookings
```

Example:

```json
{
  "gigId": "GIG_ID"
}
```

The backend validates the authenticated client, gig, availability and duplicate active-booking restrictions before storing the request.

```text
Client selects a gig
        |
        v
POST /api/bookings
        |
        v
Validation + duplicate booking checks
        |
        v
Pending booking request
        |
        +------------------------------+
        |                              |
        v                              v
Freelancer accepts             Freelancer rejects
        |                              |
        v                              v
Booking becomes booked         Request is rejected
        |                              |
        v                              v
Transaction created            No transaction created
        |
        v
When transaction is completed,
it contributes to freelancer income
```

## Freelancer Decision Endpoints

```http
PATCH /api/bookings/:bookingId/accept
PATCH /api/bookings/:bookingId/reject
```

Only the freelancer assigned to the gig can accept or reject the request. The backend validates booking state and authorisation. Acceptance records the transaction; rejection does not generate income.

**Important:** The application records marketplace transactions; the README does not claim integration with a live payment gateway.

---

# 14. Duplicate Booking Protection

A client cannot submit another active request for the same gig while an existing request is pending or booked.

```text
Client A + Gig 1 → Pending request          Allowed
Client A + Gig 1 → Another active request   Rejected
Client A + Gig 2 → New request              Allowed
Client B + Gig 1 → New request              Allowed
```

The restriction is based on the authenticated client, gig and active booking status. The API returns `409 Conflict` for duplicate active bookings. The frontend indicates when the current client already has an active booking, but the server remains responsible for enforcement.

---

# 15. Client Booking Management

Clients can view their own booking requests using:

```http
GET /api/bookings/mine
```

Only the authenticated client's bookings are returned. The client interface displays booking information and its current status, including requests awaiting a freelancer decision.

---

# 16. Freelancer Booking Management

Freelancers can view requests for their own gigs using:

```http
GET /api/bookings/freelancer
```

The interface provides the service, client, price, date and status. The assigned freelancer can accept or reject a pending request through the protected booking decision endpoints. Other freelancers cannot act on bookings that do not belong to their gigs.

---

# 17. Transactions, Income and Estimated Tax

A transaction is created when a freelancer **accepts** a pending booking request, rather than when the client first submits the request.

The transaction relates the client, booking, gig and freelancer. The application distinguishes transaction records from income that has actually been completed.

Freelancers retrieve income information through:

```http
GET /api/bookings/income
```

**Only completed transactions contribute to the reported earned income.** Pending requests, rejected requests and transactions that have not been completed are excluded from completed-income totals.

The financial dashboard presents completed gross income, estimated tax, estimated net income, effective tax rate and completed transaction details. The backend calculates an **estimated flat 15% income tax** for display purposes:

```text
Estimated Tax = Completed Income × 0.15
Estimated After-Tax Income = Completed Income − Estimated Tax
```

For example, R10,000 of completed income gives an estimated tax of R1,500 and an estimated after-tax amount of R8,500.

This percentage is an application estimate, **not an official South African tax assessment or a tax payment function**.

```text
Client request → Pending booking → Freelancer acceptance
                                          |
                                          v
                                  Booked + Transaction
                                          |
                                          v
                                 Completed transaction
                                          |
                                          v
                            Completed income + tax estimate
```

---

# 18. MongoDB Storage

Part 1 initially used local JSON storage for secure authentication.

Part 2 replaces this with MongoDB using Mongoose.

The application stores:

```text
Users
Gigs
Bookings
Transactions
```

MongoDB provides persistent database-backed storage for the marketplace.

The connection string is stored using:

```text
MONGODB_URI
```

inside the local `.env` file.

The database connection value is not committed to GitHub.

---

# 19. Input Validation

Incoming API requests are validated before controller logic executes.

Validation is implemented using:

```text
express-validator
```

Authentication validation includes:

### Name

- Required
- Length restrictions
- Character restrictions

### Email

- Required
- Valid email format
- Normalised

### Password

- Required
- Minimum and maximum length
- Uppercase character
- Lowercase character
- Number
- Special character

Marketplace requests are also validated to prevent invalid gig, bookings and user data from being processed.

Invalid input produces:

```http
400 Bad Request
```

---

# 20. Security Measures

## HTTPS and TLS

The backend serves its API over HTTPS at `https://localhost:5000` during local development. A self-signed certificate is used locally and generated temporarily during CI testing.

## HTTP Security Headers

Helmet supplies security-related HTTP headers, including a configured Content Security Policy (CSP). CORS restricts permitted browser origins.

## Authentication and Authorisation

- JWTs authenticate protected requests and expire according to `JWT_EXPIRES_IN` (the documented default is `1h`).
- Server-side role checks enforce client, freelancer and administrator permissions.
- Resource ownership checks prevent freelancers from changing other freelancers' gigs or bookings.
- Passwords are stored as bcrypt hashes with a documented cost factor of 12.

## Login Protection

- After **five incorrect password attempts**, the account is locked for **15 minutes**.
- Requests to an account during its lock period return **`423 Locked`**.
- An authentication IP rate limiter restricts excessive attempts (configured at **10 requests per 15 minutes** in the final tested version), with **`429 Too Many Requests`** when exceeded.
- General API rate limiting is also enabled.

## Request Validation and Error Handling

`express-validator` checks untrusted inputs. The application returns controlled error responses and avoids exposing password hashes, signing secrets, connection strings and internal stack traces to clients.

## Secret Management

Local `.env` files and private keys must not be committed. GitHub Actions uses repository secrets for test credentials and database connections.

---

# 21. Error Handling

The API returns controlled error responses.

Examples include:

```text
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
423 Locked
429 Too Many Requests
500 Internal Server Error
```

The application avoids exposing:

- Password hashes
- JWT secrets
- Database connection strings
- File system paths
- Stack traces
- Internal configuration values

Unexpected errors return a generic response instead of exposing internal application details.

---

# 22. Environment Variables

The backend reads sensitive configuration from environment variables. Copy `backend/.env.example` to `backend/.env` and supply your own values.

Illustrative development configuration (placeholders only):

```env
NODE_ENV=development
HTTPS_PORT=5000
JWT_SECRET=replace_with_a_long_random_secret
JWT_EXPIRES_IN=1h
MONGODB_URI=your_mongodb_atlas_connection_string
```

The frontend connects to the API at `https://localhost:5000/api`. Where required, configure its Vite API URL using:

```env
VITE_API_URL=https://localhost:5000/api
```

**Never commit real `.env` files, JWT secrets, MongoDB credentials, test passwords or private TLS keys.**

## Dedicated End-to-End Database

Automated browser testing uses a separate MongoDB Atlas database named `hustlehub_e2e`, configured through `MONGODB_E2E_URI` in test mode. The backend's environment validation also requires `MONGODB_URI`, so both are set to the E2E connection string in the GitHub Actions test jobs.

This separation prevents CI tests from intentionally writing into the development database.

---

# 23. Running the Backend

## Prerequisites

Install:

- Node.js
- npm
- MongoDB Atlas account/database
- OpenSSL
- Git

Navigate to:

```powershell
cd HustleHub\backend
```

Install dependencies:

```powershell
npm install
```

Create:

```text
.env
```

using:

```text
.env.example
```

as the template.

Start the backend:

```powershell
npm run dev
```

The API will run at:

```text
https://localhost:5000
```

A successful startup should confirm the MongoDB connection and HTTPS server.

---

# 24. Running the Frontend

Open a separate terminal.

Navigate to:

```powershell
cd HustleHub\frontend
```

Install dependencies:

```powershell
npm install
```

Start Vite:

```powershell
npm run dev
```

Open the local URL displayed by Vite in the browser.

The frontend communicates with:

```text
https://localhost:5000/api
```

Because the backend uses a self-signed development certificate, the certificate may need to be accepted locally before browser requests succeed.

---

# 25. Backend Automated Testing

Backend tests use **Jest** and **Supertest** to verify authentication, validation, security restrictions and marketplace operations.

From `backend/`:

```powershell
npm ci
npm run lint
npm test
```

**Verified final result: 52 of 52 Jest tests passed across 3 suites, with zero failures.** The GitHub Actions Backend Tests job also passed, running dependency installation, ESLint and Jest with test-specific configuration.

The tests cover areas such as login validation, role enforcement, duplicate booking protection and the updated freelancer booking decision workflow.

---

# 26. Frontend Automated Testing

Frontend component and interface tests use **Vitest**, **React Testing Library** and **jsdom**.

From `frontend/`:

```powershell
npm ci
npm run lint
npm test -- --run
```

**Verified final result: 58 of 58 Vitest tests passed across 9 test files, with zero failures.** The GitHub Actions Frontend Tests and Build job also passed ESLint and the Vite production build.

Tested interface areas include login, protected routes, role routing, marketplace browsing, booking status, client bookings, freelancer gigs, freelancer bookings and income displays.

Playwright browser tests are documented separately in Section 33.

---

# 27. Production Build Testing

The frontend production build can be verified using:

```powershell
npm run build
```

A successful build creates:

```text
frontend/dist/
```

This verifies that the React application can be compiled successfully for production.

---

# 28. Postman and Newman Testing

The Part 2 API collection remains available at:

```text
postman/HustleHub-Part2.postman_collection.json
```

It documents the earlier marketplace API testing workflow: health checks, registration, login, role promotion, gig creation, marketplace browsing, bookings, income and role-based security checks.

From `backend/`, an example Newman command is:

```powershell
npx newman run ../postman/HustleHub-Part2.postman_collection.json `
  --insecure `
  --env-var "baseUrl=https://localhost:5000/api" `
  --env-var "adminEmail=YOUR_ADMIN_EMAIL" `
  --env-var "adminPassword=YOUR_ADMIN_PASSWORD"
```

**Part 2 result:** 16 requests passed, 30 assertions passed and zero failures.

**Version note:** This is the original **Part 2** collection. Its earlier booking-to-transaction assumptions may not reflect the final pending/accept/reject workflow. The final functionality is covered by the updated Jest and Playwright suites; do not present the old Newman result as proof of the updated booking lifecycle without rerunning or revising the collection.

---

# 29. Application Testing Procedure

The final verification procedure is:

## Backend

```powershell
cd backend
npm ci
npm run lint
npm test
npm run dev
```

## Frontend

Open a separate terminal:

```powershell
cd frontend
npm ci
npm run lint
npm test -- --run
npm run build
npm run dev
```

## Browser End-to-End Tests

Start the backend using the isolated E2E database and provide the test client and freelancer credentials through local environment variables. Then run from `frontend/`:

```powershell
npx playwright install chromium
npx playwright test
```

Playwright starts the Vite development server using its configuration. The test backend must be running and accessible over HTTPS.

## Docker Smoke Test

From the repository root, with Docker running, the backend `.env` configured and required development TLS certificates available:

```powershell
docker compose up -d --build
docker compose ps
docker compose down
```

## GitHub Actions

Push to `main` or open a pull request targeting `main`, then open **Actions → HustleHub CI**. Alternatively, use **Run workflow** manually. Check that all four jobs succeed: Backend Tests, Frontend Tests and Build, Playwright E2E Tests and Docker Full Stack.

---

# 30. Security Testing

Security verification covers:

- Invalid registration data and login attempts
- Missing, invalid and expired JWTs
- Role-based and ownership-based access rejection
- Duplicate active-booking prevention
- Account lockout after five failed password attempts
- Authentication and general API rate limiting
- Validation and controlled error responses
- Booking acceptance/rejection authorisation
- Test database isolation

Expected examples:

```text
Missing/invalid JWT                   → 401 Unauthorized
Client calling freelancer-only route  → 403 Forbidden
Duplicate active booking             → 409 Conflict
Locked account                       → 423 Locked
Excessive authentication requests    → 429 Too Many Requests
```

The Jest and Playwright suites provide repeatable regression checks. The final reported CI run passed all configured jobs.

---

# 31. Git and Secret Management

Sensitive files are excluded from Git through `.gitignore`.

Examples include:

```text
.env
node_modules/
backend/certificates/*.key
backend/certificates/*.crt
frontend/dist/
coverage/
```

The repository contains:

```text
.env.example
```

so developers can configure the project without exposing real credentials.

JWT secrets, MongoDB credentials and administrator passwords must not be committed.

---

# 32. Final POE Completion Status

The completed application and development infrastructure include:

- [x] React/Vite frontend and Express backend
- [x] MongoDB Atlas persistence through Mongoose
- [x] Client, freelancer and administrator roles
- [x] Registration, login and JWT-protected endpoints
- [x] bcrypt password hashing and input validation
- [x] Gig creation, browsing, editing and deletion
- [x] Client booking requests and duplicate active-booking protection
- [x] Freelancer booking acceptance and rejection
- [x] Transaction creation on accepted bookings
- [x] Completed-transaction income reporting
- [x] Estimated 15% tax calculation
- [x] HTTPS/TLS, Helmet, CSP and CORS
- [x] Additional security feature 1: account lockout after 5 failed attempts for 15 minutes
- [x] Additional security feature 2: JWT revocation on logout
- [x] Authentication and booking rate limiting
- [x] Controlled error handling and access checks
- [x] Backend Jest/Supertest tests (52 passed in the final reported local run)
- [x] Frontend Vitest/React Testing Library tests (58 passed across 9 files)
- [x] Playwright browser end-to-end tests (6 passed in the verified GitHub Actions run)
- [x] ESLint static analysis
- [x] Docker images and Docker Compose smoke testing
- [x] GitHub Actions CI triggered automatically on push/PR, with all four jobs passing
- [x] Better Stack cloud logging for authentication, bookings and transactions
- [x] Final security review covering injection, XSS, authentication and access control
- [x] Dedicated `hustlehub_e2e` MongoDB test database
- [x] Previous Part 1 and Part 2 and POE demonstration videos

```text
Authentication → Gig Marketplace → Pending Booking Request
                                      |
                                      v
                             Freelancer Decision
                               /           \
                          Accept           Reject
                            |                |
                            v                v
                       Transaction      No Transaction
                            |
                            v
                   Completed Income
                            |
                            v
                      Tax Estimate
```

---

# 33. Playwright End-to-End Testing

The final application includes **Playwright** tests that run a real Chromium browser against the React frontend and HTTPS backend.

Key files:

```text
frontend/playwright.config.js
frontend/e2e/
```

The Playwright configuration starts Vite automatically and uses `http://localhost:5173` for the frontend. Browser tests connect to the backend at `https://localhost:5000/api` and accept the local self-signed test certificate.

The E2E suite uses separate test credentials for a client and a freelancer, supplied through environment variables:

```text
E2E_CLIENT_EMAIL
E2E_CLIENT_PASSWORD
E2E_FREELANCER_EMAIL
E2E_FREELANCER_PASSWORD
```

Run from `frontend/` once the isolated test backend is running:

```powershell
npx playwright test
```

**Verified GitHub Actions result: 6 of 6 Playwright tests passed in 18.7 seconds.** The Playwright E2E Tests job completed successfully. CI uploads an HTML report and, on failure, test results and backend logs.

---

# 34. Docker Containerisation

HustleHub+ includes separate Docker images for the backend and frontend, orchestrated with Docker Compose.

## Docker Workflow

From the project root:

```powershell
docker compose up -d --build
```

Check running containers:

```powershell
docker compose ps
```

In the tested Compose setup:

```text
Backend API: https://localhost:5000/api
Frontend:    http://localhost:8080
```

Stop the stack:

```powershell
docker compose down
```

**Prerequisites:** Docker Desktop or Docker Engine with Compose, an appropriate backend `.env` and the required HTTPS certificate files. Local development uses the configured development database; the GitHub Actions Docker job deliberately uses the isolated E2E database.

The Docker Full Stack CI job builds both images, starts the services, checks the backend health endpoint and confirms the frontend responds before shutting down the containers.

---

# 35. GitHub Actions Continuous Integration

The workflow is stored at:

```text
.github/workflows/ci.yml
```

The workflow is triggered **automatically on pushes to `main` and pull requests targeting `main`**. It also supports `workflow_dispatch` for manual runs. The workflow validates code changes; it is **not an automatic production deployment**.

## CI Jobs

| Job | Validation performed | Final reported result |
|---|---|---|
| Backend Tests | `npm ci`, ESLint, Jest | Passed |
| Frontend Tests and Build | `npm ci`, ESLint, Vitest, Vite build | Passed |
| Playwright E2E Tests | HTTPS test backend, isolated database, Chromium tests | Passed |
| Docker Full Stack | Image builds, Compose startup, backend/frontend health checks | Passed |

The Playwright and Docker jobs depend on successful backend and frontend jobs.

## GitHub Actions Secrets

The workflow uses repository secrets rather than committing credentials:

```text
JWT_SECRET
MONGODB_E2E_URI
E2E_CLIENT_EMAIL
E2E_CLIENT_PASSWORD
E2E_FREELANCER_EMAIL
E2E_FREELANCER_PASSWORD
```

In test jobs, both `MONGODB_URI` and `MONGODB_E2E_URI` are set to the E2E secret because backend environment validation requires `MONGODB_URI`, while the test database connection uses `MONGODB_E2E_URI`.

The Playwright job also checks the backend startup log for the database name `hustlehub_e2e` to verify isolation.

## Run the Workflow

1. Push a commit to `main` or open a pull request targeting `main` to trigger the workflow automatically.
2. Open the repository on GitHub and select **Actions → HustleHub CI**.
3. Open the latest run and confirm all four jobs succeeded.
4. Alternatively, use **Run workflow** to start a manual validation.

The verified final run was triggered by a **push** and completed successfully across all four jobs. CI checks fail when configured tests, lint checks, builds or smoke checks fail.

---

# 36. Final Application Scope and Submission Notes

HustleHub+ is a full-stack educational marketplace project demonstrating secure application development, persistent storage, role-based workflows, testing and CI practices.

The final application supports the following principal flow:

```text
User registration and login
            |
            v
Freelancer publishes gig
            |
            v
Client browses and requests gig
            |
            v
Freelancer accepts or rejects
            |
            v
Accepted booking creates transaction
            |
            v
Completed transactions count toward income
            |
            v
Estimated 15% tax is displayed
```

**Scope boundaries:** Income and tax values are application calculations, not evidence of a real payment processor or a statutory tax calculation. The self-signed certificate is for local development and testing. The GitHub Actions workflow validates the application but does not deploy it automatically.

**Submission materials:** The final demonstration recording, application screenshots, test results and CI/Docker evidence should accompany the completed project as required by the lecturer's marking rubric. Existing Part 1 and Part 2 and POE demonstration links are retained at the top of this README.

---

# 37. Additional Security Features

The final POE includes **two security features implemented in addition to the existing JWT authentication, bcrypt password hashing, role-based access control, validation, HTTPS and rate limiting**. Both features have been demonstrated and tested.

## Feature 1 — Failed-Login Account Lockout

**Security risk addressed:** Repeated password guessing against a known account.

After **five unsuccessful password attempts**, the application temporarily locks the affected account for **15 minutes**. While locked, login attempts are rejected with **HTTP 423 Locked**, and the frontend displays a message explaining the temporary restriction. A successful login resets the failed-attempt state when the account is not locked.

This account-specific control complements the authentication endpoint's **10 requests per 15 minutes** rate limit; it is not the same as the rate limiter. The lockout is implemented in the authentication logic and backed by the application's user data.

**Relevant implementation:** `backend/src/controllers/authController.js`, authentication routes, user model, and frontend login interface.

**Verification evidence:** The POE screenshot shows the login interface displaying: “Too many failed login attempts. Please wait 15 minutes before trying again.” This demonstrates the user-facing result of the lockout.

## Feature 2 — JWT Revocation on Logout

**Security risk addressed:** Continued use of an otherwise valid JWT after the user has logged out.

When an authenticated user calls `POST /api/auth/logout`, the backend records the current token as revoked until its expiry. The `authenticateToken` middleware verifies the JWT signature and expiry **and checks the revocation record** before allowing access to protected endpoints. Consequently, a revoked token is rejected even if its original expiration time has not yet been reached.

**Relevant implementation:** `backend/src/controllers/authController.js`, `backend/src/middleware/authMiddleware.js`, `backend/src/utils/tokenRevocation.js`, and `backend/src/routes/authRoutes.js`.

**Verification evidence:** The same token was tested with Postman in the following sequence:

| Step | API request | Expected / observed result |
|---|---|---|
| 1 | `GET /api/auth/me` using valid bearer token | **200 OK** before logout |
| 2 | `POST /api/auth/logout` using that token | **200 OK**, logout successful |
| 3 | `GET /api/auth/me` reusing the same token | **401 Unauthorized** after logout |

This feature is distinct from the frontend simply clearing `sessionStorage`: the backend itself rejects reuse of the logged-out token.

---

# 38. Application Logging and Better Stack Monitoring 

HustleHub+ implements structured application logging in the backend and forwards operational events to **Better Stack** through the `@logtail/node` integration. The logger supports informational, warning and error messages. The integration is configured using environment variables rather than hard-coded cloud credentials.

**Relevant files:** `backend/src/utils/logger.js`, `backend/src/controllers/authController.js`, `backend/src/controllers/bookingController.js`, and `backend/src/app.js`.

Logged events include:

- Successful authentication and failed login attempts.
- Account lockout and logout events.
- Creation of a client booking request.
- Freelancer acceptance or rejection of a booking request.
- Transaction creation following booking acceptance.
- Unexpected application errors handled by the global Express error handler.

The application sends structured context such as booking IDs, freelancer IDs, status and transaction amount with relevant events. The global error handler logs diagnostic details internally while returning a controlled generic error response to clients. Logging is disabled during automated tests to avoid polluting the cloud log stream.

**Cloud configuration:** The logger uses `LOGTAIL_SOURCE_TOKEN` and `LOGTAIL_INGESTING_HOST` (where configured). These are sensitive configuration values and must be kept outside version control.

**Verification evidence:** The POE includes a Better Stack screenshot of the **HustleHub Backend** source showing timestamped INFO entries for successful login, booking request creation, booking review and transaction creation. Additional captured logs include a WARN event for an account lockout. This demonstrates that application events are reaching the external monitoring service, not merely being printed locally.

**Operational boundary:** Logging provides visibility and diagnostics; it is not a substitute for alerting, access control or secure retention policies. Credentials and bearer tokens should never be logged.

---

# 39. Final Security Review 

This review assesses the final application against the four specified categories: **injection, cross-site scripting (XSS), authentication and access control**. It describes protections observed in the submitted code; it does not claim the system is free of all vulnerabilities.

## 39.1 Injection and Input Handling

The Express API uses `express-validator` to validate incoming data before controllers process requests. Registration and login validate expected field formats; gig creation and updates enforce data types, length limits, permitted fields and price constraints; booking creation validates `gigId` as a MongoDB ObjectId. Validation failures are returned as **HTTP 400** through the shared validation middleware.

MongoDB access uses **Mongoose models** rather than manually concatenated query strings. The reviewed booking controller uses fixed query keys and application-defined operators, and the gig update controller explicitly allowlists writable fields (`title`, `description`, `category`, `price`, `isActive`). These practices reduce injection and mass-assignment risk.

**Residual risk:** Mongoose alone does not guarantee immunity to NoSQL injection. Any new endpoint accepting query objects, filters or operators from the client should validate and restrict them before database use. Route parameters not covered by an ObjectId validator should continue to handle invalid IDs safely.

**Files:** `backend/src/validators/`, `backend/src/middleware/validationMiddleware.js`, `backend/src/controllers/gigController.js`, `backend/src/controllers/bookingController.js`.

## 39.2 Cross-Site Scripting (XSS)

Gig text fields are escaped through `express-validator`, and normal text rendered by React JSX is escaped by React. Helmet provides security-related HTTP headers, including the configured Content Security Policy (CSP). Together these controls reduce the risk of injected markup or scripts executing in a user's browser.

**Residual risk:** React's normal escaping does not protect HTML inserted through unsafe raw-HTML rendering, third-party components or unsafe DOM manipulation. The application stores its JWT in browser `sessionStorage`, so preventing script execution remains important: a successful XSS attack could expose the token. Any future rich-text or HTML rendering must be reviewed and sanitized.

**Files:** `backend/src/validators/gigValidators.js`, `backend/src/app.js`, and React frontend components.

## 39.3 Authentication and Session Security

Protected endpoints use signed JWTs. `authenticateToken` rejects missing, invalid, expired and revoked tokens. Passwords are hashed using **bcryptjs** with cost factor **12**, and registration applies password complexity checks. JWT signing secrets and database credentials are held in environment configuration rather than source code.

Additional protections include **10 authentication requests per 15 minutes**, account lockout after **five failed attempts for 15 minutes**, and **server-side token revocation on logout**. Authentication and security events are recorded through the logger.

**Residual risk:** Token security still depends on protecting signing keys, using HTTPS, controlling XSS and maintaining the revocation store. The development self-signed TLS certificate is not a production certificate.

**Files:** `backend/src/controllers/authController.js`, `backend/src/middleware/authMiddleware.js`, `backend/src/routes/authRoutes.js`, `backend/src/utils/tokenRevocation.js`.

## 39.4 Broken Access Control and Ownership

All reviewed gig, booking, income and administrator routes require JWT authentication. The `requireRole` middleware restricts actions by role: clients create booking requests, freelancers manage gigs and review assigned bookings, and **only administrators** can update user roles. Missing authentication returns **401**; insufficient role privileges return **403**.

The backend also enforces **resource ownership**, independently of the frontend:

- `getMyGigs` queries gigs owned by `req.user.sub`.
- `updateGig` and `deleteGig` compare the gig's `freelancer` with the authenticated user's ID and reject non-owners with **403**.
- `getMyClientBookings` filters by authenticated client ID.
- `getFreelancerBookings` and `getFreelancerIncome` filter by authenticated freelancer ID.
- Booking acceptance/rejection updates only a booking matching the requested ID, the authenticated freelancer and `status: "pending"`.
- Booking approval and transaction creation occur in a MongoDB transaction, reducing inconsistent partial updates.

These measures reduce the risk of **Insecure Direct Object Reference (IDOR)** and unauthorized state changes.

**Review observation:** `GET /api/gigs/:id` retrieves a gig by ID for any authenticated user without checking `isActive`. If inactive gigs are meant to be hidden from everyone except their owner, that endpoint should add an owner-or-active visibility condition. The existing update/delete ownership checks remain enforced.

**Files:** `backend/src/middleware/roleMiddleware.js`, `backend/src/routes/userRoutes.js`, `backend/src/routes/gigRoutes.js`, `backend/src/routes/bookingRoutes.js`, `backend/src/controllers/gigController.js`, `backend/src/controllers/bookingController.js`.

## Security Review Conclusion

The final implementation uses layered defenses: validation and field allowlists, browser-side escaping and CSP, password hashing and JWT verification, account lockout and token revocation, rate limiting, RBAC and record-level ownership checks. These measures address the four requested security categories while preserving realistic limitations and future review requirements.

---

# 40. Final POE Evidence and Verification Summary

| POE requirement | Implemented evidence | Verification |
|---|---|---|
| Tax estimation and income dashboard (20) | Completed-transaction income, 15% estimate, net income, dashboard and transaction details | Dashboard screenshot with completed transactions |
| CI/CD automated validation (20) | `.github/workflows/ci.yml` runs on push/PR to `main` and optionally manually | Green push-triggered GitHub Actions run; four passing jobs |
| Static analysis in CI (10) | ESLint in backend and frontend CI jobs | Both lint steps completed successfully |
| Docker and Compose in CI (10) | `backend/Dockerfile`, `frontend/Dockerfile`, `docker-compose.yml` | Both containers up in CI; backend and frontend smoke checks |
| Comprehensive/cloud logging (10) | Backend logger and Better Stack integration | Timestamped cloud events for login, booking and transaction |
| Two additional security features (15) | Failed-login account lockout; JWT revocation on logout | Login lockout screenshot and Postman 200/200/401 sequence |
| Final security review (10) | Section 39 covers injection, XSS, authentication, access control | Source-level inspection documented above |
| Complete README (5) | Architecture, setup, security, endpoints, tests, Docker, CI, logging, features, contributors | This document |

## Final Verified Test Totals

| Suite | Passed | Scope |
|---|---:|---|
| Backend Jest / Supertest | **52 / 52** | **3 suites** |
| Frontend Vitest / React Testing Library | **58 / 58** | **9 test files** |
| Playwright end-to-end | **6 / 6** | Chromium browser tests in GitHub Actions |
| **Combined** | **116 / 116** | **0 failures in the reported runs** |

The GitHub Actions workflow also verifies ESLint, frontend build and Docker Compose startup. Those checks are not counted as additional unit or E2E tests in the total above.

---

## References
- Code attributes added to relevant files.
- Document contains full reference list.

# Group Members

**ST10315122 - Nikhil Rajkumar**  
Backend functions

**ST10456110 - Mohammed Amaan Gaffar**  
Frontend

**ST10456550 - Talhah Patel**  
Backend authentication

**ST10260039 - Veolin Naidoo**  
Testing
