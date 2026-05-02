param(
    [string]$Tag
)

if ($Tag) {
    if ($Tag -notmatch '^v\d+\.\d+\.\d+$') {
        Write-Error "Tag must be format v1.2.3"
        exit 1
    }
} else {
    $latest = git tag --list 'v*' --sort=-version:refname | Select-Object -First 1
    if ($latest -match '^v(\d+)\.(\d+)\.(\d+)$') {
        $Tag = "v$($Matches[1]).$($Matches[2]).$([int]$Matches[3] + 1)"
    } else {
        $Tag = "v1.0.0"
    }
    Write-Host "Auto tag: $Tag"
}

$existing = git tag --list $Tag
if ($existing) {
    Write-Error "Tag $Tag already exists"
    exit 1
}

git tag $Tag
git push origin $Tag
Write-Host "Pushed $Tag — release workflow starting on GitHub"
