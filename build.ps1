$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
 $compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
 if(!(Test-Path -LiteralPath $compiler)){throw '.NET Framework 4.x C# compiler is required.'}
 New-Item -ItemType Directory -Path dist -Force | Out-Null
 $sources=@(Get-ChildItem -LiteralPath src -Filter '*.cs' | ForEach-Object {$_.FullName})
 & $compiler /nologo /target:winexe /platform:x64 /optimize+ /debug- /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /win32manifest:app.manifest /win32icon:assets/Stackify.ico /out:dist/Stackify.exe @sources
 if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
 Copy-Item -LiteralPath assets/Stackify.ico -Destination dist/Stackify.ico
 Copy-Item -LiteralPath README.md,LICENSE,PRIVACY.md -Destination dist
 Get-FileHash -Algorithm SHA256 -LiteralPath dist/Stackify.exe | ForEach-Object {($_.Hash.ToLower()+'  Stackify.exe')} | Set-Content -LiteralPath dist/SHA256SUMS.txt -Encoding ASCII
}finally{Pop-Location}
