$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
 New-Item -ItemType Directory -Path build -Force | Out-Null
 $compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
 $sources=@(Get-ChildItem src -Filter '*.cs' | ForEach-Object {$_.FullName})
 & $compiler /nologo /target:exe /platform:x64 /main:Tests /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /out:build/Tests.exe @sources (Join-Path $PSScriptRoot 'tests\Tests.cs')
 if($LASTEXITCODE -ne 0){throw 'Tests did not compile'}
 & ./build/Tests.exe
 if($LASTEXITCODE -ne 0){throw 'Tests failed'}
 & $compiler /nologo /target:exe /platform:x64 /main:ClosingTest /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /out:build/ClosingTest.exe @sources (Join-Path $PSScriptRoot 'tests\ClosingTest.cs')
 if($LASTEXITCODE -ne 0){throw 'Close/launch tests did not compile'}
 & ./build/ClosingTest.exe
 if($LASTEXITCODE -ne 0){throw 'Close/launch tests failed'}
}finally{Pop-Location}


