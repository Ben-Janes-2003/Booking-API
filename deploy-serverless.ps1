$ApiProjectFolder = "BookingApi"
$CdkProjectFolder = "infrastructure-serverless"
$TargetFramework = "net8.0"

$ApiPublishFolder = Join-Path -Path $ApiProjectFolder -ChildPath "bin/Release/$($TargetFramework)/publish"

$ZipFile = Join-Path -Path $ApiProjectFolder -ChildPath "bin/Release/$($TargetFramework)/publish.zip"

Write-Output "Step 1: Cleaning up old build artifacts..."
if (Test-Path $ApiPublishFolder) { Remove-Item -Recurse -Force $ApiPublishFolder }
if (Test-Path $ZipFile) { Remove-Item -Force $ZipFile }

Write-Output "Step 2: Publishing the .NET application for $($TargetFramework)..."
dotnet publish -c Release -f $TargetFramework $ApiProjectFolder

if (-not $?) { Write-Output ".NET publish failed."; exit 1 }

Write-Output "Step 3: Zipping the published files..."
Compress-Archive -Path "$($ApiPublishFolder)\*" -DestinationPath $ZipFile

if (-not $?) { Write-Output "Zipping failed."; exit 1 }

Write-Output "Step 4: Deploying with AWS CDK..."
Push-Location $CdkProjectFolder
cdk deploy --require-approval never
$CdkExitCode = $LASTEXITCODE
Pop-Location

if ($CdkExitCode -ne 0) { Write-Output "CDK deployment failed."; exit 1 }

Write-Output "Deployment successful!"