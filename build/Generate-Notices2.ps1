#Requires -Version 5.1
# Gera a tabela de licencas das dependencias Rust do Console Mode 2 Alpha (docs\v2\THIRD_PARTY_NOTICES.md).
# Usa so o `cargo metadata` (sem ferramentas extras): percorre o grafo de cm-app para o alvo Windows,
# sem dev-dependencies, e escreve a tabela entre os marcadores do arquivo. O resto do arquivo (texto
# escrito a mao: GPUI, SDL3, fonte, sons) fica como esta.
#   .\build\Generate-Notices2.ps1            reescreve a secao gerada
#   .\build\Generate-Notices2.ps1 -Check     falha se o arquivo estiver desatualizado

param([switch]$Check)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$file = Join-Path $root "docs\v2\THIRD_PARTY_NOTICES.md"
$begin = "<!-- BEGIN GENERATED (build/Generate-Notices2.ps1) -->"
$end = "<!-- END GENERATED -->"

Push-Location $root
try {
    # Cargo may report a package-cache wait on stderr even when metadata succeeds.
    $ErrorActionPreference = "Continue"
    $json = cargo metadata --format-version 1 --locked --filter-platform x86_64-pc-windows-msvc | Out-String
    $code = $LASTEXITCODE
    $ErrorActionPreference = "Stop"
    if ($code -ne 0) { throw "cargo metadata falhou" }
}
finally { Pop-Location }
$meta = $json | ConvertFrom-Json

# Reachable from cm-app through normal and build dependencies (dev ones never ship).
$byId = @{}; foreach ($p in $meta.packages) { $byId[$p.id] = $p }
$nodes = @{}; foreach ($n in $meta.resolve.nodes) { $nodes[$n.id] = $n }
$app = $meta.packages | Where-Object { $_.name -eq "cm-app" } | Select-Object -First 1
$seen = [System.Collections.Generic.HashSet[string]]::new()
$queue = [System.Collections.Generic.Queue[string]]::new()
$queue.Enqueue($app.id); [void]$seen.Add($app.id)
while ($queue.Count -gt 0) {
    $node = $nodes[$queue.Dequeue()]
    foreach ($d in $node.deps) {
        $shipping = $d.dep_kinds | Where-Object { $_.kind -ne "dev" }
        if (-not $shipping) { continue }
        if ($seen.Add($d.pkg)) { $queue.Enqueue($d.pkg) }
    }
}

$rows = foreach ($id in $seen) {
    $p = $byId[$id]
    # Our own workspace crates are the project itself, not third-party code.
    if ($p.source -eq $null) { continue }
    $license = if ($p.license) { $p.license -replace '\s*/\s*', ' OR ' } else { "(ver o repositorio)" }
    $repo = if ($p.repository) { $p.repository } elseif ($p.homepage) { $p.homepage } else { "https://crates.io/crates/$($p.name)" }
    [pscustomobject]@{ Name = $p.name; Version = $p.version; License = $license; Repo = $repo }
}
$rows = $rows | Sort-Object Name, Version

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine($begin)
[void]$sb.AppendLine()
[void]$sb.AppendLine("Resumo por licenca ($($rows.Count) pacotes):")
[void]$sb.AppendLine()
foreach ($g in ($rows | Group-Object License | Sort-Object Count -Descending)) {
    [void]$sb.AppendLine("- ``$($g.Name)``: $($g.Count)")
}
[void]$sb.AppendLine()
[void]$sb.AppendLine("| Pacote | Versao | Licenca | Origem |")
[void]$sb.AppendLine("|--------|--------|---------|--------|")
foreach ($r in $rows) {
    [void]$sb.AppendLine("| ``$($r.Name)`` | $($r.Version) | $($r.License) | $($r.Repo) |")
}
[void]$sb.AppendLine()
[void]$sb.Append($end)
$generated = $sb.ToString()

$text = [System.IO.File]::ReadAllText($file)
# keep the file's own line endings (git may have checked it out with CRLF)
$nl = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
$generated = $generated -replace "`r?`n", $nl
$pattern = [regex]::Escape($begin) + "[\s\S]*?" + [regex]::Escape($end)
if ($text -notmatch $pattern) { throw "Marcadores BEGIN/END GENERATED nao encontrados em $file" }
$updated = [regex]::Replace($text, $pattern, { param($m) $generated })

if ($Check) {
    if ($updated -ne $text) { throw "THIRD_PARTY_NOTICES.md do 2.0 esta desatualizado: rode build\Generate-Notices2.ps1" }
    Write-Host "OK: THIRD_PARTY_NOTICES.md do 2.0 esta em dia ($($rows.Count) pacotes)"
}
else {
    [System.IO.File]::WriteAllText($file, $updated, [System.Text.UTF8Encoding]::new($false))
    Write-Host "OK: $file ($($rows.Count) pacotes)"
}
