<#
.SYNOPSIS
  Bootstraps a local kind cluster running the full search-engine stack via ArgoCD.

.DESCRIPTION
  1. Creates the kind cluster (deploy/kind/kind-config.yaml).
  2. Installs ingress-nginx (kind-specific manifest) and waits for it to be ready.
  3. Installs ArgoCD into the argocd namespace and waits for it to be ready.
  4. Builds every service's Docker image locally, tags it with the short git SHA,
     and loads it into the kind cluster (no registry round-trip).
  5. Rewrites each chart's values.yaml image.tag to that SHA and commits the change
     locally — it does NOT push. Review the diff and push it yourself so ArgoCD
     (which tracks origin/master) can pick it up.
  6. Applies the ArgoCD AppProject and all Applications.

  Safe to re-run: cluster/namespace/install steps are idempotent.
#>

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$ClusterName = "search-engine"
$Sha = (git -C $RepoRoot rev-parse --short HEAD).Trim()

Write-Host "== Repo root: $RepoRoot" -ForegroundColor Cyan
Write-Host "== Image tag for this run: $Sha" -ForegroundColor Cyan

# 1. Create the kind cluster
$existingClusters = kind get clusters 2>$null
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
    --for=condition=ready pod `
    --selector=app.kubernetes.io/component=controller `
    --timeout=180s

# 3. Install ArgoCD
Write-Host "== Installing ArgoCD..." -ForegroundColor Cyan
kubectl create namespace argocd --dry-run=client -o yaml | kubectl apply -f -
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml
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

# 4. Build, tag, and load each service image
$services = @(
    @{ Chart = "document-ingestion-service"; Dockerfile = "src\Services\DocumentIngestionService\Dockerfile" },
    @{ Chart = "embedding-service";          Dockerfile = "src\Services\EmbeddingService\Dockerfile" },
    @{ Chart = "keyword-index-service";      Dockerfile = "src\Services\KeywordIndexService\Dockerfile" },
    @{ Chart = "reliability-service";        Dockerfile = "src\Services\ReliabilityService\Dockerfile" },
    @{ Chart = "search-service";             Dockerfile = "src\Services\SearchService\Dockerfile" },
    @{ Chart = "upload-service";             Dockerfile = "src\Services\UploadService\Dockerfile" },
    @{ Chart = "web-ui";                     Dockerfile = "src\Services\WebUI\Dockerfile" }
)

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

# 5. Commit locally (does NOT push — review and push yourself)
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

# 6. Apply ArgoCD project + applications
Write-Host "== Applying ArgoCD project and applications..." -ForegroundColor Cyan
kubectl apply -f "$RepoRoot\deploy\argocd\projects\search-engine-project.yaml"
kubectl apply -f "$RepoRoot\deploy\argocd\applications\"

Write-Host "== Done. Push the commit above to master, then watch sync status with:" -ForegroundColor Cyan
Write-Host "     kubectl get applications -n argocd -w" -ForegroundColor Cyan
