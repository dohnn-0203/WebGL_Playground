<#
.SYNOPSIS
    Publishes the freshly-built docs/ folder to the gh-pages branch as a single
    orphan commit, then force-pushes it. Build binaries therefore live ONLY on
    gh-pages (always one commit) and never accumulate in main's history.

.DESCRIPTION
    GitHub Pages must be configured as:  Settings -> Pages -> Source = "gh-pages" / (root).

    How it works (no worktree, never touches your working tree or main index):
      1. Stage docs/ contents at repo ROOT into a throwaway index.
      2. Write that as a tree, commit it with NO parent (orphan = single commit).
      3. Force-push to origin/gh-pages (replaces the previous deploy entirely).

    Run it AFTER building the WebGL output into docs/, e.g. the Unity menu
    "MergeCafe -> Build WebGL (docs)" or the batch build in the README.

.EXAMPLE
    pwsh tools/deploy-pages.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

function Invoke-Git {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Args)
    & git @Args
    if ($LASTEXITCODE -ne 0) { throw "git $($Args -join ' ') failed (exit $LASTEXITCODE)" }
}

$root = (& git rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0) { throw "Not inside a git repository." }
Set-Location $root

$docs = Join-Path $root 'docs'
if (-not (Test-Path (Join-Path $docs 'index.html'))) {
    throw "docs/index.html not found - build the WebGL output into docs/ first."
}

$short = (& git rev-parse --short HEAD).Trim()
$branch = (& git rev-parse --abbrev-ref HEAD).Trim()
$index = Join-Path ([System.IO.Path]::GetTempPath()) ("ghpages_" + [System.Guid]::NewGuid().ToString('N') + ".index")

try {
    $env:GIT_INDEX_FILE = $index

    # Stage everything under docs/ using docs/ AS the work tree, so paths are
    # recorded relative to root (index.html, Build/..., .nojekyll) - not docs/...
    Push-Location $docs
    try { Invoke-Git '--work-tree=.' add -A -f . }
    finally { Pop-Location }

    $tree = (& git write-tree).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($tree)) { throw "git write-tree failed." }

    # No -p => orphan commit with no history => gh-pages is always a single commit.
    $commit = (& git commit-tree $tree -m "deploy WebGL build ($short from $branch)").Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($commit)) { throw "git commit-tree failed." }

    Invoke-Git update-ref refs/heads/gh-pages $commit
    Invoke-Git push -f origin gh-pages
    Write-Host "Deployed $commit to gh-pages (from $short). GitHub Pages will update shortly." -ForegroundColor Green
}
finally {
    Remove-Item Env:\GIT_INDEX_FILE -ErrorAction SilentlyContinue
    if (Test-Path $index) { Remove-Item $index -Force -ErrorAction SilentlyContinue }
    # Drop the local ref so build blobs don't accumulate locally either; remote keeps the deploy.
    & git branch -D gh-pages 2>$null | Out-Null
}
