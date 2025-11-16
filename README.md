# MaksIT.CertsUI - Container-based Let's Encrypt ACME client with WebUI

Powerful client to obtain and manage Let's Encrypt HTTPS certificates.
This client currently supports the HTTP-01 challenge and is designed to follow the official [Let's Encrypt requirements and guidelines](https://letsencrypt.org/docs/), implementing the ACME protocol and adhering to recommended security and operational practices.


If you find this project useful, please consider supporting its development:

[<img src="https://cdn.buymeacoffee.com/buttons/v2/default-blue.png" alt="Buy Me A Coffee" style="height: 60px; width: 217px;">](https://www.buymeacoffee.com/maksitcom)


---

## Table of Contents

- [MaksIT.CertsUI - Container-based Let's Encrypt ACME client with WebUI](#maksitcertsui---container-based-lets-encrypt-acme-client-with-webui)
  - [Table of Contents](#table-of-contents)
  - [Versions History](#versions-history)
  - [Architecture](#architecture)
    - [Current Limitations](#current-limitations)
    - [Architecture Scheme](#architecture-scheme)
    - [Architecture Description](#architecture-description)
      - [MaksIT.CertsUI Agent](#maksitcertsui-agent)
      - [MaksIT.CertsUI WebUI](#maksitcertsui-webui)
      - [MaksIT.CertsUI WebAPI](#maksitcertsui-webapi)
      - [Flow Overview](#flow-overview)
  - [HAProxy configuration](#haproxy-configuration)
    - [Explanation](#explanation)
  - [MaksIT.CertsUI Agent installation](#maksitcertsui-agent-installation)
  - [MaksIT.CertsUI Server Installation on Linux with Podman Compose](#maksitcertsui-server-installation-on-linux-with-podman-compose)
    - [Prerequisites](#prerequisites)
    - [Running the Project with Podman Compose](#running-the-project-with-podman-compose)
  - [MaksIT.CertsUI Server Installation on Windows with Docker Compose](#maksitcertsui-server-installation-on-windows-with-docker-compose)
    - [Prerequisites](#prerequisites-1)
    - [Secrets and Configuration](#secrets-and-configuration)
    - [Running the Project with Docker Compose](#running-the-project-with-docker-compose)
  - [MaksIT.CertsUI Server installation on Kubernetes](#maksitcertsui-server-installation-on-kubernetes)
    - [1. Add MaksIT Helm Repository](#1-add-maksit-helm-repository)
    - [2. Prepare Namespace, Secrets, and ConfigMap](#2-prepare-namespace-secrets-and-configmap)
    - [3. Create a Minimal Custom Values File](#3-create-a-minimal-custom-values-file)
    - [4. Install the Helm Chart](#4-install-the-helm-chart)
  - [MaksIT.CertsUI Interface Overview](#maksitcertsui-interface-overview)
  - [Contact](#contact)


## Versions History

* 29 Jun, 2019 - V1.0.0
* 01 Nov, 2019 - V2.0.0 (Dependency Injection pattern implementation)
* 31 May, 2024 - V3.0.0 (Webapi and containerization)
* 11 Aug, 2024 - V3.1.0 (Release)
* 11 Sep, 2025 - V3.2.0 New WebUI with authentication
* 15 Nov, 2025 - V3.3.0 Pre release


---

## Architecture

This solution provides automated, secure management of Let's Encrypt certificates for environments where the edge proxy is behind NAT and certificate management logic runs in a Docker/Podman compose or Kubernetes cluster.


### Current Limitations


- **Single Agent Support:**  
  The current implementation supports only a single MaksIT.CertsUI Agent instance. High-availability (HA) or multi-agent deployments are not supported at this time. (Multi-agent/HA support may be added in future releases.)

- **No HA Mode:**  
  There is no built-in high-availability mode for the WebAPI or Agent components. This is by design, as the solution targets environments where a single edge proxy and agent are sufficient, and additional complexity is unnecessary.

- **HTTP-01 Challenge Only:**  
  Only the HTTP-01 ACME challenge type is supported. DNS-01 and other challenge types are not implemented.

- **Single Kubernetes Replica:**  
  The solution is intended for use with a single Kubernetes replica for the MaksIT.CertsUI server and MaksIT.WebUI components.

These limitations are intentional to keep the architecture simple and reliable for typical edge proxy scenarios. Future releases may introduce additional features based on user demand.

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


**Language Independence:**  
A standard **C# WebAPI implementation** of the Agent is available in this repository for immediate use or customization. However, the Agent is fully independent from the MaksIT.CertsUI server and communicates via standard **HTTP APIs**. This means you can implement the Agent in **any programming language or framework** that supports HTTP endpoints (such as **C#**, **Go**, **Python**, **Rust**, **Node.js**, etc.). The only requirements are:

- Ability to **receive certificate files via HTTP**
- Ability to **write files** to the proxy’s certificate directory
- Ability to **reload or restart** the proxy process


**Security:**  
Communication between the Agent and the **MaksIT.CertsUI** server is secured using a **shared API key**. This ensures that only authorized servers can deploy certificates and trigger proxy reloads, protecting your edge infrastructure from unauthorized access.

> **Warning:** Never commit secrets or API keys to version control. Always use strong, unique secrets and passwords.

This flexibility allows you to integrate the Agent into diverse environments and choose the best technology stack for your edge server.

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


> **Note:** Currently, only HTTP-01 challenges and a single Kubernetes replica are supported by this solution.


---

## HAProxy configuration

```bash
sudo mkdir /etc/haproxy/certs
```

```bash
sudo nano /etc/haproxy/haproxy.cfg
```

```cfg
#---------------------------------------------------------------------
# Global settings
#---------------------------------------------------------------------
global
    log         127.0.0.1 local2
    chroot      /var/lib/haproxy
    pidfile     /var/run/haproxy.pid
    maxconn     4000
    user        haproxy
    group       haproxy
    daemon
    stats socket /var/lib/haproxy/stats
    ssl-default-bind-ciphers PROFILE=SYSTEM
    ssl-default-server-ciphers PROFILE=SYSTEM

#---------------------------------------------------------------------
# common defaults that all the 'listen' and 'backend' sections will
# use if not designated in their block
#---------------------------------------------------------------------
defaults
    mode                    http
    log                     global
    option                  httplog
    option                  dontlognull
    option http-server-close
    option forwardfor       except 127.0.0.0/8
    option                  redispatch
    retries                 3
    timeout http-request    10s
    timeout queue           1m
    timeout connect         10s
    timeout client          1m
    timeout server          1m
    timeout http-keep-alive 10s
    timeout check           10s
    maxconn                 3000

#---------------------------------------------------------------------
# Frontend for HTTP traffic on port 80
#---------------------------------------------------------------------
frontend http_frontend
    bind *:80
    acl acme_path path_beg /.well-known/acme-challenge/

    # Redirect all HTTP traffic to HTTPS except ACME challenge requests
    redirect scheme https if !acme_path

    # Use the appropriate backend based on hostname if it's an ACME challenge request
    use_backend acme_backend if acme_path

#---------------------------------------------------------------------
# Backend to handle ACME challenge requests
#---------------------------------------------------------------------
backend acme_backend
    #server local_acme  172.16.0.5:8080

#---------------------------------------------------------------------
# Frontend for HTTPS traffic (port 443) with SNI and strict-sni
#---------------------------------------------------------------------
frontend https_frontend
    bind *:443 ssl crt /etc/haproxy/certs strict-sni

    http-request capture req.hdr(host) len 64

    # Define ACLs for routing based on hostname
    acl host_homepage hdr(host) -i maks-it.com

    # Use appropriate backend based on SNI hostname
    use_backend homepage_backend if host_homepage

    default_backend homepage_backend

#---------------------------------------------------------------------
# Backend for maks-it.com
#---------------------------------------------------------------------
backend homepage_backend
    http-request set-header X-Forwarded-Proto https
    http-request set-header X-Forwarded-Host %[hdr(host)]
    server homepage_server 172.16.0.10:8080
```

### Explanation
* ACME Challenge Handling:
The http_frontend listens on port 80 and checks if the request path starts with /.well-known/acme-challenge/. These requests are required by Let's Encrypt for domain validation and are forwarded to the acme_backend. All other HTTP requests are redirected to HTTPS.
* HTTPS Frontend:
The https_frontend listens on port 443, uses SNI (Server Name Indication) to serve the correct certificate, and routes requests to the appropriate backend based on the hostname.
*	Backends:
  * acme_backend should point to your ACME challenge responder (such as your LetsEncrypt client).
  * homepage_backend is an example backend for your main site, forwarding requests to your application server.
* Certificate Storage:
Place your SSL certificates in /etc/haproxy/certs. Each certificate file should contain the full certificate chain and private key.

## MaksIT.CertsUI Agent installation

Agent should be installed on same machine with your reverse proxy.

From your home directory

```bash
git clone https://github.com/MAKS-IT-COM/certs-ui.git
```

```bash
cd certs-ui/src/Agent
```

Edit `appsettings.json` configuration:

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
    "ApiKey": "<your-agent-key>",
    "CertsPath": "<your-certs-dir-path>"
  }
}
```


**Note:**  
Replace `<your-auth-secret>` with your shared API key and `<your-certs-dir-path>` with the path to your certificates directory (e.g., `/etc/haproxy/certs` as referenced in `haproxy.cfg`).
> **Warning:** Never commit secrets or API keys to version control. Always use strong, unique secrets and passwords.

If you are using a **RHEL-based** distribution, you can deploy the agent with:

```bash
sudo sh ./build_and_deploy.sh
```

This script will create the `maks-it-agent` service and open port `5000` for communication.


---

## MaksIT.CertsUI Server Installation on Linux with Podman Compose

Podman Compose usage to orchestrate multiple **MaksIT.CertsUI** services on Linux.

### Prerequisites

- [Podman](https://podman.io/getting-started/installation)
- Create these folders:
  - `/opt/Compose/MaksIT.CertsUI/acme`
  - `/opt/Compose/MaksIT.CertsUI/cache`
  - `/opt/Compose/MaksIT.CertsUI/data`
  - `/opt/Compose/MaksIT.CertsUI/tmp`
  - `/opt/Compose/MaksIT.CertsUI/configMap`
  - `/opt/Compose/MaksIT.CertsUI/secrets`
  - `/opt/Compose/MaksIT.CertsUI/client`

Bash command to use:

```bash
mkdir -p /opt/Compose/MaksIT.CertsUI/acme \
  /opt/Compose/MaksIT.CertsUI/cache \
  /opt/Compose/MaksIT.CertsUI/data \
  /opt/Compose/MaksIT.CertsUI/tmp \
  /opt/Compose/MaksIT.CertsUI/configMap \
  /opt/Compose/MaksIT.CertsUI/secrets \
  /opt/Compose/MaksIT.CertsUI/client
```

Create the following files in the appropriate folders:

**1. Create the file `/opt/Compose/MaksIT.CertsUI/secrets/appsecrets.json` with this command:**

```bash
cat > /opt/Compose/MaksIT.CertsUI/secrets/appsecrets.json <<EOF
{
  "Configuration": {
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
Replace placeholder values `<your-auth-secret>`, `<your-pepper>`, `<your-agent-key>`, with secure, your environment-specific values.
Make sure `<your-agent-key>` matches the key configured in your agent deployment.

**2. Create the file  `/opt/Compose/MaksIT.CertsUI/configMap/appsettings.json` with this command:**

```bash
cat > /opt/Compose/MaksIT.CertsUI/configMap/appsettings.json <<EOF
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
    "CacheFolder": "/cache",
    "AcmeFolder": "/acme",
    "DataFolder": "/data",
    "SettingsFile": "/data/settings.json"
  }
}
EOF
```

**Note:**  
Replace all JWT-related placeholder values `<your-issuer>`, `<your-audience>` and `<your-agent-hostname>` with your environment-specific values.

**3. Create the file `/opt/Compose/MaksIT.CertsUI/client/config.js` with this command:**

```bash
cat > /opt/Compose/MaksIT.CertsUI/configMap/appsettings.json <<EOF
window.RUNTIME_CONFIG = {
  API_URL: "http://<your-server-hostname>/api"
};
EOF
```

**Note:**  
  - Replace placeholder value `<your-server-hostname>` to tell the client where **MaksIT.CertsUI** server is running

### Running the Project with Podman Compose

In the project root (`/opt/Compose/MaksIT.CertsUI`), create a new file named `docker-compose.yml` with the following content:

```yaml
services:
  reverse-proxy:
    image: cr.maks-it.com/certs-ui/reverseproxy:latest
    container_name: reverse-proxy
    ports:
      - "8080:8080"
    depends_on:
      - certs-ui-client
      - certs-ui-server
    networks:
      - certs-ui-network

  certs-ui-client:
    image: cr.maks-it.com/certs-ui/client:latest
    container_name: certs-ui-client
    volumes:
    - /opt/Compose/MaksIT.CertsUI/client/config.js:/app/dist/config.js:ro
    networks:
      - certs-ui-network

  certs-ui-server:
    image: cr.maks-it.com/certs-ui/server:latest
    container_name: certs-ui-server
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_HTTP_PORTS=5000
    volumes:
      - /opt/Compose/MaksIT.CertsUI/acme:/acme
      - /opt/Compose/MaksIT.CertsUI/cache:/cache
      - /opt/Compose/MaksIT.CertsUI/data:/data
      - /opt/Compose/MaksIT.CertsUI/tmp:/tmp
      - /opt/Compose/MaksIT.CertsUI/configMap/appsettings.json:/configMap/appsettings.json:ro
      - /opt/Compose/MaksIT.CertsUI/secrets/appsecrets.json:/secrets/appsecrets.json:ro
    networks:
      - certs-ui-network

networks:
  certs-ui-network:
    driver: bridge
```

**Note:**  
  - Adjust volume paths if changed

```bash
podman compose -f docker-compose.yml up --build
```

This command builds and starts the following services:
- **reverse-proxy**: Exposes both `certs-ui-client` and `certs-ui-server` on the same hostname.
- **certs-ui-client**: The WebUI for managing certificates.
- **certs-ui-server**: The backend server handling ACME logic and certificate management.

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
  - `C:\Compose\MaksIT.CertsUI\cache`
  - `C:\Compose\MaksIT.CertsUI\data`
  - `C:\Compose\MaksIT.CertsUI\tmp`
  - `C:\Compose\MaksIT.CertsUI\configMap`
  - `C:\Compose\MaksIT.CertsUI\secrets`

Powershell command to use:

```powershell
New-Item -Path `
  'C:\Compose\MaksIT.CertsUI\acme', `
  'C:\Compose\MaksIT.CertsUI\cache', `
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
Replace placeholder values `<your-auth-secret>`, `<your-pepper>`, `<your-agent-key>`, with secure, your environment-specific values.
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
    "CacheFolder": "/cache",
    "AcmeFolder": "/acme",
    "DataFolder": "/data",
    "SettingsFile": "/data/settings.json"
  }
}
'@
```

**Note:**  
Replace all JWT-related placeholder values `<your-issuer>`, `<your-audience>` and `<your-agent-hostname>` with your environment-specific values.

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
  reverse-proxy:
    image: cr.maks-it.com/certs-ui/reverseproxy:latest
    container_name: reverse-proxy
    ports:
      - "8080:8080"
    depends_on:
      - certs-ui-client
      - certs-ui-server
    networks:
      - certs-ui-network

  certs-ui-client:
    image: cr.maks-it.com/certs-ui/client:latest
    container_name: certs-ui-client
    volumes:
    - C:\Compose\MaksIT.CertsUI\client\config.js:/app/dist/config.js:ro
    networks:
      - certs-ui-network

  certs-ui-server:
    image: cr.maks-it.com/certs-ui/server:latest
    container_name: certs-ui-server
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_HTTP_PORTS=5000
    volumes:
      - C:\Compose\MaksIT.CertsUI\acme:/acme
      - C:\Compose\MaksIT.CertsUI\cache:/cache
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
- **reverse-proxy**: Exposes both `certs-ui-client` and `certs-ui-server` on the same hostname.
- **certs-ui-client**: The WebUI for managing certificates.
- **certs-ui-server**: The backend server handling ACME logic and certificate management.

**Stop the services:**

Press `Ctrl+C` in the terminal, then run:

```powershell
docker compose -f docker-compose.yml down
```




---

## MaksIT.CertsUI Server installation on Kubernetes

### 1. Add MaksIT Helm Repository

The MaksIT.CertsUI Helm chart is available from the MaksIT container registry. Add the MaksIT Helm repository to your Helm client:

```bash
helm repo add maksit https://cr.maks-it.com/chartrepo/charts
helm repo update
```

### 2. Prepare Namespace, Secrets, and ConfigMap

Before installing the Helm chart, create a dedicated namespace and provide the required secrets and configuration for the MaksIT.CertsUI Webapi.

**Step 1: Create Namespace**

```bash
kubectl create namespace certs-ui
```

**Step 2: Create the Secret (`appsecrets.json`)**

Replace the placeholder values with your actual secrets. This secret contains authentication and agent keys required by the Webapi.

```json
{
  "Auth": {
    "Secret": "<your-auth-secret>",
    "Pepper": "<your-pepper>"
},
  "Agent": {
    "AgentKey": "<your-agent-key>"
  }
}
```

```bash
kubectl create secret generic certs-ui-server-secrets \
  --from-literal=appsecrets.json='{
    "Auth": {
      "Secret": "<your-auth-secret>",
      "Pepper": "<your-pepper>"
    },
    "Agent": {
      "AgentKey": "<your-agent-key>"
    }
  }' \
  -n certs-ui
```

**Note:**  
Replace placeholder values `<your-auth-secret>`, `<your-pepper>`, `<your-agent-key>`, with secure, your environment-specific values.
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
    "CacheFolder": "/cache",
    "AcmeFolder": "/acme",
    "DataFolder": "/data",
    "SettingsFile": "/data/settings.json"
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
      "CacheFolder": "/cache",
      "AcmeFolder": "/acme",
      "DataFolder": "/data",
      "SettingsFile": "/data/settings.json"
    }
  }' \
  -n certs-ui
```

**Note:**  
Replace all JWT-related placeholder values `<your-issuer>`, `<your-audience>` and `<your-agent-hostname>` with your environment-specific values.

### 3. Create a Minimal Custom Values File

Below is a minimal example of a `custom-values.yaml` for most users. It disables image pull secrets by default (since the chart and images are public), sets the storage class for persistent volumes, and configures the reverse proxy service. You can further customize this file as needed for your environment.

```yaml
global:
  imagePullSecrets: []  # Keep empty

components:
  server:
    persistence:
      storageClass: local-path

  reverseproxy:
    service:
      type: LoadBalancer
      port: 8080
      targetPort: 8080
      # Remove or comment out the next two lines to let your cloud provider assign a dynamic IP
      # loadBalancerIP: "172.16.0.5"
      # annotations:
      #   lbipam.cilium.io/ips: "172.16.0.5"
      externalTrafficPolicy: Local
      sessionAffinity: ClientIP
      sessionAffinityConfig:
        clientIP:
          timeoutSeconds: 10800
```

### 4. Install the Helm Chart

Install the MaksIT.CertsUI chart using your custom values file:

```bash
helm install certs-ui maksit/certs-ui -n certs-ui -f custom-values.yaml
```






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

---

> **Tip:** For the latest updates, documentation, and source code, visit the [GitHub repository](https://github.com/MAKS-IT-COM/certs-ui).