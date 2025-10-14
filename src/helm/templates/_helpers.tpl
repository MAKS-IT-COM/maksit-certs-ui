{{- define "certs-ui.name" -}}
{{- .Chart.Name | trunc 63 | trimSuffix "-" -}}
{{- end }}

{{- define "certs-ui.fullname" -}}
{{- $name := .Chart.Name -}}
{{- $rel  := .Release.Name -}}
{{- if or (hasPrefix (printf "%s-" $name) $rel) (eq $rel $name) -}}
{{- $rel | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" $rel $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end }}

{{- define "certs-ui.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" -}}
{{- end }}

{{- define "certs-ui.labels" -}}
app.kubernetes.io/name: {{ include "certs-ui.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
helm.sh/chart: {{ include "certs-ui.chart" . }}
{{- end }}

{{- /* Image pull secrets (global) -> list of names) */ -}}
{{- define "certs-ui.imagePullSecrets" -}}
{{- $ips := default (list) .Values.global.imagePullSecrets -}}
{{- if $ips }}
imagePullSecrets:
{{- range $ips }}
  - name: {{ .name }}
{{- end }}
{{- end }}
{{- end }}
