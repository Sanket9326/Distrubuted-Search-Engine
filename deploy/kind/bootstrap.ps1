<#
.SYNOPSIS
  Bootstraps a local kind cluster running the full search-engine stack via ArgoCD.

.DESCRIPTION
  1. Creates the kind cluster (deploy/kind/kind-config.yaml).
  2. Installs ingress-nginx (kind-specific manifest) and waits for it to be ready.
  3. Installs Metrics Server so local HorizontalPodAutoscalers can report healthy.
  4. Installs ArgoCD into the argocd namespace and waits for it to be ready.
  5. By default, builds every service's Docker image locally, tags it with the short
     git SHA, and loads it into the kind cluster (no registry round-trip).
  6. By default, rewrites each chart's values.yaml image.tag to that SHA and commits
     the change locally — it does NOT push. Review the diff and push it yourself so
     ArgoCD (which tracks origin/master) can pick it up.
  7. Applies the ArgoCD AppProject and all Applications.

  Use -UseRegistryImages to skip local image builds and use the image tags already
  committed to master by GitHub Actions. This mode is useful for validating the
  GHCR-based automated flow on a local kind cluster.

  Safe to re-run: cluster/namespace/install steps are idempotent. Use -Recreate
  to remove and rebuild the named local kind cluster when its pods or ArgoCD
  components are stuck after a Docker Desktop or WSL restart.
#>

[CmdletBinding()]
param(
    [switch]$UseRegistryImages,
    [switch]$Recreate
)

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$ClusterName = "search-engine"
$Sha = (git -C $RepoRoot rev-parse --short HEAD).Trim()

Write-Host "== Repo root: $RepoRoot" -ForegroundColor Cyan
Write-Host "== Image tag for this run: $Sha" -ForegroundColor Cyan

# 1. Create the kind cluster
$existingClusters = kind get clusters 2>$null
if ($Recreate -and $existingClusters -contains $ClusterName) {
    Write-Host "== Recreating kind cluster '$ClusterName'..." -ForegroundColor Yellow
    kind delete cluster --name $ClusterName
    $existingClusters = @()
}

if ($existingClusters -notcontains $ClusterName) {
    Write-Host "== Creating kind cluster '$ClusterName'..." -ForegroundColor Cyan
    kind create cluster --config "$RepoRoot\deploy\kind\kind-config.yaml"
} else {
    Write-Host "== kind cluster '$ClusterName' already exists, skipping create." -ForegroundColor Yellow
}

# 2. Install ingress-nginx (kind provider manifest)
Write-Host "== Installing ingress-nginx..." -ForegroundColor Cyan
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml
kubectl wait --namespace ingress-nginx `
    --for=condition=available deployment/ingress-nginx-controller `
    --timeout=180s

# 3. Install Metrics Server for HorizontalPodAutoscalers.
# kind kubelets use certificates whose IP SANs cannot be verified by Metrics
# Server, so the local-only insecure TLS flag is required.
Write-Host "== Installing Metrics Server..." -ForegroundColor Cyan
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml
kubectl -n kube-system patch deployment metrics-server --type=json `
    -p '[{"op":"add","path":"/spec/template/spec/containers/0/args/-","value":"--kubelet-insecure-tls"}]'
kubectl rollout status deployment/metrics-server -n kube-system --timeout=180s

# 4. Install ArgoCD
Write-Host "== Installing ArgoCD..." -ForegroundColor Cyan
kubectl create namespace argocd --dry-run=client -o yaml | kubectl apply -f -
# ArgoCD's ApplicationSet CRD is larger than Kubernetes' 256 KiB
# last-applied-configuration annotation limit. Server-side apply avoids that
# client-side annotation and still allows this install step to be re-run.
kubectl apply --server-side -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml
kubectl wait --namespace argocd `
    --for=condition=available deployment/argocd-server `
    --timeout=300s

$adminPasswordB64 = kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath="{.data.password}" 2>$null
if ($adminPasswordB64) {
    $adminPassword = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($adminPasswordB64))
    Write-Host "== ArgoCD admin password: $adminPassword" -ForegroundColor Green
    Write-Host "   (port-forward with: kubectl -n argocd port-forward svc/argocd-server 8080:443)" -ForegroundColor Green
} else {
    Write-Host "== ArgoCD admin secret not found (already rotated?) - skipping password print." -ForegroundColor Yellow
}

# 5. Build, tag, and load each service image unless registry mode was requested
$services = @(
    @{ Chart = "document-ingestion-service"; Dockerfile = "src\Services\DocumentIngestionService\Dockerfile" },
    @{ Chart = "embedding-service";          Dockerfile = "src\Services\EmbeddingService\Dockerfile" },
    @{ Chart = "keyword-index-service";      Dockerfile = "src\Services\KeywordIndexService\Dockerfile" },
    @{ Chart = "reliability-service";        Dockerfile = "src\Services\ReliabilityService\Dockerfile" },
    @{ Chart = "search-service";             Dockerfile = "src\Services\SearchService\Dockerfile" },
    @{ Chart = "upload-service";             Dockerfile = "src\Services\UploadService\Dockerfile" },
    @{ Chart = "web-ui";                     Dockerfile = "src\Services\WebUI\Dockerfile" }
)

if ($UseRegistryImages) {
    Write-Host "== Registry mode enabled: skipping local image builds and Helm tag commits." -ForegroundColor Yellow
    Write-Host "   ArgoCD will use the image tags committed to origin/master by GitHub Actions." -ForegroundColor Yellow
} else {
    foreach ($svc in $services) {
        $valuesPath = Join-Path $RepoRoot "deploy\helm\$($svc.Chart)\values.yaml"
        $repository = (Select-String -Path $valuesPath -Pattern "^\s*repository:\s*(\S+)").Matches[0].Groups[1].Value
        $image = "${repository}:${Sha}"

        Write-Host "== Building $image..." -ForegroundColor Cyan
        docker build -f (Join-Path $RepoRoot $svc.Dockerfile) -t $image $RepoRoot

        Write-Host "== Loading $image into kind cluster '$ClusterName'..." -ForegroundColor Cyan
        kind load docker-image $image --name $ClusterName

        Write-Host "== Updating $valuesPath image.tag -> $Sha" -ForegroundColor Cyan
        (Get-Content $valuesPath -Raw) -replace "(?m)^(\s*tag:\s*)`"?[\w.\-]+`"?\s*$", "`${1}`"$Sha`"" |
            Set-Content -Path $valuesPath -NoNewline
    }

    # 6. Commit locally (does NOT push — review and push yourself)
    Push-Location $RepoRoot
    try {
        git add deploy/helm/*/values.yaml
        $staged = git diff --cached --name-only
        if ($staged) {
            git commit -m "chore: bump local kind image tags to $Sha"
            Write-Host "== Committed image tag bump locally. Review with 'git show' then push:" -ForegroundColor Green
            Write-Host "     git push origin <your-branch>   (then merge to master for ArgoCD to sync)" -ForegroundColor Green
        } else {
            Write-Host "== No image.tag changes to commit (already at $Sha?)." -ForegroundColor Yellow
        }
    } finally {
        Pop-Location
    }
}

# 7. Apply ArgoCD project + applications
Write-Host "== Applying ArgoCD project and applications..." -ForegroundColor Cyan

# The Search Service runtime secret must exist before ArgoCD creates the
# Application destination namespace. Create the namespace explicitly so a
# first bootstrap can reliably install the secret from .env.
kubectl create namespace search-engine --dry-run=client -o yaml | kubectl apply -f -

# Keep local credentials out of Git-managed Helm values. If .env contains a
# Gemini key, create/update the runtime-only Secret before ArgoCD syncs the
# search service. The chart references this Secret through existingSecret.
$envFile = Join-Path $RepoRoot ".env"
if (Test-Path $envFile) {
    $geminiKey = ((Get-Content $envFile | Where-Object { $_ -match '^GEMINI_API_KEY=' } | Select-Object -First 1) -replace '^GEMINI_API_KEY=', '').Trim()
    if ($geminiKey) {
        kubectl create secret generic search-service-runtime-secrets -n search-engine `
            --from-literal="Gemini__ApiKey=$geminiKey" `
            --from-literal='Postgres__ConnectionString=Host=postgres;Port=5432;Database=documentsearch;Username=documentsearch;Password=changeme' `
            --dry-run=client -o yaml | kubectl apply -f -
    }
}

kubectl apply -f "$RepoRoot\deploy\argocd\projects\search-engine-project.yaml"
kubectl apply -f "$RepoRoot\deploy\argocd\applications\"

# In local mode, ArgoCD tracks origin/master but the images above were built locally
# and loaded directly into kind. Set the image tag as an ArgoCD Helm parameter so
# a stale remote tag cannot leave an old ReplicaSet in ImagePullBackOff.
# In registry mode, remove the local override and use the tags from master.
foreach ($svc in $services) {
    $parameters = @()
    if (-not $UseRegistryImages) {
        $parameters += @{ name = "image.tag"; value = $Sha }
    }
    if ($svc.Chart -eq "search-service") {
        $parameters += @{ name = "existingSecret"; value = "search-service-runtime-secrets" }
    }
    $patch = @{ spec = @{ source = @{ helm = @{ parameters = $parameters } } } } |
        ConvertTo-Json -Depth 10 -Compress
    kubectl -n argocd patch application $svc.Chart --type merge -p $patch
}

Write-Host "== Waiting for local application rollouts..." -ForegroundColor Cyan

# ArgoCD creates the Deployments asynchronously after its Application resources
# are applied. Wait for each Deployment to exist before asking kubectl for its
# rollout status, otherwise a freshly created cluster reports a misleading
# NotFound error even though reconciliation is still in progress.
function Wait-ForDeployment {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [int]$TimeoutSeconds = 180
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $deployment = kubectl get deployment $Name -n search-engine -o name 2>$null
        if ($LASTEXITCODE -eq 0 -and $deployment) {
            return $true
        }

        Start-Sleep -Seconds 5
    } while ((Get-Date) -lt $deadline)

    return $false
}

foreach ($svc in $services) {
    $deploymentName = "$($svc.Chart)-$($svc.Chart)"
    if (Wait-ForDeployment -Name $deploymentName) {
        kubectl -n search-engine rollout status "deployment/$deploymentName" --timeout=300s
    } else {
        Write-Warning "Deployment '$deploymentName' was not created by ArgoCD within 180 seconds."
    }
}

if ($UseRegistryImages) {
    Write-Host "== Done. Registry images are managed by GitHub Actions; watch sync status with:" -ForegroundColor Cyan
} else {
    Write-Host "== Done. Push the commit above to master, then watch sync status with:" -ForegroundColor Cyan
}
Write-Host "     kubectl get applications -n argocd -w" -ForegroundColor Cyan
