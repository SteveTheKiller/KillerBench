"""Compile the two published C# examples without executing diagnostics.

Requires an authorized full source checkout and .NET 10 SDK. Restore uses no
package feeds. A temporary console project is removed after compilation.
"""
import os
import re
import subprocess
import tempfile
from pathlib import Path
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parent


def check():
    markdown = (ROOT/'engine/docs/getting-started.md').read_text(encoding='utf-8')
    blocks = re.findall(r'```csharp\n(.*?)\n```', markdown, re.S)
    if len(blocks) != 2:
        raise ValueError('Expected two published workflow examples.')
    focused, broad = blocks
    imports = re.findall(r'^using .*?;$', focused, re.M)
    body = re.sub(r'^using .*?;\n', '', focused, flags=re.M)
    source = '\n'.join(imports)+'\ninternal static class Examples {\n'
    source += 'public static async Task Focused() {\n'+body+'\n}\n'
    source += 'public static async Task Broad(string journalFolder, IProgress<string> progress) {\n'+broad+'\n}\n'
    source += 'public static void Main() {}\n}\n'
    project = ROOT.parent/'Engine.Windows/KillerBench.Engine.Windows.csproj'
    if not project.is_file():
        raise ValueError('An authorized full source checkout is required.')
    with tempfile.TemporaryDirectory(prefix='killerbench-guide-') as directory:
        folder = Path(directory)
        (folder/'Examples.cs').write_text(source, encoding='utf-8')
        (folder/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding='utf-8')
        (folder/'Examples.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="'+escape(str(project), {'"':'&quot;'})+'" /></ItemGroup></Project>', encoding='utf-8')
        env = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1')
        subprocess.run(['dotnet', 'restore', str(folder/'Examples.csproj'), '--configfile', str(folder/'NuGet.Config'), '--disable-parallel', '-p:NuGetAudit=false'], env=env, check=True)
        subprocess.run(['dotnet', 'build', str(folder/'Examples.csproj'), '--no-restore', '--maxcpucount:1', '-v:minimal'], env=env, check=True)
    print('Both published C# examples compiled; no example or native workflow was executed.')


if __name__ == '__main__':
    check()
