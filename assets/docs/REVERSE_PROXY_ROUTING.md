# Reverse proxy routing (YARP)

The **`ReverseProxy`** project ([`src/ReverseProxy`](../../src/ReverseProxy)) is an **ASP.NET + YARP** edge that listens on **port 8080** in Docker Compose (`docker-compose.override.yml` maps `8080:8080`).

**Config:** [`src/ReverseProxy/appsettings.json`](../../src/ReverseProxy/appsettings.json)

**Related:** The same topic in **MaksIT-Vault** is documented at [`REVERSE_PROXY_ROUTING.md`](../../maksit-vault/assets/docs/REVERSE_PROXY_ROUTING.md) when both repos sit side by side; otherwise open that path in your Vault clone. Vault’s table does **not** include an **ACME HTTP-01** path—Certs adds **`/.well-known/acme-challenge/`** so challenges hit the WebAPI on the **same public host** as the UI.

---

## Route table

Routes use explicit **`Order`** (lower = matched first), matching **MaksIT-Vault**, so the SPA catch-all never wins over `/api`, `/swagger`, or `/.well-known/` when JSON key order varies.

Compose service names are **`server`** (WebAPI) and **`client`** (Vite/WebUI). Cluster IDs **`webapiCluster`** / **`webuiCluster`** match Vault for a parallel mental model.

| Order | Path match | Cluster | Upstream (Compose) |
|------|------------|---------|---------------------|
| 5 | `/.well-known/acme-challenge/{**catch-all}` | `webapiCluster` | `http://server:5000/` |
| 10 | `/swagger/{**catch-all}` | `webapiCluster` | `http://server:5000/` |
| 20 | `/api/{**catch-all}` | `webapiCluster` | `http://server:5000/` |
| 1000 | `/{**catch-all}` | `webuiCluster` | `http://client:5173/` |

YARP forwards the **same path** to the destination. Example:

- Client: `POST http://localhost:8080/api/identity/login`
- Proxied to: `POST http://server:5000/api/identity/login`

Controllers use the usual **`/api/...`** prefix (e.g. `api/identity`, account and certificate flows)—there is **no** `api/vault`-style segment. Locally, the Web UI uses `public/config.js` / `.env` with `http://localhost:8080/api` so XHR calls go **through** YARP.

### HTTP-01 (Let’s Encrypt)

Traffic for **`/.well-known/acme-challenge/*`** must reach **MaksIT.CertsUI** so the HTTP-01 validator can fetch the token body from the API (backed by PostgreSQL). The dedicated route sends that path to the **`server`** service (same `webapiCluster` as `/api`).

### Kubernetes (Helm)

The chart can mount **`config.js`** from a ConfigMap (`certsClientRuntime.apiUrl`). Defaults in `values.yaml` may use a full origin (example host); you can also use a **relative** API base such as **`/api`** so the browser uses the same host and port as the page (ingress / port-forward to **8080** on the reverse-proxy Service) without hard-coding `localhost`. Use a full URL only if the UI and API are served from different origins.

---

## Automation and clients

- **Base URL** for scripts, the Agent, or any HTTP client talking to the **composed** stack should be the **proxy origin**: `http://localhost:8080` when you use Compose’s published port.
- **Path shape:** Call **`/api/...`** on that origin (either concatenate `BaseAddress` + `api/...` or set `VITE_API_URL` / runtime `API_URL` to `http://localhost:8080/api` so the client already includes `/api`).
- **YARP** forwards request headers to the WebAPI by default (including **`Authorization: Bearer`** for JWT). No special transform is required unless you customize YARP transforms.

---

## Direct vs proxied ports (local dev)

| Scenario | Typical base URL for Certs HTTP API |
|----------|-------------------------------------|
| Docker Compose (this repo) | `http://localhost:8080` (through YARP) |
| Run **MaksIT.CertsUI** only (F5 / `dotnet run`) | See `launchSettings.json` (e.g. `http://localhost:5016`) — **no** YARP |
| Run **ReverseProxy** only (outside Compose) | `launchSettings`: e.g. `http://localhost:5276` — cluster addresses in `appsettings.json` must resolve (Compose service names only work **inside** the Compose network) |

If authentication succeeds but API calls fail, confirm traffic reaches the **same** WebAPI instance and data volume you expect (not a different port or stale container).

---

## Related files

- [`src/docker-compose.yml`](../../src/docker-compose.yml), [`src/docker-compose.override.yml`](../../src/docker-compose.override.yml)
- [`src/ReverseProxy/Program.cs`](../../src/ReverseProxy/Program.cs)
- WebUI runtime API base: [`src/MaksIT.WebUI/public/config.js`](../../src/MaksIT.WebUI/public/config.js), Helm `certsClientRuntime.apiUrl` in [`src/helm/values.yaml`](../../src/helm/values.yaml)
