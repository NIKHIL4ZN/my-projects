
# BudgetTracker — Personal Budget Tracker

## 📋 Overview

Budget Tracker is a user-friendly personal budget tracker Android application developed for the **PROG7313 / OPSC7311** Portfolio of Evidence. The app helps users track their spending habits, set budget goals, work towards savings targets, and manage recurring subscriptions — all wrapped in a clean, modern Material 3 UI with gamification elements to keep budgeting engaging.

| Part | Focus | Storage |
|------|-------|---------|
| Part 1 | Research, Planning & Design | — |
| Part 2 | App Prototype Development | **Room (local SQLite)** |
| Part 3 | Final App Development | **Supabase (online PostgreSQL)** |

> **Switching between Part 2 and Part 3:** Change the single constant in `AppViewModel.kt`:
> ```kotlin
> const val USE_LOCAL_DB = true   // true = Part 2 (Room) | false = Part 3 (Supabase)
> ```

---

## ✨ Features

### Core Requirements
| Feature | Status |
|---------|--------|
| User registration and login | ✅ |
| Create expense categories with icon and colour | ✅ |
| Add expenses (amount, date, description, category) | ✅ |
| Attach receipt photo (camera or gallery) | ✅ |
| Set monthly minimum and maximum budget goals | ✅ |
| View expenses filtered by date range | ✅ |
| View receipt photos from the transaction list | ✅ |
| View total spent per category for a period | ✅ |
| Daily spending graph (custom Canvas bar chart) | ✅ |
| Visual progress dashboard with overspend highlighting | ✅ |
| Gamification badges and achievements | ✅ |
| Local offline database (Room / SQLite) — Part 2 | ✅ |
| Online database (Supabase) — Part 3 | ✅ |

### 🌟 Extra Feature 1 — Savings Goals
- Create personalised savings goals (e.g. "Save R5 000 for a laptop")
- Set a target amount and an optional deadline
- Make contributions and track progress as a live percentage
- Circular and linear progress indicators per goal
- Completion badge awarded when a goal is reached

### 🔄 Extra Feature 2 — Recurring Transactions & Subscriptions
- Add recurring expenses (Netflix, Spotify, Gym, etc.)
- Supports **monthly**, **weekly**, and **yearly** frequencies
- Calculates and displays the total **fixed monthly cost**
- Shows daily and yearly cost equivalents
- Pause or cancel individual subscriptions with a toggle switch

---

## 🏗 Architecture

FinWise follows the **MVVM (Model-View-ViewModel)** architecture pattern with a clean separation between the UI, business logic, and data layers.

```
┌─────────────────────────────────────────────┐
│           UI Layer (Jetpack Compose)         │
│  LoginScreen  OverviewScreen  BudgetScreen  │
│  TransactionsScreen  AnalyzeScreen  etc.    │
└──────────────────┬──────────────────────────┘
                   │ observes StateFlow
┌──────────────────▼──────────────────────────┐
│           ViewModel Layer                    │
│       AuthViewModel    AppViewModel          │
└──────────────────┬──────────────────────────┘
                   │ suspend functions
┌──────────────────▼──────────────────────────┐
│           Repository Layer                   │
│  Local (Room)          Remote (Supabase)     │
│  LocalAuthRepository   AuthRepository        │
│  LocalExpenseRepo      ExpenseRepository     │
│  LocalBudgetRepo       BudgetRepository      │
│  LocalSavingsRepo      SavingsRepository     │
│  LocalRecurringRepo    RecurringRepository   │
└──────┬───────────────────────┬──────────────┘
       │                       │
┌──────▼───────┐     ┌─────────▼────────┐
│  Room / SQLite│     │ Supabase + Airtable│
│  (Part 2)    │     │ (Part 3)           │
└──────────────┘     └────────────────────┘
```

### Technology Stack

| Category | Technology |
|----------|-----------|
| Language | Kotlin 2.0.0 |
| UI Framework | Jetpack Compose + Material 3 |
| Architecture | MVVM + Repository Pattern |
| Local Database | Room 2.6.1 (SQLite) |
| Remote Database | Supabase (PostgreSQL) |
| Reporting Sync | Airtable REST API |
| Networking | Retrofit 2.11.0 + OkHttp 4.12.0 |
| Image Loading | Coil 2.6.0 |
| Charts | Custom Canvas bar chart (zero dependencies) |
| Navigation | Navigation Compose 2.7.7 |
| Permissions | Accompanist Permissions 0.34.0 |
| CI/CD | GitHub Actions |

---

## 🚀 Setup

### Prerequisites
- Android Studio Hedgehog or newer
- JDK 17
- Android device or emulator (API 25+)
- Git

---

## 🗄 Database Design

### Local — Room (SQLite) — Part 2

```
users                    categories
─────────────────────    ──────────────────────────
id          TEXT PK      id           TEXT PK
email       TEXT         user_id      TEXT
password_hash TEXT        name         TEXT
                         icon         TEXT
                         color        TEXT
                         budget_limit REAL
                         created_at   TEXT

expenses                 budget_goals
─────────────────────    ──────────────────────────
id          TEXT PK      id           TEXT PK
user_id     TEXT         user_id      TEXT
category_id TEXT         month        INTEGER
amount      REAL         year         INTEGER
date        TEXT         min_amount   REAL
description TEXT         max_amount   REAL
receipt_url TEXT         total_budget REAL
created_at  TEXT         created_at   TEXT

savings_goals            recurring_transactions
──────────────────────   ─────────────────────────
id           TEXT PK     id           TEXT PK
user_id      TEXT        user_id      TEXT
name         TEXT        category_id  TEXT
target_amount REAL       name         TEXT
current_amount REAL      amount       REAL
deadline     TEXT        frequency    TEXT
icon         TEXT        next_date    TEXT
color        TEXT        is_active    INTEGER
created_at   TEXT        icon         TEXT
                         created_at   TEXT
```

### Remote — Supabase (PostgreSQL) — Part 3

The same schema is mirrored in Supabase with:
- **Row Level Security (RLS)** so users can only access their own data
- **UUID primary keys** auto-generated by `gen_random_uuid()`
- **Supabase Storage** bucket (`receipts`) for receipt images

---

## ⚙️ GitHub Actions CI

The CI pipeline runs automatically on every push to `main` or `develop` and on every pull request.

### What it does

```
Push to main/develop
        │
        ▼
┌───────────────────┐
│  Checkout code    │
└────────┬──────────┘
         ▼
┌───────────────────┐
│  Set up JDK 17    │
└────────┬──────────┘
         ▼
┌───────────────────┐
│  Write secrets to │
│  local.properties │
└────────┬──────────┘
         ▼
┌───────────────────┐
│  ./gradlew        │
│  assembleDebug    │
└────────┬──────────┘
         ▼
┌───────────────────┐
│  ./gradlew test   │
└────────┬──────────┘
         ▼
┌───────────────────┐
│  Upload APK and   │
│  test results     │
└───────────────────┘
```

### Setting up GitHub Secrets

Go to your repository → **Settings → Secrets and variables → Actions → New repository secret** and add:

| Secret Name | Description |
|-------------|-------------|
| `SUPABASE_URL` | Your Supabase project URL |
| `SUPABASE_ANON_KEY` | Your Supabase anonymous/public key |
| `AIRTABLE_API_KEY` | Your Airtable personal access token |
| `AIRTABLE_BASE_ID` | Your Airtable base ID |

### Workflow file

The workflow is located at [`.github/workflows/build.yml`](.github/workflows/build.yml).

**References:**
- [Automated Build Android App with GitHub Action](https://github.com/marketplace/actions/automated-build-android-app-with-github-action) [Accessed 03 November 2025]
- [IMAD5112 GitHub Actions build.yml example](https://github.com/IMAD5112/Github-actions/blob/main/.github/workflows/build.yml) [Accessed 03 November 2025]

---

## 🏆 Gamification

FinWise rewards consistent budgeting behaviour with badges:

| Badge | Icon | How to Earn |
|-------|------|-------------|
| First Step | 🌱 | Log your first expense |
| Week Warrior | 🔥 | Log expenses 7 days in a row |
| Budget Boss | 🏆 | Stay within budget for a month |
| Savings Starter | 💰 | Create your first savings goal |
| Goal Getter | 🎯 | Complete a savings goal |
| Tracking Pro | 📊 | Log 10 expense entries |
| Organiser | 🗂️ | Create 5 categories |

---

## 📁 Project Structure

```
application001/
├── .github/
│   └── workflows/
│       └── build.yml                   ← GitHub Actions CI pipeline
├── app/
│   └── src/main/
│       ├── AndroidManifest.xml
│       ├── res/
│       │   ├── drawable/
│       │   │   └── ic_app_logo.png     ← App logo (launcher + splash + UI)
│       │   ├── values/
│       │   │   └── themes.xml
│       │   └── xml/
│       │       └── file_paths.xml      ← FileProvider for receipt camera
│       └── java/com/example/application001/
│           ├── MainActivity.kt
│           ├── data/
│           │   ├── local/              ← Room database layer (Part 2)
│           │   │   ├── AppDatabase.kt
│           │   │   ├── Mappers.kt
│           │   │   ├── dao/Daos.kt
│           │   │   └── entity/Entities.kt
│           │   ├── model/Models.kt     ← Shared domain models
│           │   ├── remote/             ← Supabase + Airtable (Part 3)
│           │   │   ├── SupabaseClient.kt
│           │   │   └── AirtableService.kt
│           │   └── repository/
│           │       ├── AuthRepository.kt         ← Supabase auth
│           │       ├── LocalAuthRepository.kt    ← Room auth
│           │       ├── ExpenseRepository.kt      ← Supabase data
│           │       ├── LocalRepositories.kt      ← Room data
│           │       └── Repositories.kt           ← Budget/Savings/Recurring
│           ├── ui/
│           │   ├── components/CommonComponents.kt
│           │   ├── navigation/NavGraph.kt
│           │   ├── screens/
│           │   │   ├── auth/           ← Login, Register
│           │   │   ├── overview/       ← Dashboard
│           │   │   ├── budget/         ← Budget management
│           │   │   ├── transactions/   ← Expense list + Add expense
│           │   │   ├── analyze/        ← Charts and analytics
│           │   │   ├── goals/          ← Savings goals
│           │   │   └── subscriptions/  ← Recurring transactions
│           │   └── theme/              ← Colors, Typography, Theme
│           └── viewmodel/
│               ├── AppViewModel.kt     ← USE_LOCAL_DB flag here
│               └── AuthViewModel.kt
├── gradle/
│   └── libs.versions.toml             ← Dependency version catalog
├── supabase_setup.sql                 ← Run in Supabase SQL Editor
├── build.gradle.kts
└── settings.gradle.kts
```

---

## 🎬 Demo Video


📹 **Video Link for part 2:** https://youtu.be/x7zztGBKxMw?si=0uKJZ2ymXk2y10Hr

📹 **Video Link for POE, part 3:** https://youtu.be/aQJe8Ci96Mk

The video demonstrates:
- Registering a new account and logging in
- Creating expense categories
- Adding expenses with receipt photos
- Setting monthly budget goals
- Viewing the spending dashboard and charts
- Using savings goals with progress tracking
- Managing recurring subscriptions
- Earning gamification badges

---

## 👥 Group Members

ST10456110----MOHAMMED AMAAN GAFFAR

ST10315122----NIKHIL RAJKUMAR

ST10456550----TALHAH PATEL

ST10260039----VEOLIN NAIDOO 

ST10252574----SABIEN NAIDOO

---
