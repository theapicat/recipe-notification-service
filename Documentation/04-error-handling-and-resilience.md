# 🛡️ Feilhåndtering og resiliens

Dette dokumentet beskriver strategiene for e-postleveransefeil, ugyldige mottakeradresser,
tilstandsovervåking i minnet og den transiente MongoDB-bufferen.

## Kjerneprinsipper

1. **Fail-fast ved kritiske malfeil.** Mangler en Scriban-mal, har den syntaksfeil, eller mangler en
   påkrevd modellvariabel, kastes `TemplateRenderException` og prosessen for den meldingen avbrytes
   umiddelbart (ingen retry, ingen buffering) — se
   [03-email-templates-and-design.md](03-email-templates-and-design.md). Unntaket slipper ut av consumeren.
   Det er ikke satt opp retry/redelivery i MassTransit, så meldingen flyttes **med en gang** til køens
   error-kø (`<kønavn>_error`, f.eks. `notification-UserRegistered_error`) i RabbitMQ, og MassTransit publiserer en
   `Fault<TEvent>`. Meldingen blir altså **ikke** liggende i hovedkøen og leveres ikke på nytt av seg selv.
   Etter at malen er rettet, kan meldingene flyttes tilbake fra error-køen (f.eks. «Move messages» i
   RabbitMQ Management UI). Error-køene må derfor overvåkes; de vises ikke i admin-dashbordet.
2. **In-line gjenforsøk ved transiente SMTP-feil.** `PendingEmailService` prøver opptil **5 ganger**,
   direkte i den samme skopede prosessen, med kort ventetid mellom forsøkene (`2 * forsøksnummer`
   sekunder).
3. **Hard bounce avbryter umiddelbart.** Avviser SMTP-serveren selve **mottakeren** permanent, stoppes
   videre forsøk etter **første** forsøk, og `InvalidEmailDetectedEvent` publiseres til `recipe-auth-api`
   (`Reason` = SMTP-kode + serverens svar, f.eks. `550 5.1.1 User unknown`). Avgjørelsen tas på MailKit-unntaket
   (`SmtpCommandException`, pakket inn i `EmailDeliveryException`), **ikke** på feilteksten:
   * Hard bounce: `ErrorCode = RecipientNotAccepted` (avvist ved `RCPT TO`) **og** statuskode 5xx (f.eks. 550,
     551, 553), **unntatt** når serverens svar har utvidet kode `5.7.x` (policy, f.eks. «Relay access denied»).
   * Ikke hard bounce (vanlig retry): tilkoblings-/tidsavbruddsfeil, midlertidige 4xx-koder, og avvisning av
     selve meldingen (`MessageNotAccepted`, f.eks. spamfilter) - da kan adressen være gyldig.
   * Tidligere ble feilteksten søkt etter `550`, «does not exist» osv. Teksten inneholder mottakeradresse og
     emne, så det ga falske treff (f.eks. en timeout for `ola550@…`), og auth kunne sperre en gyldig bruker.
4. **MongoDB er kun en transient buffer.** Et dokument opprettes først når alle 5 in-line forsøk har
   feilet. Så snart e-posten leveres (ved første forsøk eller ved et senere re-forsøk fra admin), slettes
   dokumentet.
5. **Avduplisert admin-varsling.** En singleton (`NotificationStateStore`) sørger for at admin varsles
   per e-post **kun én gang** per feilperiode, ikke én gang per feilet melding.

## Komponentoversikt

```text
[ Innkommende MassTransit-event ]
              │
              ▼
    [ Processor ] ──(rendering feiler)──► TemplateRenderException  🛑 fail-fast
              │
              ▼ (rendering OK)
   [ PendingEmailService.ProcessEmailWithRetryAsync ]
              │
              ├─(hard bounce)─► Publish InvalidEmailDetectedEvent ──► recipe-auth-api
              │
              ├─ In-line retry (opptil 5 forsøk over SMTP)
              │
              └─(5/5 feilet)──► Lagre/oppdater i MongoDB [ failed_notifications ]
                                            │
                                            ▼
                               [ NotificationStateStore ]
                                            │
                             (endring fra 0 til 1 feilet e-post?)
                                    ├── JA  ──► Send Scriban-varsel til admin 📧
                                    └── NEI ──► Ingen ny e-post (admin allerede varslet)
```

### `PendingEmailService` (Scoped)

Ansvar:

* In-line gjenforsøk (opptil 5 ganger) mot `IEmailDeliveryService`.
* Hard bounce-deteksjon og publisering av `InvalidEmailDetectedEvent`.
* Ved 5 mislykkede forsøk: lagrer/oppdaterer dokumentet i MongoDB og trigger admin-varsling.
* Returnerer `Task<bool>` - `true` betyr at e-posten faktisk ble levert. Dette brukes av
  admin-retry-flyten (se under) til å avgjøre om det bufrede dokumentet skal slettes.

Admin-varselet sendes via samme Scriban-vei som alt annet (`AdminActions/PendingEmailAlert.html`), til
`SmtpSettings.AdminNotificationEmail` - **ikke** en hardkodet adresse. Mottaker, emne og feilmelding sendes
uendret til malen og HTML-escapes der av `TemplateRenderService` (som for alle maler), siden emne/feilmelding
i noen tilfeller kan stamme fra brukerkontrollert tekst (f.eks. kontaktskjema eller admin-egendefinert e-post).

### `NotificationStateStore` (Singleton)

Trådsikre, boolske flagg i minnet:

* `HasPendingNotifications` - finnes det minst ett dokument i MongoDB?
* `HasNotifiedAdmin` - er admin allerede varslet for gjeldende feilperiode?

`SetPendingStatus(false)` og `Reset()` nullstiller også `HasNotifiedAdmin`, slik at admin varsles på nytt
dersom en fremtidig feilperiode oppstår.

`StateStoreInitializerHostedService` kjører ved oppstart og setter `HasPendingNotifications` basert på
antall eksisterende dokumenter i MongoDB (`IFailedNotificationRepository.GetPendingCountAsync`).

### `FailedNotificationRepository` (Persistence)

Operasjoner mot MongoDB-samlingen `failed_notifications`: `AddAsync`, `DeleteAsync`/`DeleteManyAsync`,
`ResetRetryCountManyAsync` (nullstiller til admin-styrt re-forsøk), `MarkRetryFailedAsync` (oppdaterer et
eksisterende dokument etter et mislykket re-forsøk), `GetPendingCountAsync`, `GetAllPendingAsync`.

## Admin-gjenoppretting: retry uten å miste eller duplisere data

Når admin ber om re-forsøk (`RetryFailedNotificationCommand`, se
[05-notification-management-and-planned-work.md](05-notification-management-and-planned-work.md)), gjelder
et strengt prinsipp: **dokumentet slettes kun ved vellykket levering. Det slettes aldri automatisk bare
fordi et re-forsøk ble gjort.**

1. `RetryFailedNotificationCommandConsumer` nullstiller `RetryCount` til 0 på de valgte dokumentene
   (`ResetRetryCountManyAsync`) før noe forsøkes sendt på nytt.
2. For hvert dokument kalles `PendingEmailService.ProcessEmailWithRetryAsync(..., existingNotificationId: email.Id)`.
   `existingNotificationId` er det som skiller en admin-styrt retry fra en helt ny hendelse:
   * **Lykkes sendingen** (på forsøk 1-5): dokumentet med den gitte iden slettes fra MongoDB.
   * **Feiler alle 5 forsøk på nytt**: dokumentet **beholdes og oppdateres in-place** (`MarkRetryFailedAsync`
     - ny feilmelding, `RetryCount` og tidspunkt). Det opprettes **ikke** et nytt dokument med ny id, og det
     gamle slettes **ikke**.
   * **Hard bounce**: `InvalidEmailDetectedEvent` publiseres, og dokumentet blir liggende **uendret** til admin
     sletter det manuelt.
3. Når antall dokumenter i MongoDB når 0, tilbakestilles `NotificationStateStore`
   (`HasPendingNotifications = false`, `HasNotifiedAdmin = false`).

Uten `existingNotificationId` (dvs. for en helt ny, ubuffret e-post) oppfører `ProcessEmailWithRetryAsync`
seg som normalt: et helt nytt dokument opprettes (`AddAsync`) hvis alle 5 forsøk feiler.

## Unntakstyper

| Unntak | Kastes når | Håndtering |
| --- | --- | --- |
| `TemplateRenderException` | Manglende mal, Scriban-syntaksfeil, eller kjøretidsfeil under rendering | Ufanget - meldingen flyttes til `<kønavn>_error` i RabbitMQ (fail-fast, ingen retry). |
| `EmailDeliveryException` | SMTP-feil av enhver art (transient eller permanent) | Fanges av `PendingEmailService`, som skiller hard bounce fra transiente feil ut fra det innpakkede `SmtpCommandException` (`ErrorCode`/`StatusCode`), ikke feilteksten. |
