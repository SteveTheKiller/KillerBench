param(
    [Parameter(Mandatory = $true)][string]$ScratchDirectory,
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [switch]$NoticeOnly
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath($RepositoryRoot)
$scratch = [IO.Path]::GetFullPath($ScratchDirectory)
if ($scratch.StartsWith($repo.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase) -or $scratch -eq $repo) {
    throw 'Installer fixture output must be outside the product repository.'
}
$work = Join-Path $scratch ('wizard-state-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($work) | Out-Null
$repoXml = [Security.SecurityElement]::Escape($repo.Replace('\', '/'))
$probeXml = [Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'InstallerWizardStateProbe.cs').Replace('\', '/'))
$bench = [IO.File]::Exists((Join-Path $repo 'Assets\AppIcon.ico'))
$define = if ($bench) { 'BENCH_WIZARD' } else { '' }
$resources = if ($bench) {
    '<EmbeddedResource Include="' + $repoXml + '/Packaging/KillerLauncher/Strings.xml" LogicalName="KillerLauncher.Strings.xml" />' +
    '<Resource Include="' + $repoXml + '/Assets/AppIcon.ico" Link="Resources/AppIcon.ico" />' +
    '<Resource Include="' + $repoXml + '/Assets/AppIcon.png" Link="Resources/app-icon-master.png" />'
} else {
    '<EmbeddedResource Include="' + $repoXml + '/Packaging/KillerLauncher/Strings.xml" LogicalName="KillerLauncher.Strings.xml" Condition="Exists(''' + $repoXml + '/Packaging/KillerLauncher/Strings.xml'')" />' +
    '<EmbeddedResource Include="' + $repoXml + '/Packaging/KillerLauncher/Strings.*.xml" LogicalName="KillerLauncher.%(Filename)%(Extension)" WithCulture="false" />' +
    '<Resource Include="' + $repoXml + '/Resources/kp-icon.ico" Link="Resources/kp-icon.ico" />' +
    '<Resource Include="' + $repoXml + '/Resources/kp-icon.png" Link="Resources/kp-icon.png" />' +
    '<Resource Include="' + $repoXml + '/Resources/app-icon-master.png" Link="Resources/app-icon-master.png" Condition="Exists(''' + $repoXml + '/Resources/app-icon-master.png'')" />'
}
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net48</TargetFramework><PlatformTarget>x64</PlatformTarget>
    <LangVersion>latest</LangVersion><Nullable>enable</Nullable><ImplicitUsings>disable</ImplicitUsings>
    <UseWindowsForms>true</UseWindowsForms><UseWPF>true</UseWPF><EnableDefaultItems>false</EnableDefaultItems>
    <StartupObject>InstallerWizardStateProbe</StartupObject><DefineConstants>$define</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$probeXml" />
    <Compile Include="$repoXml/Packaging/KillerLauncher/InstallerWizard.xaml.cs;$repoXml/Packaging/KillerLauncher/LauncherStrings.cs;$repoXml/Packaging/KillerLauncher/AppIcon.cs;$repoXml/Packaging/KillerLauncher/InstallerExitCodes.cs;$repoXml/Packaging/KillerLauncher/OverlayFocus.cs;$repoXml/Packaging/KillerLauncher/ModalOverlayPanel.cs" />
    <Compile Include="$repoXml/Packaging/KillerLauncher/SetupVersionText.cs" Condition="Exists('$repoXml/Packaging/KillerLauncher/SetupVersionText.cs')" />
    <Page Include="$repoXml/Packaging/KillerLauncher/InstallerWizard.xaml" Link="InstallerWizard.xaml" />
    $resources
    <Resource Include="$repoXml/Fonts/*.ttf" Link="Fonts/%(Filename)%(Extension)" />
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3" PrivateAssets="all" />
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $work 'WizardState.csproj'
$stream = [IO.FileStream]::new($projectPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try { $bytes = [Text.UTF8Encoding]::new($false).GetBytes($project); $stream.Write($bytes, 0, $bytes.Length) }
finally { $stream.Dispose() }
& dotnet build $projectPath -c Release --artifacts-path (Join-Path $work 'artifacts') --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Installer wizard fixture build failed.' }
$executable = Join-Path $work 'artifacts\bin\WizardState\release\WizardState.exe'
& $executable --notice
if ($LASTEXITCODE -ne 0) { throw 'Installer notice state check failed.' }
if (-not $NoticeOnly) {
    if (-not $bench) { throw 'Transaction-lifetime fixture is scoped to KillerBench; pass NoticeOnly for other family launchers.' }
    foreach ($result in @(0, 5, 112, 1223)) {
        & $executable --result $result
        if ($LASTEXITCODE -ne 0) { throw ('Installer transaction state check failed for result ' + $result) }
    }
}
