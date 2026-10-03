# HappyHeadlines

HappyHeadlines er et studieprojekt, der implementerer en nyhedsplatform med C#/.NET 8 og microservices. Systemet håndterer artikler, kommentarer, kladder, publicering og nyhedsbreve.

Projektets primære fokus er **skalering langs X-, Y- og Z-aksen** i en applikation opbygget af microservices. Derudover demonstrerer løsningen asynkron kommunikation, caching og overvågning.

---

## Arkitektur og skalering

| Akse | Princip | Anvendelse i projektet |
| --- | --- | --- |
| **X** | Flere identiske instanser af samme service. | ArticleService kører med tre API-instanser bag en Nginx-load balancer. |
| **Y** | Opdeling efter funktion og ansvar. | Artikler, kommentarer, tekstfiltrering, kladder, publicering og nyhedsbreve er opdelt i separate services. |
| **Z** | Opdeling af data efter eksempelvis region. | Artikler fordeles på separate PostgreSQL-databaser for Europe, Asia, Africa, NorthAmerica, SouthAmerica, Oceania, Antarctica og Global. |

Regionerne simuleres lokalt med Docker-containere. De illustrerer dataopdeling; databaserne er ikke fysisk placeret i forskellige verdensdele. Opdelingen af artikeldatabaser demonstrerer Z-aksen, mens de tre API-instanser kan tilgå alle regioner.

---

## Services

| Service/projekt | Ansvar |
| --- | --- |
| ArticleService | Oprettelse, læsning, opdatering og sletning af artikler i regionsdatabaserne. |
| article-cache-worker | Separat baggrundsproces, der opdaterer den globale artikelcache. Bruger samme kode som ArticleService i worker-tilstand. |
| CommentService | Kommentarer, kald til tekstfiltrering, kommentarcache og cachemetrics. |
| ProfanityService | Filtrering af tekst ud fra en database med forbudte ord. |
| DraftService | Oprettelse og administration af artikelkladder. |
| PublisherService | Sender publiceringsbeskeder via RabbitMQ. |
| NewsletterService | Abonnenter, mails ved publicering og daglige nyhedsbreve via API. |
| Contracts | Fælles beskedkontrakter og typer. |
| Monitoring | Fælles kode til logging og tracing. |

Ved publicering sender PublisherService en besked til RabbitMQ. ArticleService gemmer artiklen, og NewsletterService sender mails til abonnenterne. Flowet er asynkront, så et `202 Accepted`-svar betyder, at publiceringen er accepteret til behandling.

---

## Teknologier

- **C# og ASP.NET Core / .NET 8:** API’er, forretningslogik og baggrundsprocesser.
- **Entity Framework Core, Npgsql og PostgreSQL:** Dataadgang og permanent lagring.
- **Docker Compose:** Samlet opstart af services og infrastruktur.
- **Nginx:** Load balancing foran ArticleService.
- **RabbitMQ og EasyNetQ:** Asynkron beskedkommunikation.
- **Redis og StackExchange.Redis:** Fælles cache og cachetællere.
- **Lua:** Atomiske operationer i kommentarcachen.
- **Polly:** Resilience på de konfigurerede HTTP-klienter.
- **Serilog og Seq:** Struktureret logging.
- **OpenTelemetry og Zipkin:** Tracing af instrumenterede operationer.
- **Prometheus og Grafana:** Indsamling og visning af cachemetrics.
- **Mailpit:** Lokal modtagelse og visning af testmails.
- **Swagger / OpenAPI:** API-dokumentation og manuel afprøvning.

---

## Caching

### Artikelcache

Artikelcachen bruges til opslag af **globale artikler via ID**. En separat worker henter artikler fra de seneste **14 dage** og erstatter cachesættet cirka hvert **30. sekund**.

Hele sættet har en TTL på to minutter, så cachen udløber, hvis opdateringerne stopper. Artikler uden for 14-dagesvinduet udelades fra cachen, men slettes ikke fra PostgreSQL. Der er ingen fast grænse for antal artikler i artikelcachen. Ved et cache miss hentes artiklen fra databasen.

### Kommentarcache

Kommentarcachen indeholder højst **30 kommentarlister**, fordelt på kombinationen af region og artikel-ID. En liste kan indeholde nul, én eller mange kommentarer.

Ved et cache miss hentes listen fra PostgreSQL og caches i **to minutter**. Også tomme lister caches, så gentagne opslag kan give hit uden endnu et databaseopslag. Oprettelse af en kommentar invaliderer den berørte liste.

**LRU (Least Recently Used)** fjerner den mindst nyligt brugte liste, når kapaciteten overskrides. Lua-scripts udfører blandt andet opslag, tællere og LRU-opdateringer atomisk i Redis. Et versionsmærke beskytter mod, at et igangværende opslag genindsætter et resultat efter invalidering.

---

## Overvågning

Prometheus indsamler begge caches' hit/miss-tællere og kommentarcachens størrelse fra CommentService på `/metrics`. Grafana viser disse målinger i et cachedashboard.

Dashboard og datasource provisioneres fra filer under `observability/grafana`, så opsætningen følger projektet. Logs undersøges separat i konsollen eller Seq, og traces i Zipkin, hvor services er instrumenteret.

---

## Kom i gang

Installer Docker Desktop med Linux-containere. .NET 8 SDK er kun nødvendigt, hvis projektet også skal bygges uden for Docker.

Kør fra projektets rodmappe, hvor `docker-compose.yml` ligger:

```powershell
docker compose up -d --build
docker compose ps
```

Første opstart kan tage tid, mens images bygges og databaser bliver klar.

---

## Lokale adresser

| Service/værktøj | Adresse |
| --- | --- |
| ArticleService via Nginx | http://localhost:8090/swagger |
| CommentService | http://localhost:8091/swagger |
| ProfanityService | http://localhost:8092/swagger |
| DraftService | http://localhost:8093/swagger |
| PublisherService | http://localhost:8094/swagger |
| NewsletterService | http://localhost:8095/swagger |
| Grafana | http://localhost:3000 |
| Prometheus | http://localhost:9090 |
| Seq | http://localhost:5342 |
| Zipkin | http://localhost:9411 |
| RabbitMQ Management | http://localhost:15672 |
| Mailpit | http://localhost:8025 |

RabbitMQs udviklingslogin er `happyheadlines` / `happyheadlines_dev_password`. Grafanas login er `admin` / `admin`;.


