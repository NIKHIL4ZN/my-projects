# FamilyLink Live — Release Notes

## v3.0.0 — Final POE

Every functional change since the Part 2 prototype (v1.0), grouped by what a user can now do.

### New: features required by the brief
| Feature | What changed |
|---|---|
| **Biometric authentication** | Fingerprint / face (or device PIN on Android 11+) unlocks the app. Offered once after first sign-in, toggled in *Settings → Biometric lock*, and re-asked after 30 seconds in the background. The flag is also saved on the server. |
| **Offline mode with synchronisation** | Messages, location shares and SOS alerts are saved to an on-device Room outbox and sent automatically when a connection returns (instantly if the app is open, via WorkManager if it was closed). Queued messages show "Sending…". Contacts, places and chat history are cached for offline reading. An orange/blue banner shows offline and syncing status. |
| **Real-time push notifications** | Firebase Cloud Messaging alerts for new messages, contact requests, accepted requests, arrivals/departures and SOS. Separate notification channels; SOS is high-priority. Tapping a notification opens the right screen (chat, map centred on the point, or contacts). |
| **Multi-language** | The whole interface and every push notification are available in **English, Afrikaans and isiZulu**, switchable in *Settings → Language* with no restart. Notifications use the *recipient's* language. |
| **Production assets** | Adaptive launcher icon with Android 13 themed (monochrome) variant, status-bar notification icon, 512 px store icon, 1024×500 feature graphic. |

### New: user-defined features
| # | Feature | What changed |
|---|---|---|
| 4 | **Geofence alerts** | The server compares each location update with the person's Places. When they arrive or leave, every accepted contact gets a push and an entry in their Activity tab. Places can carry an optional *arrival message* that is posted into each contact's chat. |
| 5 | **Emergency SOS** | Press-and-hold the SOS button, then a 5-second cancellable countdown. Sends the exact location to all accepted contacts with a high-priority alert, ignoring the "share my location" switch. Works offline (queued). |

### New: cloud storage requirements
| Item | What changed |
|---|---|
| **Blob storage** | Profile photos are resized on the phone, uploaded through the API to **Azure Blob Storage**, and shown in Settings and the contact list. |
| **NoSQL database** | A new **Activity** tab reads alert history from **Cloud Firestore** in real time. The backend writes the documents; security rules let each user read only their own. |

### New: supporting improvements
* **Smart Ping live sharing** — optional *Live location sharing* toggle runs a visible foreground service that pings every 15 s (driving) / 60 s (walking) / 5 min (still).
* **Silent session refresh** — when the 24-hour session token expires the app signs in again quietly instead of failing; this also protects the offline sync after a long time offline.
* **Account deletion** — *Settings → Delete my account* removes the user's data from the database, Firestore, Blob Storage and Firebase Authentication (required by Google Play).
* **Persistent hosting database** — the backend can use PostgreSQL (`DATABASE_URL`); the previous embedded H2 file was wiped every time Render's free tier slept.
* **Logging** — `android.util.Log` added across lifecycle, authentication, sync, biometric, push and location code.
* **Technical referencing** — source comments cite the documentation each pattern comes from.

### Changed
* Bottom navigation now has five tabs: Map, Contacts, Places, **Activity**, Settings.
* "Share my location" now requests a *fresh* GPS fix instead of reading a possibly-empty cache.
* Contact avatars use the uploaded profile photo.
* Chat messages are idempotent — a retried message can never appear twice.
* Backups are disabled (`allowBackup=false`) so chats and tokens aren't copied to cloud backups.

### Fixed
* Location sharing failed with "could not get your current location" on devices with no cached fix.
* The offline outbox and chat could not recover once the 24-hour session token had expired.
* GitHub Actions never ran because the workflow only listened to `main`/`develop`; it now runs on every branch, and no longer needs the private `google-services.json`.
* The map polling loop did not restart after returning to the Map tab.

### Known limitations
Places, invitations and account changes need a connection · chat uses 5 s polling plus push, not WebSockets ·
geofence detection has no boundary hysteresis · Afrikaans/isiZulu text needs native-speaker proof-reading ·
Render's free tier sleeps when idle.

---

## v1.0 — Part 2 prototype (for comparison)
Google SSO · settings (location sharing, sign out) · custom REST API · live map · places (geofence CRUD) ·
email-invite contacts with accept · private per-contact chat · GitHub Actions CI.
