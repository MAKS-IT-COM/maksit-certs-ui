# MaksIT.CertsUI – Modern container-native ACME client with a full WebUI experience

![Line Coverage](assets/badges/coverage-lines.svg) ![Branch Coverage](assets/badges/coverage-branches.svg) ![Method Coverage](assets/badges/coverage-methods.svg)

MaksIT.CertsUI is a powerful, container-native ACMEv2 client built to simplify and automate the entire lifecycle of HTTPS certificates issued by Let’s Encrypt. It is an independent, unofficial project and is not affiliated with or endorsed by Let’s Encrypt or ISRG.

Designed for modern infrastructure, it combines a robust WebAPI, intuitive WebUI, and lightweight edge Agent to deliver fully automated certificate issuance, renewal, and deployment across Docker, Podman, and Kubernetes environments. MaksIT.CertsUI supports the HTTP-01 challenge and follows the official [Let’s Encrypt guidelines](https://letsencrypt.org/docs/) while implementing recommended security and operational best practices.

---


If you find this project useful, please consider supporting its development:

[<img src="https://cdn.buymeacoffee.com/buttons/v2/default-blue.png" alt="Buy Me A Coffee" style="height: 60px; width: 217px;">](https://www.buymeacoffee.com/maksitcom)


---

## Table of Contents

- [MaksIT.CertsUI – Modern container-native ACME client with a full WebUI experience](#maksitcertsui--modern-container-native-acme-client-with-a-full-webui-experience)
  - [Table of Contents](#table-of-contents)
  - [Changelog](#changelog)
  - [Contributing](#contributing)
  - [Patch and delta reference](#patch-and-delta-reference)
  - [Login and refresh token architecture](#login-and-refresh-token-architecture)
  - [Reverse proxy routing (YARP)](#reverse-proxy-routing-yarp)
  - [High availability architecture](#high-availability-architecture)
  - [Architecture](#architecture)
    - [Current Limitations](#current-limitations)
    - [Architecture Scheme](#architecture-scheme)
    - [Architecture Description](#architecture-description)
      - [MaksIT.CertsUI Agent](#maksitcertsui-agent)
      - [MaksIT.CertsUI WebUI](#maksitcertsui-webui)
      - [MaksIT.CertsUI WebAPI](#maksitcertsui-webapi)
      - [Flow Overview](#flow-overview)
  - [MaksIT.CertsUI Server Installation on Linux with Podman Compose](#maksitcertsui-server-installation-on-linux-with-podman-compose)
    - [Prerequisites](#prerequisites)
    - [Running the Project with Podman Compose](#running-the-project-with-podman-compose)
  - [MaksIT.CertsUI Server Installation on Windows with Docker Compose](#maksitcertsui-server-installation-on-windows-with-docker-compose)
    - [Prerequisites](#prerequisites-1)
    - [Secrets and Configuration](#secrets-and-configuration)
    - [Running the Project with Docker Compose](#running-the-project-with-docker-compose)
  - [MaksIT.CertsUI Server installation on Kubernetes](#maksitcertsui-server-installation-on-kubernetes)
    - [2. Prepare Namespace, Secrets, and ConfigMap](#2-prepare-namespace-secrets-and-configmap)
    - [3. Create a Minimal Custom Values File](#3-create-a-minimal-custom-values-file)
    - [4. Install the Helm Chart](#4-install-the-helm-chart)
    - [5. Uninstall the Helm Chart](#5-uninstall-the-helm-chart)
  - [Run E2E Against k3s Ingress (PowerShell)](#run-e2e-against-k3s-ingress-powershell)
  - [MaksIT.CertsUI Interface Overview](#maksitcertsui-interface-overview)
  - [Contact](#contact)


## Changelog

Version history and release notes live in [CHANGELOG.md](CHANGELOG.md).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for setup, pull request expectations, and security reporting.

## Patch and delta reference

How PATCH payloads (deltas) are built and applied is documented in **[assets/docs/PATCH_DELTA_REFERENCE.md](assets/docs/PATCH_DELTA_REFERENCE.md)**. It matches the **MaksIT.Core** contract; this repo focuses on **account** PATCH and **`hostnames`** in the WebUI.

- [TL;DR (start here)](assets/docs/PATCH_DELTA_REFERENCE.md#tldr-start-here)
- [1. Core contract (MaksIT.Core)](assets/docs/PATCH_DELTA_REFERENCE.md#1-core-contract-maksitcore)
- [2. Backend (BE) rules](assets/docs/PATCH_DELTA_REFERENCE.md#2-backend-be-rules)
- [3. Frontend (FE) rules](assets/docs/PATCH_DELTA_REFERENCE.md#3-frontend-fe-rules)
- [4. Payload examples](assets/docs/PATCH_DELTA_REFERENCE.md#4-payload-examples)
- [5. Quick reference](assets/docs/PATCH_DELTA_REFERENCE.md#5-quick-reference)
- [6. Related docs](assets/docs/PATCH_DELTA_REFERENCE.md#6-related-docs)
- [7. Current implementation vs reference](assets/docs/PATCH_DELTA_REFERENCE.md#7-current-implementation-vs-reference-maksit-certsui)

## Login and refresh token architecture

How login, JWT access tokens, refresh tokens, axios interceptors, and logout interact is documented in **[assets/docs/LOGIN_AND_REFRESH_TOKEN_ARCHITECTURE.md](assets/docs/LOGIN_AND_REFRESH_TOKEN_ARCHITECTURE.md)**. **Certs WebAPI** persists users in PostgreSQL; **2FA** follows whatever this repo’s backend and WebUI implement (shared models may carry optional fields).

- [1. Overview](assets/docs/LOGIN_AND_REFRESH_TOKEN_ARCHITECTURE.md#1-overview)
- [2. Token model](assets/docs/LOGIN_AND_REFRESH_TOKEN_ARCHITECTURE.md#2-token-model)
- [3. Backend flow](assets/docs/LOGIN_AND_REFRESH_TOKEN_ARCHITECTURE.md#3-backend-flow)
- [4. Frontend flow](assets/docs/LOGIN_AND_REFRESH_TOKEN_ARCHITECTURE.md#4-frontend-flow)
- [5. API summary](assets/docs/LOGIN_AND_REFRESH_TOKEN_ARCHITECTURE.md#5-api-summary)
- [6. Sequence overview](assets/docs/LOGIN_AND_REFRESH_TOKEN_ARCHITECTURE.md#6-sequence-overview)
- [7. Security notes](assets/docs/LOGIN_AND_REFRESH_TOKEN_ARCHITECTURE.md#7-security-notes)
- [8. Key files reference](assets/docs/LOGIN_AND_REFRESH_TOKEN_ARCHITECTURE.md#8-key-files-reference)

## Reverse proxy routing (YARP)

How the **YARP** edge splits **ACME challenge**, **Swagger**, **WebAPI**, and **WebUI** traffic is documented in **[assets/docs/REVERSE_PROXY_ROUTING.md](assets/docs/REVERSE_PROXY_ROUTING.md)**, including **`/.well-known/acme-challenge/`** for HTTP-01.

- [Route table](assets/docs/REVERSE_PROXY_ROUTING.md#route-table)
- [HTTP-01 (Let’s Encrypt)](assets/docs/REVERSE_PROXY_ROUTING.md#http-01-lets-encrypt)
- [Kubernetes (Helm)](assets/docs/REVERSE_PROXY_ROUTING.md#kubernetes-helm)
- [Automation and clients](assets/docs/REVERSE_PROXY_ROUTING.md#automation-and-clients)
- [Direct vs proxied ports (local dev)](assets/docs/REVERSE_PROXY_ROUTING.md#direct-vs-proxied-ports-local-dev)
- [Related files](assets/docs/REVERSE_PROXY_ROUTING.md#related-files)

## High availability architecture

High-availability behavior for ACME coordination, challenge coherence, leases, background services, and Kubernetes probes is documented in **[assets/docs/HA_ARCHITECTURE.md](assets/docs/HA_ARCHITECTURE.md)**.

- [Goals and runtime model](assets/docs/HA_ARCHITECTURE.md#goals)
- [Lease design](assets/docs/HA_ARCHITECTURE.md#lease-design)
- [HTTP-01 coherence design](assets/docs/HA_ARCHITECTURE.md#http-01-coherence-design)
- [Kubernetes behavior](assets/docs/HA_ARCHITECTURE.md#kubernetes-behavior)
- [Files involved](assets/docs/HA_ARCHITECTURE.md#files-involved)

---

## Architecture

This solution provides automated, secure management of Let's Encrypt certificates for environments where the edge proxy is behind NAT and certificate management logic runs in a Docker/Podman compose or Kubernetes cluster.


### Current Limitations


- **Single Agent Support:**  
  The current implementation supports only a single MaksIT.CertsUI Agent instance. Multi-agent edge deployments are not supported at this time.

- **HTTP-01 Challenge Only:**  
  Only the HTTP-01 ACME challenge type is supported. DNS-01 and other challenge types are not implemented.

- **Optional Dedicated Worker Split:**  
  ACME write coordination is implemented through PostgreSQL runtime leases in the main server process. A dedicated ACME worker deployment/chart split is optional and not implemented in this repository yet.

The server component supports HA replicas with DB-backed coordination. See [High availability architecture](#high-availability-architecture) for details.

### Architecture Scheme

```
                             Edge Proxy (NAT)                 Kubernetes Cluster (Internal Network)
                             +-----------------------+        +----------------------------------------------------+
                             |                       |  HTTP  |  +-----------------------+     +----------------+  |
+-------------------+        |     Reverse Proxy-----|--------|->|                       |     |                |  |
|                   |  ACME  |    (HAProxy/Nginx)    |        |  | MaksIT.CertsUI Server |<--->|  MaksIT.WebUI  |  |
|   Let's Encrypt   |<------>|                       |  HTTP  |  |     (ACME logic)      |     |  (Management)  |  |
|    (Internet)     |  HTTP  | MaksIT.CertsUI Agent<-|--------|--|                       |     |                |  |
|                   |        |    (Edge WebAPI,      |        |  +-----------------------+     +----------------+  |
+-------------------+        |   next to HAProxy)    |        +----------------------------------------------------+
                             |                       |
                             +-----------------------+
                                         | 
                                         | 
                                         v 
                             +-----------------------+
                             |                       |
                             |    Application(s)     |
                             |                       |
                             +-----------------------+
```

### Architecture Description


#### MaksIT.CertsUI Agent

The **MaksIT.CertsUI Agent** is a lightweight service responsible for **receiving cached certificates** from the **MaksIT.CertsUI** server and **deploying them to the local file system** used by your reverse proxy (e.g., **HAProxy** or **Nginx**). It also handles **proxy service reloads** to activate new certificates.

Check **Agent** repository for more details and installation instructions: [MaksIT.CertsUI Agent](https://github.com/MAKS-IT-COM/maksit-certs-ui-agent.git)

#### MaksIT.CertsUI WebUI

The **MaksIT.CertsUI WebUI** is a user-friendly web interface designed to simplify the management of Let's Encrypt certificates within your infrastructure. With the WebUI, administrators can easily view, import, and export the certificate cache, streamlining certificate operations without the need for direct command-line interaction.

**Key Features:**

- **Certificate Management Dashboard:**  
  Provides a clear overview of all managed certificates, their status, and expiration dates.

- **Import/Export Certificate Cache:**  
  Easily back up or restore your certificate cache. This feature allows you to move certificates between environments or recover from failures without reissuing certificates from Let's Encrypt.

- **Redeploy Cached Certificates:**  
  Instantly redeploy existing (cached) certificates to your edge agents or proxies. This avoids unnecessary requests to Let's Encrypt, reducing rate limit concerns and ensuring rapid recovery or migration.

- **No Need for Reissuance:**  
  By leveraging the cache import/export and redeploy features, you can restore or migrate certificates without triggering new ACME challenges or consuming additional Let's Encrypt issuance quotas.

- **Secure Access:**  
  The WebUI is accessible only via username and password authentication, ensuring that only authorized users can manage certificates.

The WebUI is designed for operational efficiency and security, making certificate lifecycle management straightforward for both small and large deployments.


#### MaksIT.CertsUI WebAPI

The **MaksIT.CertsUI Webapi** is the core backend service responsible for orchestrating all certificate management operations in the MaksIT.CertsUI solution. It implements the ACME protocol to interact with Let's Encrypt, handles HTTP-01 challenges, manages the certificate cache, and coordinates certificate deployment to edge agents.

**Main Responsibilities:**

- **ACME Protocol Handling:**  
  Communicates with Let's Encrypt to request, renew, and revoke certificates using the official ACME protocol.

- **Challenge Management:**  
  Receives and responds to HTTP-01 challenge requests, enabling domain validation even when the edge proxy is behind NAT.

- **Certificate Cache Management:**  
  Stores issued certificates securely and provides endpoints for importing, exporting, and redeploying cached certificates.

- **Agent Coordination:**  
  Sends certificates to edge agents (such as the MaksIT.CertsUI Agent) and instructs them to reload or restart the proxy service to activate new certificates.

- **API Security:**  
  All API endpoints are protected by authentication mechanisms (such as API keys), ensuring that only authorized agents and users can perform sensitive operations.

- **Integration with WebUI:**  
  Serves as the backend for the MaksIT.CertsUI WebUI, enabling administrators to manage certificates through a secure and intuitive web interface.

The Webapi is designed for deployment in secure environments such as Kubernetes clusters, centralizing certificate management while keeping HTTPS termination and certificate storage at the network edge.

#### Flow Overview

1. **ACME Challenge Routing:**
 - The Edge Proxy (HAProxy/Nginx), running behind NAT, listens for HTTP requests on port80.
 - Requests to `/.well-known/acme-challenge/` are forwarded via HTTP to the LetsEncrypt Client running in the Kubernetes cluster.

2. **LetsEncrypt Client (Kubernetes):**
 - Handles ACME challenge requests, responds to Let's Encrypt for domain validation, and manages certificate issuance.
 - The Web UI provides management for certificate operations.

3. **Certificate Deployment:**
 - After successful validation and certificate issuance, the LetsEncrypt Client sends the new certificates to the Agent (Edge WebAPI) running on the same machine as the Edge Proxy.
 - The Agent stores the certificates in the directory used by the proxy (e.g., `/etc/haproxy/certs`).

4. **Proxy Reload:**
 - The LetsEncrypt Client instructs the Agent to reload or restart the Edge Proxy, ensuring the new certificates are used for HTTPS traffic.

5. **HTTPS Serving:**
 - The Edge Proxy serves HTTPS traffic to backend applications using the updated certificates.


**Key Points:**
- The Edge Proxy and Agent are deployed together on the edge server, while the LetsEncrypt Client and Web UI run in Kubernetes.
- All ACME challenge and certificate management logic is centralized in Kubernetes, while certificate storage and HTTPS termination remain at the edge.
- This architecture supports secure automation even when the edge server is not directly accessible from the public internet.


> **Note:** Currently, only HTTP-01 challenges are supported. The server supports multi-replica Kubernetes deployments with PostgreSQL-backed coordination.


---

## MaksIT.CertsUI Server Installation on Linux with Podman Compose

Podman Compose usage to orchestrate multiple **MaksIT.CertsUI** services on Linux.

### Prerequisites

- [Podman](https://podman.io/getting-started/installation)

- sudo dnf install podman-compose -y



- Create these folders:
  - `/opt/Compose/MaksIT.CertsUI/acme`
  - `/opt/Compose/MaksIT.CertsUI/data`
  - `/opt/Compose/MaksIT.CertsUI/tmp`
  - `/opt/Compose/MaksIT.CertsUI/configMap`
  - `/opt/Compose/MaksIT.CertsUI/secrets`
  - `/opt/Compose/MaksIT.CertsUI/client`

Bash command to use:

```bash
sudo mkdir -p /opt/Compose/MaksIT.CertsUI/acme \
  /opt/Compose/MaksIT.CertsUI/data \
  /opt/Compose/MaksIT.CertsUI/tmp \
  /opt/Compose/MaksIT.CertsUI/configMap \
  /opt/Compose/MaksIT.CertsUI/secrets \
  /opt/Compose/MaksIT.CertsUI/client
```

Create the following files in the appropriate folders:

**1. Create the file `/opt/Compose/MaksIT.CertsUI/secrets/appsecrets.json` with this command:**

```bash
sudo tee /opt/Compose/MaksIT.CertsUI/secrets/appsecrets.json > /dev/null <<EOF
{
  "Configuration": {
    "CertsEngineConfiguration": {
      "ConnectionString": "Host=postgres;Port=5432;Database=certsui;Username=certsui;Password=certsui;SslMode=Prefer"
    },
    "Auth": {
      "Secret": "<your-auth-secret>",
      "Pepper": "<your-pepper>"
    },
    "Agent": {
      "AgentKey": "<your-agent-key>"
    }
  }
}
EOF
```

**Note:**  
PostgreSQL is configured as **`Configuration:CertsEngineConfiguration:ConnectionString`**. For Docker Compose, use the Postgres service hostname (here **`postgres`**) and credentials that match **`docker-compose.override.yml`** (**`certsui`** / **`certsui`** / database **`certsui`** by default). The host also accepts legacy **`ConnectionStrings:Certs`** if needed. Replace placeholder values `<your-auth-secret>`, `<your-pepper>`, `<your-agent-key>`, with secure, your environment-specific values.
Make sure `<your-agent-key>` matches the key configured in your agent deployment.

**2. Create the file  `/opt/Compose/MaksIT.CertsUI/configMap/appsettings.json` with this command:**

```bash
sudo tee /opt/Compose/MaksIT.CertsUI/configMap/appsettings.json <<EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Configuration": {
    "Auth": {
      "Issuer": "<your-issuer>",
      "Audience": "<your-audience>",
      "Expiration": 15,
      "RefreshExpiration": 180
    },
    "Agent": {
      "AgentHostname": "http://<your-agent-hostname>",
      "AgentPort": 5000,
      "ServiceToReload": "haproxy"
    },
    "Production": "https://acme-v02.api.letsencrypt.org/directory",
    "Staging": "https://acme-staging-v02.api.letsencrypt.org/directory",
  }
}
EOF
```

**Note:**  
ACME sessions, HTTP-01 challenges, Terms of Service caching, and registration data live in PostgreSQL. Replace all JWT-related placeholder values `<your-issuer>`, `<your-audience>` and `<your-agent-hostname>` with your environment-specific values.

**3. Create the file `/opt/Compose/MaksIT.CertsUI/client/config.js` with this command:**

```bash
sudo tee /opt/Compose/MaksIT.CertsUI/client/config.js <<EOF
window.RUNTIME_CONFIG = {
  API_URL: "http://<your-server-hostname>/api"
};
EOF
```

**Note:**  
  - Replace placeholder value `<your-server-hostname>` to tell the client where **MaksIT.CertsUI** server is running

### Running the Project with Podman Compose

In the project root (`/opt/Compose/MaksIT.CertsUI`), create a new file named `docker-compose.yml` with the following content:

```bash
sudo tee /opt/Compose/MaksIT.CertsUI/docker-compose.yml <<EOF
services:
  reverseproxy:
    image: cr.maks-it.com/certs-ui/reverseproxy:latest
    container_name: reverseproxy
    ports:
      - "8080:8080"
    depends_on:
      - client
      - server
    networks:
      - certs-ui-network

  client:
    image: cr.maks-it.com/certs-ui/client:latest
    container_name: certs-ui-client
    volumes:
    - /opt/Compose/MaksIT.CertsUI/client/config.js:/app/dist/config.js:ro
    networks:
      - certs-ui-network

  server:
    image: cr.maks-it.com/certs-ui/server:latest
    container_name: certs-ui-server
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_HTTP_PORTS=5000
    volumes:
      - /opt/Compose/MaksIT.CertsUI/acme:/acme
      - /opt/Compose/MaksIT.CertsUI/data:/data
      - /opt/Compose/MaksIT.CertsUI/tmp:/tmp
      - /opt/Compose/MaksIT.CertsUI/configMap/appsettings.json:/configMap/appsettings.json:ro
      - /opt/Compose/MaksIT.CertsUI/secrets/appsecrets.json:/secrets/appsecrets.json:ro
    networks:
      - certs-ui-network

networks:
  certs-ui-network:
    driver: bridge
EOF
```

**Note:**  
  - Adjust volume paths if changed

**1. Run Podman compose in Rootfull mode (The only supported by podman-compose):**

```bash
sudo chown -R 1654:1654 /opt/Compose/MaksIT.CertsUI/{data,acme}
sudo chmod -R 775 /opt/Compose/MaksIT.CertsUI/{data,acme}

sudo chown -R 1654:1654 /opt/Compose/MaksIT.CertsUI/tmp
sudo chmod 1777 /opt/Compose/MaksIT.CertsUI/tmp

sudo su -
sudo bash -c 'echo "export PATH=/usr/local/bin:/usr/local/sbin:\$PATH" >> /root/.bashrc'

exit
sudo su -

podman compose -f docker-compose.yml up --build
```

**2. Run Podman compose in Rootless mode (Not supported by podman-compose on Alma10, havent tested):**

Correct UID and GID for `app` user inside container:

```bash
[root@test-podman maksym]# podman exec certs-ui-server id -u app
1654
[root@test-podman maksym]# podman exec certs-ui-server id -g app
1654
```

Then you have to find your `subuid` and `subgid` ranges:

```bash
[<youruser>@<yourdomain> ~]$ grep $(whoami) /etc/subuid
<youruser>:524288:65536
[<youruser>@<yourdomain> ~]$ grep $(whoami) /etc/subgid
<youruser>:524288:65536
```

Calculate host UID and GID that maps to container's `app

```
host_uid = subuid_start + container_uid
         = 524288 + 1654
         = 525942

host_gid = 525942
```

Apply correct ownership and permissions to the volumes:

```bash
sudo chown -R 525942:525942 /opt/Compose/MaksIT.CertsUI/{data,acme}
sudo chmod -R 775 /opt/Compose/MaksIT.CertsUI/{data,acme}

sudo chown -R 525942:525942 /opt/Compose/MaksIT.CertsUI/tmp
sudo chmod 1777 /opt/Compose/MaksIT.CertsUI/tmp
```

Then run podman compose as normal user:

```bash
podman compose -f docker-compose.yml up --build
```

This command builds and starts the following services:
- **reverseproxy**: YARP edge on port 8080; routes `/api`, `/.well-known/`, and the SPA to **`server`** / **`client`** (same layout as `src/docker-compose.yml` in this repo).
- **client**: WebUI (Vite) — Compose service name used by YARP (`http://client:5173/` inside the stack).
- **server**: WebAPI — Compose service name used by YARP (`http://server:5000/` inside the stack).

**Stop the services:**

Press `Ctrl+C` in the terminal, then run:

```bash
podman compose -f docker-compose.yml down
```


---

## MaksIT.CertsUI Server Installation on Windows with Docker Compose

Use Docker Compose to orchestrate multiple **MaksIT.CertsUI** services on Windows.

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (includes Docker Compose)
- Create these folders:
  - `C:\Compose\MaksIT.CertsUI\acme`
  - `C:\Compose\MaksIT.CertsUI\data`
  - `C:\Compose\MaksIT.CertsUI\tmp`
  - `C:\Compose\MaksIT.CertsUI\configMap`
  - `C:\Compose\MaksIT.CertsUI\secrets`

Powershell command to use:

```powershell
New-Item -Path `
  'C:\Compose\MaksIT.CertsUI\acme', `
  'C:\Compose\MaksIT.CertsUI\data', `
  'C:\Compose\MaksIT.CertsUI\tmp', `
  'C:\Compose\MaksIT.CertsUI\configMap', `
  'C:\Compose\MaksIT.CertsUI\secrets' `
  -ItemType Directory -Force
```

### Secrets and Configuration

Create the following files in the appropriate folders:

**1. Create the file `C:\Compose\MaksIT.CertsUI\secrets\appsecrets.json` with this command:**

```powershell
Set-Content -Path 'C:\Compose\MaksIT.CertsUI\secrets\appsecrets.json' -Value @'
{
  "Configuration": {
    "CertsEngineConfiguration": {
      "ConnectionString": "Host=postgres;Port=5432;Database=certsui;Username=certsui;Password=certsui;SslMode=Prefer"
    },
    "Auth": {
      "Secret": "<your-auth-secret>",
      "Pepper": "<your-pepper>"
    },
    "Agent": {
      "AgentKey": "<your-agent-key>"
    }
  }
}
'@
```

**Note:**  
PostgreSQL is **`Configuration:CertsEngineConfiguration:ConnectionString`**. For Docker Compose, use the Postgres service hostname (here **`postgres`**) and credentials that match **`docker-compose.override.yml`** (**`certsui`** defaults). Legacy **`ConnectionStrings:Certs`** is still supported. Replace placeholder values `<your-auth-secret>`, `<your-pepper>`, `<your-agent-key>`, with secure, your environment-specific values.
Make sure `<your-agent-key>` matches the key configured in your agent deployment.

**2. Create the file  `C:\Compose\MaksIT.CertsUI\configMap\appsettings.json` with this command:**

```powershell
Set-Content -Path 'C:\Compose\MaksIT.CertsUI\configMap\appsettings.json' -Value @'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Configuration": {
    "Auth": {
      "Issuer": "<your-issuer>",
      "Audience": "<your-audience>",
      "Expiration": 15,
      "RefreshExpiration": 180
    },
    "Agent": {
      "AgentHostname": "http://<your-agent-hostname>",
      "AgentPort": 5000,
      "ServiceToReload": "haproxy"
    },
    "Production": "https://acme-v02.api.letsencrypt.org/directory",
    "Staging": "https://acme-staging-v02.api.letsencrypt.org/directory",
  }
}
'@
```

**Note:**  
ACME sessions, HTTP-01 challenges, Terms of Service caching, and registration data live in PostgreSQL. Replace all JWT-related placeholder values `<your-issuer>`, `<your-audience>` and `<your-agent-hostname>` with your environment-specific values.

**3. Create the file `C:\Compose\MaksIT.CertsUI\client\config.js` with this command:**

```powershell
Set-Content -Path 'C:\Compose\MaksIT.CertsUI\client\config.js' -Value @'
window.RUNTIME_CONFIG = {
  API_URL: "http://<your-server-hostname>:8080/api"
};
'@
```

**Note:**  
  - Replace placeholder value `<your-server-hostname>` to tell the client where **MaksIT.CertsUI** server is running

### Running the Project with Docker Compose

In the project root (`C:\Compose\MaksIT.CertsUI`), create a new file named `docker-compose.yml` with the following content:

```powershell
Set-Content -Path 'C:\Compose\MaksIT.CertsUI\docker-compose.yml' -Value @'
services:
  reverseproxy:
    image: cr.maks-it.com/certs-ui/reverseproxy:latest
    container_name: reverseproxy
    ports:
      - "8080:8080"
    depends_on:
      - client
      - server
    networks:
      - certs-ui-network

  client:
    image: cr.maks-it.com/certs-ui/client:latest
    container_name: certs-ui-client
    volumes:
    - C:\Compose\MaksIT.CertsUI\client\config.js:/app/dist/config.js:ro
    networks:
      - certs-ui-network

  server:
    image: cr.maks-it.com/certs-ui/server:latest
    container_name: certs-ui-server
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_HTTP_PORTS=5000
    volumes:
      - C:\Compose\MaksIT.CertsUI\acme:/acme
      - C:\Compose\MaksIT.CertsUI\data:/data
      - C:\Compose\MaksIT.CertsUI\tmp:/tmp
      - C:\Compose\MaksIT.CertsUI\configMap\appsettings.json:/configMap/appsettings.json:ro
      - C:\Compose\MaksIT.CertsUI\secrets\appsecrets.json:/secrets/appsecrets.json:ro
    networks:
      - certs-ui-network

networks:
  certs-ui-network:
    driver: bridge
'@
```

**Note:**  
  - Adjust volume paths if changed

```powershell
docker compose -f docker-compose.yml up --build
```

This command builds and starts the following services:
- **reverseproxy**: YARP edge on port 8080; routes `/api`, `/.well-known/`, and the SPA to **`server`** / **`client`** (same layout as `src/docker-compose.yml` in this repo).
- **client**: WebUI (Vite) — Compose service name used by YARP (`http://client:5173/` inside the stack).
- **server**: WebAPI — Compose service name used by YARP (`http://server:5000/` inside the stack).

**Stop the services:**

Press `Ctrl+C` in the terminal, then run:

```powershell
docker compose -f docker-compose.yml down
```




---

## MaksIT.CertsUI Server installation on Kubernetes

The MaksIT.CertsUI Helm chart is distributed via the MaksIT container registry using the Helm OCI (Open Container Initiative) protocol.

**What is Helm OCI?**  
Helm OCI support enables you to pull and install Helm charts directly from container registries (such as Harbor, Docker Hub, or GitHub Container Registry), just like you would with Docker images. This approach is secure, versioned, and recommended for modern Kubernetes deployments.

### 2. Prepare Namespace, Secrets, and ConfigMap

By default, the chart creates the server Secret, server ConfigMap, and client ConfigMap from Helm values (`certsServerSecrets`, `certsServerConfig`, `certsClientRuntime`) as defined in [`src/helm/values.yaml`](src/helm/values.yaml). Set those keys in your `custom-values.yaml` (see the next section) and you can skip the manual `kubectl` resources below.

If you prefer to manage Secrets and ConfigMaps yourself, create them in the namespace and point the chart at them with `components.server.secretsFile.existingSecret`, `components.server.configMapFile.existingConfigMap`, and `components.client.configMapFile.existingConfigMap` (leave the templated `content` unused for that component).

**Step 1: Create Namespace**

```bash
kubectl create namespace certs-ui
```

**Step 2: Create the Secret (`appsecrets.json`)**

Replace the placeholder values with your actual secrets. This secret contains the PostgreSQL connection string, authentication, and agent keys required by the Webapi (same shape as the chart’s templated `appsecrets.json`).

```json
{
  "Configuration": {
    "CertsEngineConfiguration": {
      "ConnectionString": "Host=<postgres-host>;Port=5432;Database=certsui;Username=certsui;Password=certsui;SslMode=Prefer"
    },
    "Auth": {
      "Secret": "<your-auth-secret>",
      "Pepper": "<your-pepper>"
    },
    "Agent": {
      "AgentKey": "<your-agent-key>"
    }
  }
}
```

```bash
kubectl create secret generic certs-ui-server-secrets \
  --from-literal=appsecrets.json='{
    "Configuration": {
      "CertsEngineConfiguration": {
        "ConnectionString": "Host=<postgres-host>;Port=5432;Database=certsui;Username=certsui;Password=certsui;SslMode=Prefer"
      },
      "Auth": {
        "Secret": "<your-auth-secret>",
        "Pepper": "<your-pepper>"
      },
      "Agent": {
        "AgentKey": "<your-agent-key>"
      }
    }
  }' \
  -n certs-ui
```

**Note:**  
Replace `<postgres-host>`, `<user>`, `<password>`, and the auth placeholders with your environment-specific values. Replace placeholder values `<your-auth-secret>`, `<your-pepper>`, `<your-agent-key>`, with secure values.
Make sure `<your-agent-key>` matches the key configured in your agent deployment.

**Step 3: Create the ConfigMap (`appsettings.json`)**

Edit the values as needed for your environment. This configmap contains application settings for logging, authentication, agent configuration, and ACME endpoints.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Configuration": {
    "Auth": {
      "Issuer": "<your-issuer>",
      "Audience": "<your-audience>",
      "Expiration": 15,
      "RefreshExpiration": 180
    },
    "Agent": {
      "AgentHostname": "http://<your-agent-hostname>",
      "AgentPort": 5000,
      "ServiceToReload": "haproxy"
    },
    "Production": "https://acme-v02.api.letsencrypt.org/directory",
    "Staging": "https://acme-staging-v02.api.letsencrypt.org/directory",
  }
}
```

```bash
kubectl create configmap certs-ui-server-configmap \
  --from-literal=appsettings.json='{
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "AllowedHosts": "*",
    "Configuration": {
      "Auth": {
        "Issuer": "<your-issuer>",
        "Audience": "<your-audience>",
        "Expiration": 15,
        "RefreshExpiration": 180
      },
      "Agent": {
        "AgentHostname": "http://<your-agent-hostname>",
        "AgentPort": 5000,
        "ServiceToReload": "haproxy"
      },
      "Production": "https://acme-v02.api.letsencrypt.org/directory",
      "Staging": "https://acme-staging-v02.api.letsencrypt.org/directory",
    }
  }' \
  -n certs-ui
```

**Note:**  
Replace all JWT-related placeholder values `<your-issuer>`, `<your-audience>` and `<your-agent-hostname>` with your environment-specific values.

**Step 4: Create the ConfigMap (`config.js`)**

Edit the values as needed for your environment. This ConfigMap supplies the WebUI runtime config. When using Helm values instead, set `certsClientRuntime.apiUrl` (see the next section).

```javascript
window.RUNTIME_CONFIG = {
  API_URL: "http://<your-public-hostname>/api"
};
```

Use the URL your **browser** will call for the API (often the reverse proxy or ingress hostname, not an internal ClusterIP).

```bash
kubectl create configmap certs-ui-client-configmap \
  --from-literal=config.js='
    window.RUNTIME_CONFIG = {
      API_URL: "http://<your-public-hostname>/api"
    };' \
  -n certs-ui
```

**Note:**  
Replace `<your-public-hostname>` with the hostname or IP users use to reach the app (including TLS and port if not 80/443).

### 3. Create a Minimal Custom Values File

Below is a minimal `custom-values.yaml` aligned with the chart’s value schema in [`src/helm/values.yaml`](src/helm/values.yaml). It sets the client API URL, storage class for server PVCs, and optional registry pull secrets.

```yaml
global:
  imagePullSecrets: []

certsClientRuntime:
  apiUrl: "https://certs-ui.example.com/api"

components:
  server:
    persistence:
      storageClass: local-path
```

Override **`certsServerSecrets`** (including **`certsServerSecrets.certsEngineConfiguration.connectionString`** for PostgreSQL) and **`certsServerConfig`** here for production (JWT issuer/audience, agent hostname, ACME endpoints, and auth secrets). Chart defaults are placeholders only.

**Services:** The chart renders one `Service` per component (`server`, `client`, `reverseproxy`). Each `service` block supports `enabled`, `type`, `port`, and `targetPort` only. For Cilium LB-IPAM, MetalLB, or cloud load balancers, use a separate manifest or your platform’s pattern so you can set annotations, `loadBalancerIP`, and session affinity; point that Service at the **reverseproxy** pods (`app.kubernetes.io/component: reverseproxy`).

### 4. Install the Helm Chart

Install or upgrade the MaksIT.CertsUI chart using your custom values file (`helm upgrade --install` creates the release on first run).

**On Linux:**

```bash
helm upgrade --install certs-ui oci://cr.maks-it.com/charts/certs-ui \
  -n certs-ui \
  -f custom-values.yaml \
  --version X.Y.Z
```

**On Windows PowerShell:**

```powershell
helm upgrade --install certs-ui oci://cr.maks-it.com/charts/certs-ui `
  -n certs-ui `
  -f custom-values.yaml `
  --version X.Y.Z
```

**Note:**
`Chart.yaml` in the repository uses placeholder `version` / `appVersion` (`0.0.0`); the release pipeline sets both from the app semver when pushing the chart. When installing from your registry, pass `--version` with the chart version you published (same semver as the app release, e.g. `3.3.4`).

### 5. Uninstall the Helm Chart

PVCs for the server component use `helm.sh/resource-policy: keep` by default (`components.server.persistence.volumes[].pvc.keep: true`), so **`helm uninstall` does not delete them**—ACME and data volumes remain until you remove the claims manually. Set `pvc.keep: false` on a volume if you want that claim deleted with the release.

To uninstall the release (deployments, services, etc.) run:

**On Linux:**

```bash
helm uninstall certs-ui -n certs-ui
```

**On Windows PowerShell:**

```powershell
helm uninstall certs-ui -n certs-ui
```

## Run E2E Against k3s Ingress (PowerShell)

Use the API-key E2E tests from `MaksIT.CertsUI.Tests` to validate health, authorization, and multi-replica routing behavior through your ingress.

### 1) Create a read-capable API key

Create the API key in the WebUI (or API) and copy the plaintext key once. The E2E flow expects this key in `X-API-KEY`.

### 2) Set E2E environment variables

```powershell
$env:CERTSUI_E2E_BASE_URL = "https://certs-ui.<your-domain>"
$env:CERTSUI_E2E_API_KEY = "<paste-api-key>"
$env:CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES = "2"
```

Notes:
- `CERTSUI_E2E_BASE_URL` must be the public ingress URL (no `/api` suffix).
- `CERTSUI_E2E_EXPECT_MIN_DISTINCT_INSTANCES` defaults to `2` if omitted.
- Ensure ingress session affinity is disabled (or not sticky) for the multi-replica assertion.

### 3) Run only the API-key E2E suite

```powershell
dotnet test .\src\MaksIT.CertsUI.Tests\MaksIT.CertsUI.Tests.csproj --filter "FullyQualifiedName~CertsUiApiKeyE2ETests"
```

### 4) Optional: run only the replica-distribution assertion

```powershell
dotnet test .\src\MaksIT.CertsUI.Tests\MaksIT.CertsUI.Tests.csproj --filter "FullyQualifiedName~ApiKey_StickyLessRequests_RuntimeInstanceId_ObservesMultipleReplicas"
```

If this test reports fewer instances than expected, check:
- `components.server.replicaCount` in Helm values;
- ingress/load-balancer session affinity settings;
- rollout completion (`kubectl -n certs-ui get pods -l app.kubernetes.io/component=server`).

---

## MaksIT.CertsUI Interface Overview

The **MaksIT.CertsUI** interface provides a user-friendly web dashboard for managing Let's Encrypt certificates in your environment. Below is a step-by-step guide to getting started with the WebUI:


1. **First Login:**
  Open the client in your browser.
  **Default credentials:**
  - Username: `admin`
  - Password: `password`
  > **Important:** Change the default password immediately after logging in for the first time for security.

   ![LoginScreen](/assets/chrome_IvvCTtYcbi.png)

2. **Change the Default Password:**  
   Click on the **admin** username in the top right corner and select **Edit User**.

   ![LogoutOfcanvas](/assets/chrome_KJw6epn11q.png)

   Go to **Change password**.

   ![ChangePassword](/assets/chrome_YYr4z77ROv.png)

   Enter a strong, alphanumeric password with special characters.

   ![InsertNewPassword](/assets/chrome_hTAVSZW6fb.png)

3. **Verify Agent Connectivity:**  
   Before registering certificates, ensure the agent is properly configured and available.  
   Go to **Utilities** and click **Test agent**. If everything is set up correctly, a "Hello World!" toast notification will appear.

   ![Utilities](/assets/chrome_j5MltyeUfB.png)

4. **Register a New Account:**  
   Proceed to register a new account and fill in the required fields.  
   *Tip: Start with a few hostnames and gradually add 3 or 4 at a time.*

   ![Register](/assets/chrome_vPdHf2afpO.png)

5. **View and Manage Certificates:**  
   If registration is successful, your new account will be created and you will see a list of obtained certificates along with their expiration dates.

   ![Home](/assets/chrome_ydyMattQYU.png)

The WebUI streamlines certificate management, making it easy to change credentials, verify agent status, and monitor certificate lifecycles.


---

## Contact

For any inquiries or contributions, feel free to reach out:

- **Email**: maksym.sadovnychyy@gmail.com
- **Author**: Maksym Sadovnychyy (MAKS-IT)
