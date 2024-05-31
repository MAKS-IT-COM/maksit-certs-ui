# LetsEncrypt C# Client by Maks-IT.com

Simple client to obtain Let's Encrypt HTTPS certificates developed with .net core and curently works only with http challange

## Versions History

* 29 Jun, 2019 - V1.0
* 01 Nov, 2019 - V2.0 (Dependency Injection pattern impelemtation)
* 31 May, 2024 - V3.0 (Webapi and containerization)

## Haproxy configuration

```bash
#---------------------------------------------------------------------
# Example configuration for a possible web application.  See the
# full configuration options online.
#
#   https://www.haproxy.org/download/1.8/doc/configuration.txt
#
#---------------------------------------------------------------------

#---------------------------------------------------------------------
# Global settings
#---------------------------------------------------------------------
global
    # to have these messages end up in /var/log/haproxy.log you will
    # need to:
    #
    # 1) configure syslog to accept network log events.  This is done
    #    by adding the '-r' option to the SYSLOGD_OPTIONS in
    #    /etc/sysconfig/syslog
    #
    # 2) configure local2 events to go to the /var/log/haproxy.log
    #   file. A line like the following can be added to
    #   /etc/sysconfig/syslog
    #
    #    local2.*                       /var/log/haproxy.log
    #
    log         127.0.0.1 local2

    chroot      /var/lib/haproxy
    pidfile     /var/run/haproxy.pid
    maxconn     4000
    user        haproxy
    group       haproxy
    daemon

    # Adjust the maxconn value based on your server\'s capacity
    maxconn 2048

    # SSL certificates directory
    # ca-base /etc/ssl/certs
    #crt-base /etc/ssl/private

    # Default SSL certificate (used if no SNI match)
    #ssl-default-bind-crt /etc/haproxy/certs/default.pem

    # turn on stats unix socket
    # stats socket /var/lib/haproxy/stats level admin mode 660
    #stats socket /var/run/haproxy/admin.sock level admin mode 660 user haproxy group haproxy

    setenv ACCOUNT_THUMBPRINT \'\'

    # utilize system-wide crypto-policies
    ssl-default-bind-ciphers PROFILE=SYSTEM
    ssl-default-server-ciphers PROFILE=SYSTEM


#---------------------------------------------------------------------
# common defaults that all the \'listen\' and \'backend\' sections will
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
# Frontend configuration for handling multiple domains with SNI
#---------------------------------------------------------------------
frontend web
    bind :80
    bind :443 ssl crt /etc/haproxy/certs/ strict-sni

    # Handling for ACME challenge paths
    acl acme_challenge path_beg /.well-known/acme-challenge/
    use_backend acme_challenge_backend if acme_challenge



#---------------------------------------------------------------------
# Backend configuration for ACME challenge
#---------------------------------------------------------------------
backend acme_challenge_backend
    server acme_challenge 127.0.0.1:8080
```