# DUS Temperature — starter projekat (V0)

Ovo je "starter kit" za projekat iz Distribuiranih upravljačkih sistema:
- 10 klijenata (5 active + 5 standby, failover preko heartbeat-a)
- alarm prioritet 0/1/2/3 (klijent boji ispis, server loguje u bazu)
- konsenzus (svakih 60s server upisuje consensus vrednost)
- bezbedna komunikacija: AES enkripcija payload-a + RSA potpis + anti-replay (timestamp + MessageId)

> Napomena: V0 koristi **pre-shared client public keys** na serveru (najbrže da odmah radi).
> Register handshake možete dodati kasnije.

---

## 1) Kako da pokrenete

### A) Pokreni server (DUS.Server)
1. Build solution (Restore NuGet).
2. Start `DUS.Server`.
3. Server kreira `keys\server.private.xml` i `keys\server.private.public.xml` u output folderu (bin\Debug ili bin\Release).
4. Kopiraj `keys\server.private.public.xml` kao `server.public.xml` u:
   - `DUS.SensorClient\bin\Debug\keys\server.public.xml`
   - `DUS.ReportClient\bin\Debug\keys\server.public.xml`

### B) Pokreni klijente (DUS.SensorClient) — 10 instanci
Primer:
```
DUS.SensorClient.exe --clientId C01 --sensorId 1
```

Kad se klijent pokrene, napravi:
- `keys\C01.private.xml`
- `keys\C01.public.xml`

**Sada moraš da kopiraš public key na server:**
- iz `DUS.SensorClient\bin\Debug\keys\C01.public.xml`
- u `DUS.Server\bin\Debug\keys\clients\C01.public.xml`

Ponovi za sve klijente C01..C10.

> Komande u klijentu: upiši `sleep` (20s) ili `crash` i ENTER.

### C) Report klijent (DUS.ReportClient)
```
DUS.ReportClient.exe --clientId REPORT --fromMin 60
```
I za REPORT isto važi public key copy (REPORT.public.xml) ako server traži verifikaciju report zahteva.

---

## 2) Baza podataka + Entity Framework (EF6)

Server koristi **SQL Server LocalDB** (ne treba pgAdmin).
Connection string je u `DUS.Server/App.config`:
```
Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=DusTemperatureDb;Integrated Security=True;
```

Prvi put kad server upiše nešto u bazu, EF će automatski:
- kreirati bazu `DusTemperatureDb`
- napraviti tabelu `MeasurementEntities`

Kako da vidiš bazu:
- Visual Studio: *View → SQL Server Object Explorer* i nađi `(localdb)\MSSQLLocalDB`
- ili instaliraj **SQL Server Management Studio (SSMS)** i konektuj se na `(localdb)\MSSQLLocalDB`

---

## 3) Ako baš hoćete PostgreSQL + pgAdmin (nije preporuka za V0)
pgAdmin je alat za PostgreSQL, ali onda morate menjati:
- EF provider (Npgsql) i konfiguraciju
- (poželjno) preći na EF Core (više posla)

Za predmet je LocalDB/SQL Server najjednostavniji put.

---

## 4) Gde da proširite (šta dodati posle V0)
- pravi `Register` handshake (da server primi public key bez ručnog kopiranja)
- strogo dodeljivanje boja i uloga (active/standby) po specifikaciji
- lepši report (filter po prioritetu, prikaz po senzoru)
- test napada: replay poruke i loš potpis (server treba da odbije)
