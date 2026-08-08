{{- define "infra.labels" -}}
app.kubernetes.io/part-of: search-engine-infra
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end -}}
