$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$package = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Library/PackageCache') -Directory -Filter 'com.unity.visualscripting@*' | Select-Object -First 1
if (-not $package) { throw 'Abre el proyecto en Unity para disponer de su lector YAML.' }
$dll = Join-Path $package.FullName 'Editor/VisualScripting.Core/Dependencies/YamlDotNet/Unity.VisualScripting.YamlDotNet.dll'
$assembly = [Reflection.Assembly]::LoadFrom($dll)
$type = $assembly.GetType('Unity.VisualScripting.YamlDotNet.RepresentationModel.YamlStream')
$source = [IO.File]::ReadAllText((Join-Path $projectRoot 'Assets/Scenes/Santiago.unity'))
$headers = [regex]::Matches($source, '(?m)^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?$')
# Unity tags describe native component types; strip only these document headers.
$yaml = $source -replace '(?m)^%[^\r\n]*\r?\n', '' -replace '(?m)^--- !u!\d+ &-?\d+(?: stripped)?\r?$', '---'
$stream = [Activator]::CreateInstance($type)
$reader = [IO.StringReader]::new($yaml)
try {
    $stream.Load($reader)
    if ($stream.Documents.Count -ne $headers.Count) { throw 'Cantidad incorrecta de bloques YAML.' }
    foreach ($id in @('949000000000026', '951000000000017', '951000000000021')) {
        $index = -1
        for ($i = 0; $i -lt $headers.Count; $i++) { if ($headers[$i].Groups[2].Value -eq $id) { $index = $i; break } }
        if ($index -lt 0) { throw "Falta el texto $id." }
        $text = $stream.Documents[$index].RootNode.Children['MonoBehaviour'].Children['m_text'].Value
        if ([string]::IsNullOrWhiteSpace($text) -or $text.Contains('marca la casilla para continuar')) { throw "Texto de borrado inválido: $id." }
    }
    Write-Output ('PASS: ' + $stream.Documents.Count + ' bloques YAML válidos y textos de borrado completos.')
}
finally { $reader.Dispose() }
