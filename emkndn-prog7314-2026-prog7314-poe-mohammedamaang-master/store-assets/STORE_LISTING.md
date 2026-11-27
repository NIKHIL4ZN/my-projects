# Google Play Store Listing — FamilyLink Live

Everything below is ready to paste into the Play Console. Items marked **[YOU]** need your own input.

## Graphics (in this folder)
| Asset | File | Requirement |
|---|---|---|
| App icon | `icon-512.png` | 512 × 512 PNG |
| Feature graphic | `feature-graphic-1024x500.png` | 1024 × 500 |
| Phone screenshots **[YOU]** | `screenshots/` | 2–8 images, 16:9 or 9:16, 320–3840 px per side |

**Take the screenshots on your phone** (power + volume-down) from the signed release build. Recommended six, in this order:
1. Map with a contact's pin · 2. Contacts (one accepted, one pending) · 3. Chat thread ·
4. Activity feed with a geofence alert and an SOS · 5. Settings (language + biometric lock) · 6. SOS countdown.
Save them as `store-assets/screenshots/01-map.png` … and reuse them in `docs/screenshots/` for the README.

## Main store listing
* **App name (max 30):** `FamilyLink Live`
* **Short description (max 80):** `Share live location, chat privately and get safety alerts with your family.`
* **Category:** Social · **Tags:** family, location sharing, safety
* **Contact email:** [YOU] · **Website:** your GitHub repository URL · **Privacy policy URL:** [YOU — publish `docs/PRIVACY_POLICY.md`]

**Full description (max 4000)**
```
FamilyLink Live keeps the people you care about close — without giving up your privacy.

SEE WHERE EVERYONE IS
Invite family and friends by email. Once they accept, you can see each other on a live map. You are only ever visible
to people you have accepted, and you can switch sharing off in one tap.

CHAT PRIVATELY
Every contact has their own private conversation. Messages you send without a connection are saved and delivered
automatically the moment you are back online.

KNOW THEY ARRIVED SAFELY
Save places like Home, School or Work. FamilyLink Live tells your contacts when you arrive or leave, and can post a
message such as "Home safe" for you.

EMERGENCY SOS
Press and hold SOS, and after a 5-second countdown (cancel any time) every contact receives a high-priority alert with
your exact location. If you are offline, the alert is sent as soon as you reconnect.

BUILT FOR SOUTH AFRICA
Use the app in English, Afrikaans or isiZulu — notifications arrive in each person's own language.

SECURE
Lock the app with your fingerprint or face. Delete your account and all your data from Settings at any time.

Live location sharing in the background is optional, shows a notification while it runs, and can be stopped at any time.
```

## App content
* **Target audience:** 13 years and older (not designed for children).
* **Ads:** None. · **App access:** sign in with Google (provide a test Google account in the "App access" section for reviewers).
* **Content rating questionnaire:** user-to-user communication = yes (private messages); location sharing = yes; no violence, sexual content, gambling or purchases.

## Data safety form (answers)
| Data type | Collected | Shared | Purpose | Optional? |
|---|---|---|---|---|
| Name · Email · Profile photo | Yes | Shared with other users you accept | App functionality, account management | No (sign-in) / photo optional |
| **Approximate & precise location** | Yes | Shared with users you accept | App functionality | Yes |
| Messages | Yes | Shared with the other person in the thread | App functionality | Yes |
| Device or other IDs (push token) | Yes | No | App functionality (notifications) | No |
| Photos | Yes (profile photo only) | Shared with accepted contacts | App functionality | Yes |

* Data **encrypted in transit:** Yes (HTTPS). · **Users can request deletion:** Yes — in-app (*Settings → Delete my account*) and by email.
* No data sold, no advertising, no analytics SDKs.

## Declarations you will be asked for
* **Foreground service — location:** the user switches on *Live location sharing*; a persistent notification is shown; it
  lets contacts see a moving user and drives arrival alerts while the phone is in a pocket. Demonstrate it in a short video.
* **Location permission:** *foreground only* (`ACCESS_FINE_LOCATION`); background location permission is **not** requested.
* **Notifications (Android 13+):** requested at runtime. · **Biometric:** `USE_BIOMETRIC`.

## Release
* **Package:** `com.familylinklive.app` · **Version:** 3.0.0 (3) · **Target SDK 34 / Min SDK 25**
* **Signed artefacts:** `app-release.apk` (evidence for this POE) and `app-release.aab` (what Play Console uploads) — built with the private keystore (see `docs/PART3_SETUP_GUIDE.md`, Step 8).
* Register the **release** SHA-1 (and Play's app-signing SHA-1) in Firebase or Google Sign-In will fail on the store build.
* A Play Console developer account has a one-time registration fee, so uploading is optional for the POE; the brief asks for readiness evidence: signed APK, screenshots and the material above.
