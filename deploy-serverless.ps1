$ApiProjectFolder = "BookingApi"
$CdkProjectFolder = "infrastructure-serverless"
$TargetFramework = "net8.0"

$ApiPublishFolder = Join-Path -Path $ApiProjectFolder -ChildPath "bin/Release/$($TargetFramework)/publish"

$ZipFile = Join-Path -Path $ApiProjectFolder -ChildPath "bin/Release/$($TargetFramework)/publish.zip"

echo "Step 1: Cleaning up old build artifacts..."
if (Test-Path $ApiPublishFolder) { Remove-Item -Recurse -Force $ApiPublishFolder }
if (Test-Path $ZipFile) { Remove-Item -Force $ZipFile }

echo "Step 2: Publishing the .NET application for $($TargetFramework)..."
dotnet publish -c Release -f $TargetFramework $ApiProjectFolder

if (-not $?) { echo ".NET publish failed."; exit 1 }

echo "Step 3: Zipping the published files..."
Compress-Archive -Path "$($ApiPublishFolder)\*" -DestinationPath $ZipFile

if (-not $?) { echo "Zipping failed."; exit 1 }

echo "Step 4: Deploying with AWS CDK..."
Push-Location $CdkProjectFolder
cdk deploy --require-approval never
$CdkExitCode = $LASTEXITCODE
Pop-Location

if ($CdkExitCode -ne 0) { echo "CDK deployment failed."; exit 1 }

echo "Deployment successful!"