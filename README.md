# 📧 Recipe Notification Service

En dedikert, asynkron bakgrunnstjeneste for e-postdistribusjon og varsling i Kjøkkenhylla-plattformen. Tjenesten lytter utelukkende på meldinger fra meldingsbussen (RabbitMQ) og håndterer alt fra velkomstmeldinger og passordtilbakestillinger til admin-handlinger, sikkerhetsvarsler og automatisert konto-opprydding.

---

## 🎯 Hensikt og Formål

* **Lynrask Brukeropplevelse:** E-postutsendelse over SMTP tar tid. Ved å flytte utsendelsen til en egen bakgrunnstjeneste får brukeren umiddelbar respons i applikasjonen, mens e-posten behandles asynkront i bakgrunnen.
* **Isolasjon av E-postlogikk:** Skiller e-postmaler, HTML-design og e-postleverandører (SMTP/MailKit) helt ut fra `Account API` og `Core API`.
* **Strenge Domenegrenser:** Koden og malene er strukturert i tre tydelige domener: `AdminActions`, `SystemActions` og `UserActions`.
* **Automatisk Livssyklus & GDPR:** Håndterer påminnelser, deaktivering og permanent sletting av ubekreftede kontoer i henhold til plattformens juridiske brukervilkår (§ 3).
* **Enkel Vedlikeholdbarhet:** HTML-maler ligger adskilt fra kildekoden med inline CSS, slik at e-postdesign kan endres uten å berøre forretningslogikken.

---

## 🛠️ Teknologistack & Feilhåndtering

* **Framework:** .NET 10 / C#
* **Meldingsbuss:** MassTransit 8 (med RabbitMQ som broker)
* **E-postmotor:** MailKit / MimeKit (SMTP)
* **Malmotor:** Scriban (HTML-rendering med dynamiske variabler)
* **Spesifikke Unntak (Custom Exceptions):**
* `TemplateRenderException`: Kastes dersom en Scriban-mal mangler på disk, har syntaksfeil eller opplever feil under rendering. (Fail-Fast / Intern kildekodefeil).
* `EmailDeliveryException`: Kastes ved feil under SMTP-overføringen (nettverksproblemer, provider-feil eller feilet tilkobling mot e-posttjener).


* **Lokal Testing:** Mailpit (`http://localhost:8025`)
* **Logging:** Serilog

---

## 🗺️ Arbeidsflyt (Execution Flow)

```text
[ RabbitMQ / MassTransit ]
          │
          ▼ (Mottar hendelse, f.eks. ContactFormSubmittedEvent)
1. Consumer (ContactFormSubmittedConsumer)
          │
          ▼ (Passerer melding & CancellationToken)
2. Processor (ContactFormProcessor)
          │
          ├──► Step 1: Render HTML (TemplateRenderService / Scriban)
          │        ├─ Leser mal fra TemplateService/Templates/{Domain}/*.html
          │        └─ Feiler rendering? ──► Kastes TemplateRenderException 🛑 (Fail-Fast)
          │
          ├──► Step 2: Send e-post (EmailDeliveryService / MailKit)
          │        ├─ Sender e-post via SMTP
          │        └─ Feiler SMTP? ────────► Kastes EmailDeliveryException 🛑 (Ekstern feil)
          │
          └──► Step 3: Feilhåndtering & DLQ
                   └─ Spesifikke exceptions fanges av MassTransit for kontrollert retry eller DLQ 🔁

```

---

## ⚠️ Unntaksstrategi (Custom Exceptions)

Tjenesten skiller skarpt mellom interne malfeil og eksterne leveransefeil:

1. **`TemplateRenderException` (Kritisk / Intern feil):**
* **Årsak:** Filen finnes ikke under `Templates/`, syntaksfeil i Scriban-malen, eller ugyldig datamodell.
* **Håndtering:** Dette er en lokal utviklings-/konfigurasjonsfeil. Prosessoren avbrytes umiddelbart (Fail-Fast) slik at feilen logges høyt i Seq og meldinger stoppes fra å gi ufullstendige e-poster.


2. **`EmailDeliveryException` (Ekstern feil):**
* **Årsak:** SMTP-server er nede, feil ved autentisering, timeout eller nettverksbrudd mot e-postleverandøren.
* **Håndtering:** Logges som advarsel/feil. Ved transient-feil vil MassTransit utføre re-forsøk (retry policy) før meldingen evt. plasseres i en buffer eller Dead-Letter Queue (DLQ).



---

## 📋 Master-oversikt over E-postmaler & Eventer

Alle eventer, consumers, prosessorer og maler er organisert under sine respektive domener:

### 🛡️ 1. AdminActions (Administrative handlinger)

| Malnavn (`TemplateName`) | Trigger-event (`Contracts.Events.AdminActions`) | Mottaker | Beskrivelse |
| --- | --- | --- | --- |
| `AdminCustomEmail` | `AdminCustomEmailRequestedEvent` | Enkeltbruker | Egendefinert e-post utsendt manuelt av admin |
| `EmailManuallyConfirmedByAdmin` | `EmailManuallyConfirmedByAdminEvent` | Bruker | Bekreftelse på at admin har bekreftet e-postadressen |
| `UserAccountDeletedByAdmin` | `UserAccountDeletedByAdminEvent` | Bruker | Varsel om at kontoen er slettet av admin |
| `UserDeletedAndBlacklistedByAdmin` | `UserDeletedAndBlacklistedByAdminEvent` | Bruker | Varsel om at kontoen er slettet og e-posten svartelistet |
| `UserLockedByAdmin` | `UserLockedByAdminEvent` | Bruker | Varsel om at kontoen har blitt sperret med begrunnelse |
| `UserUnlockedByAdmin` | `UserUnlockedByAdminEvent` | Bruker | Varsel om at kontoen har blitt gjenåpnet av admin |
| `UserUpdatedByAdmin` | `UserUpdatedByAdminEvent` | Bruker | Varsel om at profilinformasjonen er endret av admin |

---

### ⚙️ 2. SystemActions (Automatiske systemprosesser)

| Malnavn (`TemplateName`) | Trigger-event (`Contracts.Events.SystemActions`) | Mottaker | Beskrivelse |
| --- | --- | --- | --- |
| `Confirmation7DaysReminder` | `Confirmation7DaysReminderEvent` | Ubekreftet bruker | Første påminnelse om å bekrefte e-post (Dag 7) |
| `Confirmation14DaysReminder` | `Confirmation14DaysReminderEvent` | Ubekreftet bruker | Varsel om at kontoen er midlertidig sperret (Dag 14) |
| `AccountDeletedBySystem` | `AccountDeletedBySystemEvent` | Tidligere bruker | Bekreftelse på permanent sletting (+30 dager fra sperring) |

---

### 👤 3. UserActions (Brukerinitierte handlinger)

| Malnavn (`TemplateName`) | Trigger-event (`Contracts.Events.UserActions`) | Mottaker | Beskrivelse |
| --- | --- | --- | --- |
| `UserRegisteredWelcome` | `UserRegisteredEvent` | Ny bruker | Velkomstmelding med bekreftelseslenke |
| `UserRegisteredWithGoogleWelcome` | `UserRegisteredWithGoogleEvent` | Ny bruker | Velkomstmelding for sømløs Google OAuth-registrering |
| `ResendEmailConfirmation` | `ResendEmailConfirmationRequestedEvent` | Ubekreftet bruker | Manuell forespørsel om ny bekreftelseslenke |
| `PasswordResetRequested` | `PasswordResetRequestedEvent` | Bruker | Lenke for tilbakestilling av glemt passord |
| `PasswordChangedSecurityNotice` | `PasswordChangedEvent` | Bruker | Sikkerhetsvarsel om at passordet har blitt endret |
| `ContactFormAdminNotification` | `ContactFormSubmittedEvent` | Admin / Support | Varsel til support om ny henvendelse fra kontaktskjema |
| `ContactFormUserReceipt` | `ContactFormSubmittedEvent` | Avsender | Automatisk bekreftelse/kvittering på henvendelsen |
| `AccountDeletedByUser` | `AccountDeletedByUserEvent` | Bruker | Bekreftelse på at brukeren selv har slettet kontoen sin |

---

## 🧪 Lokal Testing (Mailpit)

I utviklingsmiljøet sendes ingen e-poster ut til eksterne mottakere. Tjenesten er konfigurert mot **Mailpit**:

1. Start Mailpit lokalt (f.eks. via Docker).
2. Åpne `http://localhost:8025` i nettleseren.
3. Alle e-poster utsendt fra bakgrunnstjenesten fanges opp her for inspeksjon av HTML, responsive rammer og dynamiske lenker.

---

## 🚀 Roadmap / Fremtidige Funksjoner

* [ ] **Migrering til OpenTransit:**
* Per nå benyttes MassTransit 8. Tjenesten planlegges migrert over til **OpenTransit** så snart dette rammeverket ferdigstilles og lanseres som en mer moderne og fleksibel meldingsbuss-løsning.


* [ ] **Testdekning med xUnit:**
* Implementere et dedikert testprosjekt (`Service.Tests`) basert på **xUnit**, **Shouldly** / **FluentAssertions (v6.12.2)** og **NSubstitute**.
* Enhetstester for alle `Processors` med verifisering av `TemplateRenderException` og `EmailDeliveryException`.
* Integrasjonstester for rendering av Scriban HTML-maler via `TemplateRenderService`.


* [ ] **Sosiale Funksjoner (`Social Flow`):**
* `RecipeSharedWithUserEvent` / `RecipeSharedWithNonRegisteredUserEvent`: Deling av oppskrifter med både eksisterende brukere og eksterne venner.
* `AppRecommendationSentEvent`: Vervelenker og app-invitasjoner.


* [ ] **Inaktivitetsvarsler (`Inactivity Flow`):**
* `UserInactivityWarningEvent`: Varsel til brukere som ikke har vært innlogget på 6 måneder ("Vi savner deg").
* `AccountLockedInactivityEvent`: Midlertidig sperring av kontoer etter 1 års inaktivitet før eventuell GDPR-sletting.