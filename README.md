# LetsEncrypt C# Client by Maks-IT.com

Simple client to obtain Let's Encrypt HTTPS certificates developed with .net core and curently works only with http challange

## Versions History

* 29 Jun, 2019 - V1.0
* 01 Nov, 2019 - V2.0 (Dependency Injection pattern impelemtation)
* 31 May, 2024 - V3.0 (Webapi and containerization)
* 11 Aug, 2024 - V3.1 (Release)
* 11 Sep, 2025 - V3.2 New WebUI with authentication

## Haproxy configuration

```bash
sudo mkdir /etc/haproxy/certs
```

```bash
sudo nano /etc/haproxy/haproxy.cfg
```

```ini
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
    server local_acme 127.0.0.1:8080

#---------------------------------------------------------------------
# Frontend for HTTPS traffic (port 443) with SNI and strict-sni
#---------------------------------------------------------------------
frontend https_frontend
    bind *:443 ssl crt /etc/haproxy/certs strict-sni

    http-request capture req.hdr(host) len 64

    # Define ACLs for routing based on hostname
    acl host_git hdr(host) -i git.maks-it.com
    acl host_cr hdr(host) -i cr.maks-it.com

    # Use appropriate backend based on SNI hostname
    use_backend git_backend if host_git
    use_backend cr_backend if host_cr

#---------------------------------------------------------------------
# Backend for git.maks-it.com
#---------------------------------------------------------------------
backend git_backend
    http-request set-header X-Forwarded-Proto https
    http-request set-header X-Forwarded-Host %[hdr(host)]
    server git_server gitsrv0002.corp.maks-it.com:3000

#---------------------------------------------------------------------
# Backend for cr.maks-it.com
#---------------------------------------------------------------------
backend cr_backend
    http-request set-header X-Forwarded-Proto https
    http-request set-header X-Forwarded-Host %[hdr(host)]
    server cr_server hcrsrv0001.corp.maks-it.com:80

#---------------------------------------------------------------------
# letsencrypt load balancer
#---------------------------------------------------------------------
frontend letsencrypt
    bind *:8080
    mode http
    acl path_well_known_acme path_beg /.well-known/acme-challenge/
    acl path_swagger path_beg /swagger/
    acl path_api path_beg /api/

    use_backend letsencrypt_server if path_well_known_acme
    use_backend letsencrypt_server if path_swagger
    use_backend letsencrypt_server if path_api
    default_backend letsencrypt_app

backend letsencrypt_server
    mode http
    server server1 127.0.0.1:9000 check

backend letsencrypt_app
    mode http
    server app1 127.0.0.1:3000 check

```

## MaksIT agent installation

From your home directory

```bash
git clone https://github.com/MAKS-IT-COM/certs-ui.git
```

```bash
cd certs-ui/src/Agent
```

```bash
sudo sh ./build_and_deploy.sh
```


## Maks IT LetsEncrypt server installation

From your home directory

```bash
git clone https://github.com/MAKS-IT-COM/certs-ui.git
```

```bash
cd certs-ui/src
```

```bash
podman-compose -f docker-compose.final.yml up
```
