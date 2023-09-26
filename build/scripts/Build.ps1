Param(
    [string]$Platform = "x64",
    [string]$Configuration = "debug",
    [string]$Version,
    [string]$ClientId,
    [string]$ClientSecret,
    [string]$BuildStep = "all",
    [string]$AzureBuildingBranch = "main",
    [switch]$IsAzurePipelineBuild = $false,
    [switch]$Help = $false
)

$StartTime = Get-Date

if ($Help) {
    Write-Host @"
Copyright (c) Microsoft Corporation and Contributors.
Licensed under the MIT License.

Syntax:
      Build.cmd [options]

Description:
      Builds GitHubExtension for Windows.

Options:

  -Platform <platform>
      Only build the selected platform(s)
      Example: -Platform x64
      Example: -Platform "x86,x64,arm64"

  -Configuration <configuration>
      Only build the selected configuration(s)
      Example: -Configuration release
      Example: -Configuration "debug,release"

  -ClientId <clientid>
      Use this GitHub OAuth ClientId

  -ClientSecret <clientsecret>
      Use this GitHub OAuth ClientSecret

  -Help
      Display this usage message.
"@
  Exit
}

# Install NuGet Cred Provider
Invoke-Expression "& { $(irm https://aka.ms/install-artifacts-credprovider.ps1) } -AddNetfx"

# Root is two levels up from the script location.
$env:Build_RootDirectory = (Get-Item $PSScriptRoot).parent.parent.FullName
$env:Build_Platform = $Platform.ToLower()
$env:Build_Configuration = $Configuration.ToLower()
$env:msix_version = build\Scripts\CreateBuildInfo.ps1 -Version $Version -IsAzurePipelineBuild $IsAzurePipelineBuild
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator')

# Set GitHub OAuth Client App configuration if build-time parameters are present
$OAuthConfigFilePath = (Join-Path $env:Build_RootDirectory "src\GitHubExtension\Configuration\OAuthConfiguration.cs")
if (![string]::IsNullOrWhitespace($ClientId)) {
    (Get-Content $OAuthConfigFilePath).Replace("%BUILD_TIME_GITHUB_CLIENT_ID_PLACEHOLDER%", $ClientId) | Set-Content $OAuthConfigFilePath
}
else {
    Write-Host "ClientId not found at Build-time"
}

if (![string]::IsNullOrWhitespace($ClientSecret)) {
    (Get-Content $OAuthConfigFilePath).Replace("%BUILD_TIME_GITHUB_CLIENT_SECRET_PLACEHOLDER%", $ClientSecret) | Set-Content $OAuthConfigFilePath
}
else {
    Write-Host "ClientSecret not found at Build-time"
}

if ($IsAzurePipelineBuild) {
  Copy-Item (Join-Path $env:Build_RootDirectory "build\nuget.config.internal") -Destination (Join-Path $env:Build_RootDirectory "nuget.config")
}

$msbuildPath = &"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe

$ErrorActionPreference = "Stop"

. (Join-Path $env:Build_RootDirectory "build\scripts\CertSignAndInstall.ps1")

Try {
  if (($BuildStep -ieq "all") -Or ($BuildStep -ieq "msix")) {
    $buildRing = "Dev"
    $newPackageName = $null
    $newPackageDisplayName = $null
    $newAppDisplayNameResource = $null
    $newWidgetProviderDisplayName = $null

    if ($AzureBuildingBranch -ieq "release") {
      $buildRing = "Stable"
      $newPackageName = "Microsoft.Windows.DevHomeGitHubExtension"
      $newPackageDisplayName = "Dev Home GitHub Extension (Preview)"
      $newAppDisplayNameResource = "ms-resource:AppDisplayNameStable"
      $newWidgetProviderDisplayName = "ms-resource:WidgetProviderDisplayNameStable"
    } elseif ($AzureBuildingBranch -ieq "staging") {
      $buildRing = "Canary"
      $newPackageName = "Microsoft.Windows.DevHomeGitHubExtension.Canary"
      $newPackageDisplayName = "Dev Home GitHub Extension (Canary)"
      $newAppDisplayNameResource = "ms-resource:AppDisplayNameCanary"
      $newWidgetProviderDisplayName = "ms-resource:WidgetProviderDisplayNameCanary"
    }

    [Reflection.Assembly]::LoadWithPartialName("System.Xml.Linq")
    $xIdentity = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Identity");
    $xProperties = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Properties");
    $xDisplayName = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}DisplayName");
    $xApplications = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Applications");
    $xApplication = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Application");
    $uapVisualElements = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/uap/windows10}VisualElements");
    $xExtensions = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Extensions");
    $uapExtension = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/uap/windows10/3}Extension");
    $uapAppExtension = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/uap/windows10/3}AppExtension");

    # Update the appxmanifest
    $appxmanifestPath = (Join-Path $env:Build_RootDirectory "src\GitHubExtensionServer\Package.appxmanifest")
    $appxmanifest = [System.Xml.Linq.XDocument]::Load($appxmanifestPath)
    $appxmanifest.Root.Element($xIdentity).Attribute("Version").Value = $env:msix_version
    if (-not ([string]::IsNullOrEmpty($newPackageName))) {
      $appxmanifest.Root.Element($xIdentity).Attribute("Name").Value = $newPackageName
    } 
    if (-not ([string]::IsNullOrEmpty($newPackageDisplayName))) {
      $appxmanifest.Root.Element($xProperties).Element($xDisplayName).Value = $newPackageDisplayName
    }
    if (-not ([string]::IsNullOrEmpty($newAppDisplayNameResource))) {
      $appxmanifest.Root.Element($xApplications).Element($xApplication).Element($uapVisualElements).Attribute("DisplayName").Value = $newAppDisplayNameResource
      $extensions = $appxmanifest.Root.Element($xApplications).Element($xApplication).Element($xExtensions).Elements($uapExtension)
      foreach ($extension in $extensions) {
        if ($extension.Attribute("Category").Value -eq "windows.appExtension") {
          $appExtension = $extension.Element($uapAppExtension)
          switch ($appExtension.Attribute("Name").Value) {
            "com.microsoft.devhome" {
              $appExtension.Attribute("DisplayName").Value = $newAppDisplayNameResource
            }
            "com.microsoft.windows.widgets" {
              $appExtension.Attribute("DisplayName").Value = $newWidgetProviderDisplayName
            }
          }
        }
      }
    }
    $appxmanifest.Save($appxmanifestPath)

    foreach ($platform in $env:Build_Platform.Split(",")) {
      foreach ($configuration in $env:Build_Configuration.Split(",")) {
        $appxPackageDir = (Join-Path $env:Build_RootDirectory "AppxPackages\$configuration")
        $solutionPath = (Join-Path $env:Build_RootDirectory "GitHubExtension.sln")
        $msbuildArgs = @(
            ($solutionPath),
            ("/p:platform="+$platform),
            ("/p:configuration="+$configuration),
            ("/restore"),
            ("/binaryLogger:GitHubExtension.$platform.$configuration.binlog"),
            ("/p:AppxPackageOutput=$appxPackageDir\GitHubExtension-$platform.msix"),
            ("/p:AppxPackageSigningEnabled=false"),
            ("/p:GenerateAppxPackageOnBuild=true"),
            ("/p:BuildRing=$buildRing")
        )

        & $msbuildPath $msbuildArgs
        if (-not($IsAzurePipelineBuild) -And $isAdmin) {
          Invoke-SignPackage "$appxPackageDir\GitHubExtension-$platform.msix"
        }
      }
    }

    # Reset the appxmanifest to prevent unnecessary code changes
    $appxmanifest = [System.Xml.Linq.XDocument]::Load($appxmanifestPath)
    $appxmanifest.Root.Element($xIdentity).Attribute("Version").Value = "0.0.0.0"
    $appxmanifest.Root.Element($xIdentity).Attribute("Name").Value = "Microsoft.Windows.DevHomeGitHubExtension.Dev"
    $appxmanifest.Root.Element($xProperties).Element($xDisplayName).Value = "Dev Home GitHub Extension (Dev)"
    $appxmanifest.Root.Element($xApplications).Element($xApplication).Element($uapVisualElements).Attribute("DisplayName").Value = "ms-resource:AppDisplayNameDev"
    $extensions = $appxmanifest.Root.Element($xApplications).Element($xApplication).Element($xExtensions).Elements($uapExtension)
    foreach ($extension in $extensions) {
      if ($extension.Attribute("Category").Value -eq "windows.appExtension") {
        $appExtension = $extension.Element($uapAppExtension)
        switch ($appExtension.Attribute("Name").Value) {
          "com.microsoft.devhome" {
            $appExtension.Attribute("DisplayName").Value = "ms-resource:AppDisplayNameDev"
          }
          "com.microsoft.windows.widgets" {
            $appExtension.Attribute("DisplayName").Value = "ms-resource:WidgetProviderDisplayNameDev"
          }
        }
      }
    }
    $appxmanifest.Save($appxmanifestPath)
  }

  if (($BuildStep -ieq "all") -Or ($BuildStep -ieq "msixbundle")) {
    foreach ($configuration in $env:Build_Configuration.Split(",")) {
      .\build\scripts\Create-AppxBundle.ps1 -InputPath (Join-Path $env:Build_RootDirectory "AppxPackages\$configuration") -ProjectName GitHubExtension -BundleVersion ([version]$env:msix_version) -OutputPath (Join-Path $env:Build_RootDirectory ("AppxBundles\$configuration\GitHubExtension_" + $env:msix_version + "_8wekyb3d8bbwe.msixbundle"))
      if (-not($IsAzurePipelineBuild) -And $isAdmin) {
        Invoke-SignPackage ("AppxBundles\$configuration\GitHubExtension_" + $env:msix_version + "_8wekyb3d8bbwe.msixbundle")
      }
    }
  }
} Catch {
  $formatString = "`n{0}`n`n{1}`n`n"
  $fields = $_, $_.ScriptStackTrace
  Write-Host ($formatString -f $fields) -ForegroundColor RED
  Exit 1
}

$TotalTime = (Get-Date)-$StartTime
$TotalMinutes = [math]::Floor($TotalTime.TotalMinutes)
$TotalSeconds = [math]::Ceiling($TotalTime.TotalSeconds) - ($TotalMinutes * 60)

if (-not($isAdmin)) {
  Write-Host @"

WARNING: Cert signing requires admin privileges.  To sign, run the following in an elevated Developer Command Prompt.
"@ -ForegroundColor GREEN
  foreach ($platform in $env:Build_Platform.Split(",")) {
    foreach ($configuration in $env:Build_Configuration.Split(",")) {
      $appxPackageDir = (Join-Path $env:Build_RootDirectory "AppxPackages\$configuration")
        Write-Host @"
powershell -command "& { . build\scripts\CertSignAndInstall.ps1; Invoke-SignPackage $appxPackageDir\GitHubExtension-$platform.msix }"
"@ -ForegroundColor GREEN
    }
  }
}

Write-Host @"

Total Running Time:
$TotalMinutes minutes and $TotalSeconds seconds
"@ -ForegroundColor CYAN