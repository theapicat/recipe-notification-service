# 📋 Sjekkliste: Legge til ny e-postnotifikasjon

Hurtigguide for utvidelse av e-postutsendelser i **Recipe Notification Service**.

---

### 🔲 1. Kontrakt (`Contracts/`)

* [ ] Opprett event-klasse under `Contracts/Events/{DomainActions}/{EventName}.cs`.
* [ ] Inkluder kun nødvendige felt (`UserId`, `Email`, `Name`, unike lenker, datoer).

### 🔲 2. Publisering (Kildetjeneste)

* [ ] Injiser `IPublishEndpoint` i utløsende tjeneste (f.eks. Quartz Job/Controller i `recipe-auth-api`).
* [ ] Publiser eventet på meldingsbussen ved utløst handling.

### 🔲 3. HTML-mal (`Infrastructure/TemplateService/Templates/`)

Header, footer, kokkelue-logo og selve HTML-skallet ligger i den delte layouten
`Templates/_Layout.html` og skal **ikke** kopieres inn i den nye malen. Hver mal inneholder kun
`<title>`-verdi + selve innholdsblokken:

* [ ] Opprett `Templates/{DomainActions}/{TemplateName}.html` med følgende oppskrift:
  ```
  {{ title = "Tittel som vises i <title>-taggen" }}
  {{ capture content }}
      <h1 style="...">Overskrift</h1>
      <p style="...">Innhold med {{ variabler }}</p>
  {{ end }}
  {{ include "_Layout" }}
  ```
* [ ] Skal footeren avvike fra standard ("Med vennlig hilsen, Kjøkkenhylla-teamet"), sett
  `{{ footer_note = "..." }}` før `{{ capture content }}`.
* [ ] Følg designsystemet i selve innholdsblokken:
* Primærknapp: `#4a6b53`, sentrert (`align="center"`, `margin: 24px auto`).
* Varselboks (v/ tidsfrister): `#f9ebe6` bakgrunn, `#c86a4b` ramme.


* [ ] Verifiser at `.csproj` har `Copy to Output Directory: Copy if newer` (dekkes normalt automatisk av wildcard-mønsteret `Templates\**\*.html`).

### 🔲 4. Prosessor (`Infrastructure/Processors/`)

Det finnes ett felles grensesnitt for alle prosessorer, `IEventProcessor<TEvent>`
(`Infrastructure/Processors/Interfaces/IEventProcessor.cs`) — opprett **ikke** et nytt
`I{EventName}Processor`-grensesnitt per event.

* [ ] Opprett prosessorklasse i `{DomainActions}/{EventName}Processor.cs` som implementerer
  `IEventProcessor<{EventName}Event>`.
* [ ] Bygg anonym modell for Scriban-variablene.
* [ ] Rendre mal med `ITemplateRenderService.RenderTemplateAsync("{DomainActions}/{TemplateName}", model)`.
* [ ] Send via `IPendingEmailService.ProcessEmailWithRetryAsync(...)` med `nameof(EventName)`.

### 🔲 5. Consumer (`Service/Consumers/`)

* [ ] Opprett MassTransit Consumer i `Consumers/{DomainActions}/{EventName}Consumer.cs` som tar inn
  `IEventProcessor<{EventName}Event> processor` i konstruktøren.
* [ ] Implementer `IConsumer<TEvent>` og kalle tilhørende prosessor i `Consume`.

### 🔲 6. Registrering i DI (`Service/Extensions/`)

* [ ] Registrer prosessoren som **`Transient`** under sin domeneseksjon i `MailProcessorExtensions.cs`:
  `services.AddTransient<IEventProcessor<{EventName}Event>, {EventName}Processor>();`
* [ ] Registrer consumer under sin domeneseksjon i `MassTransitExtensions.cs` (`x.AddConsumer<...>()`).

### 🔲 7. Testing (`Tests/`)

* [ ] **Processor Unit Test:** Verifiser at `ITemplateRenderService` kalles med korrekt stibane/modell, og at `IPendingEmailService` kalles med riktige parametere.
* [ ] **Template Integration Test:** Verifiser at Scriban-motoren kompilerer den nye `.html`-filen fra disk uten feil.
* [ ] **Consumer Component Test:** Verifiser at `TestHarness` fanger opp eventet og utløser prosessoren.