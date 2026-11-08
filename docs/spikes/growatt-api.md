# Spike: Growatt API Validation

**Date:** 2026-11-08
**Status:** Viable with caveats
**Task:** 2.1

---

## Veredicto

**VIABLE.** La API no oficial de Growatt es ampliamente usada por la comunidad (Home Assistant, Python packages, Node.js libs). Funciona con usuario/contraseña de ShinePhone. Las tareas 2.4 y 2.5 pueden proceder con el diseño original.

**Advertencia crítica:** Growatt puede lockear cuentas si se hacen demasiadas requests. Nuestro intervalo de 1 hora es seguro (la comunidad recomienda mínimo 5 min; 1 hora es muy conservador).

---

## Auth Flow

**Endpoint:** `POST https://server.growatt.com/newTwoLoginAPI.do`

**Request:** `Content-Type: application/x-www-form-urlencoded`

```
account=tu_email@ejemplo.com&password=tu_contraseña_md5_o_sha256
```

**Nota:** La contraseña se envía como hash. Los clientes conocidos usan MD5 del password. Algunos endpoints más nuevos usan SHA-256.

**Response (200 OK):**

```json
{
  "result": 1,
  "msg": "SUCCESS",
  "user": {
    "id": "123456",
    "account": "tu_email@ejemplo.com"
  }
}
```

La sesión se mantiene via **cookie de sesión** (`Set-Cookie: JSESSIONID=...`). El `HttpClient` de .NET debe configurarse con `CookieContainer` para mantenerla entre requests.

---

## Endpoints relevantes

### 1. Lista de plantas

```
POST https://server.growatt.com/index/getUserPlantList
```

Respuesta contiene array de plantas con `plantId`, `plantName`.

### 2. Lista de inversores por planta

```
POST https://server.growatt.com/device/getDevicesByPlantList
Body: plantId=<id>&currPage=1
```

Retorna array con `sn` (serial number), `alias`, `status`.

### 3. Datos del inversor (el más importante)

```
POST https://server.growatt.com/panel/getDevicesByPlant  (mix system)
-- o --
GET https://server.growatt.com/newTwoPlantAPI.do?op=getAllDeviceList&plantId=<id>
```

Para inversores estándar (no Mix/Storage), el endpoint de datos en tiempo real es:

```
POST https://server.growatt.com/device/getInverterData
Body: op=getData&id=<inverter_sn>&type=1
```

---

## Campos mapeados a `solar_readings`

| Campo Growatt | Nuestro campo | Tipo | Notas |
|---------------|---------------|------|-------|
| `pac` | `power_w` | decimal | Potencia instantánea en W |
| `etoday` | `energy_today_kwh` | decimal | Energía del día en kWh |
| `etotal` | `energy_total_kwh` | decimal | Energía total acumulada en kWh |
| `status` | `status` | int | 1=normal, 0=offline/fault |

---

## Consideraciones de implementación

### C# HttpClient setup

```csharp
var handler = new HttpClientHandler
{
    CookieContainer = new CookieContainer(),
    UseCookies = true
};
var client = new HttpClient(handler) { BaseAddress = new Uri("https://server.growatt.com") };
```

### Password hashing

Growatt usa MD5 del password (en minúsculas) para el login:

```csharp
using var md5 = MD5.Create();
var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
var passwordHash = Convert.ToHexString(hash).ToLower();
```

### Rate limiting / Account safety

- Growatt puede lockear cuentas con >12 requests/hora aprox.
- Nuestro `PeriodicTimer` cada hora hace **1 request de login + 1 de datos = 2/hora** — completamente seguro.
- Si el login devuelve `result: -1` o `msg: "account locked"`, el sync service debe pausar 24h y alertar en logs.

### Servidores regionales

| Región | URL |
|--------|-----|
| Global (default) | `https://server.growatt.com` |
| China | `https://openapi-cn.growatt.com` |
| Norteamérica | `https://openapi-us.growatt.com` |

España usa el servidor global. Configurar como variable de entorno opcional (`GROWATT_SERVER_URL`) con default `https://server.growatt.com`.

---

## Fuentes

- [PyPi growattServer](https://pypi.org/project/growattServer/) — librería Python de referencia
- [indykoning/PyPi_GrowattServer](https://github.com/indykoning/PyPi_GrowattServer/blob/master/docs/openapiv1.md) — documentación de endpoints
- [Home Assistant Growatt integration](https://github.com/home-assistant/core/commit/a4fd991ab557834dd484e30570251f32e47abfd3) — implementación de producción
- [Community discussion: account locking](https://community.home-assistant.io/t/growatt-shinephone-account-locked-after-enabling-growatt-integration-shinewilan-x2-haos-need-safe-polling-strategy/1007887) — advertencia de rate limiting
