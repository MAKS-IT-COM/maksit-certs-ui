{{- define "certs-ui.name" -}}
{{- .Chart.Name | trunc 63 | trimSuffix "-" -}}
{{- end }}

{{- define "certs-ui.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- $name := default .Chart.Name .Values.nameOverride -}}
{{- $rel := .Release.Name -}}
{{- if contains $name $rel -}}
{{- $rel | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" $rel $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
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
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{- /* Kubernetes imagePullPolicy; accepts common casing (always / IfNotPresent). */ -}}
{{- define "certs-ui.normalizePullPolicy" -}}
{{- $in := . | toString | trim | lower -}}
{{- if eq $in "always" -}}Always{{- else if eq $in "never" -}}Never{{- else -}}IfNotPresent{{- end -}}
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

{{- /* image tag: global.image.tag when set (non-empty), else components.*.image.tag. No Chart.AppVersion. */ -}}
{{- define "certs-ui.component.imageTag" -}}
{{- $root := .root }}
{{- $comp := .comp }}
{{- $g := default dict $root.Values.global.image }}
{{- $gt := $g.tag | default "" | toString | trim -}}
{{- $ct := $comp.image.tag | default "" | toString | trim -}}
{{- $tag := ternary $gt $ct (ne $gt "") }}
{{- required "certs-ui: set global.image.tag and/or each components.*.image.tag" $tag }}
{{- end }}

{{- define "certs-ui.podLabels" -}}
{{- $root := .root }}
{{- $compName := .component }}
{{- $imageTag := .imageTag }}
app.kubernetes.io/name: {{ include "certs-ui.name" $root }}
app.kubernetes.io/instance: {{ $root.Release.Name }}
app.kubernetes.io/version: {{ $imageTag | quote }}
helm.sh/chart: {{ include "certs-ui.chart" $root }}
app.kubernetes.io/component: {{ $compName }}
{{- end }}
