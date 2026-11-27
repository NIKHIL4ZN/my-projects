package com.familylinklive.api.notifications

/**
 * Server-side translations for push notification text (REQ-20): a
 * notification is written in the RECIPIENT's saved language, not the
 * sender's. Supports English (EN), Afrikaans (AF) and isiZulu (ZU).
 */
object Localization {

    enum class Key { NEW_MESSAGE, INVITE_RECEIVED, INVITE_ACCEPTED, ARRIVED, LEFT, SOS }

    private data class Template(val title: String, val body: String)

    // %1$s / %2$s are positional so each language can reorder them freely.
    private val en = mapOf(
        Key.NEW_MESSAGE to Template("New message from %1\$s", "%2\$s"),
        Key.INVITE_RECEIVED to Template("New contact request", "%1\$s wants to connect with you on FamilyLink Live"),
        Key.INVITE_ACCEPTED to Template("Contact request accepted", "%1\$s accepted your request"),
        Key.ARRIVED to Template("%1\$s arrived", "%1\$s arrived at %2\$s"),
        Key.LEFT to Template("%1\$s left", "%1\$s left %2\$s"),
        Key.SOS to Template("\uD83D\uDEA8 SOS from %1\$s", "%1\$s needs help. Tap to see their location.")
    )

    private val af = mapOf(
        Key.NEW_MESSAGE to Template("Nuwe boodskap van %1\$s", "%2\$s"),
        Key.INVITE_RECEIVED to Template("Nuwe kontakversoek", "%1\$s wil met jou skakel op FamilyLink Live"),
        Key.INVITE_ACCEPTED to Template("Kontakversoek aanvaar", "%1\$s het jou versoek aanvaar"),
        Key.ARRIVED to Template("%1\$s het aangekom", "%1\$s het by %2\$s aangekom"),
        Key.LEFT to Template("%1\$s het vertrek", "%1\$s het %2\$s verlaat"),
        Key.SOS to Template("\uD83D\uDEA8 SOS van %1\$s", "%1\$s het hulp nodig. Tik om hul ligging te sien.")
    )

    private val zu = mapOf(
        Key.NEW_MESSAGE to Template("Umlayezo omusha ovela ku-%1\$s", "%2\$s"),
        Key.INVITE_RECEIVED to Template("Isicelo esisha sokuxhumana", "U-%1\$s ufuna ukuxhumana nawe ku-FamilyLink Live"),
        Key.INVITE_ACCEPTED to Template("Isicelo samukelwe", "U-%1\$s wamukele isicelo sakho"),
        Key.ARRIVED to Template("U-%1\$s ufikile", "U-%1\$s ufike ku-%2\$s"),
        Key.LEFT to Template("U-%1\$s uhambile", "U-%1\$s ushiye %2\$s"),
        Key.SOS to Template("\uD83D\uDEA8 I-SOS evela ku-%1\$s", "U-%1\$s udinga usizo. Thinta ukubona indawo akuyo.")
    )

    /** Returns (title, body) for [key] in [language] ("EN", "AF" or "ZU"; anything else falls back to English). */
    fun text(language: String, key: Key, args: List<String>): Pair<String, String> {
        val table = when (language.uppercase()) {
            "AF" -> af
            "ZU" -> zu
            else -> en
        }
        val template = table.getValue(key)
        val argArray = args.toTypedArray<Any?>()
        return String.format(template.title, *argArray) to String.format(template.body, *argArray)
    }
}
