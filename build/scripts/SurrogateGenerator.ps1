# Sample call: ."C:\repos\DevHomeGitHubExtension\build\scripts\SurrogateGenerator.ps1" -FileNameToCreate C:\repos\DevHomeGitHubExtension\build\surrogate.xml -SourceDirectory C:\repos\DevHomeGitHubExtension\obj\Release\x64\GitHubExtensionServer\linked
Param(
    [string]$FileNameToCreate = "surrogate.xml",
    [string]$SourceDirectory
)

# Initialise file with header information
@"
<?xml version="1.0" encoding="utf-8"?>
<APIScanSurrogates xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
    <Mappings>
"@ | Out-File "$FileNameToCreate" -Force

# Loop over the folders
$FindFiles = Get-ChildItem -File -filter *.dll $SourceDirectory
foreach ($file in $FindFiles) {
# Append individual fileName report to file
@"
         <Mapping>
            <SurrogateSet>
                <BinarySet>
                    <SymbolLocations>
                        <SymbolLocation>.</SymbolLocation>
                        <SymbolLocation>SRV*https://symweb</SymbolLocation>
                    </SymbolLocations>
                    <Binary path="GitHubExtensionServer\linked\$($file.Name)" />
                </BinarySet>
            </SurrogateSet>
            <Targets>
                <Binary path=".*\\$($file.Name)" pathType="Regex" />
            </Targets>
        </Mapping>
"@ | Out-File "$FileNameToCreate" -Force -Append
}

# Append special DevHomeGitHubExtension.r2r.dll case to file
@"
         <Mapping>
            <SurrogateSet>
                <BinarySet>
                    <SymbolLocations>
                        <SymbolLocation>.</SymbolLocation>
                        <SymbolLocation>SRV*https://symweb</SymbolLocation>
                    </SymbolLocations>
                    <Binary path="GitHubExtensionServer\linked\DevHomeGitHubExtension.dll" />
                </BinarySet>
            </SurrogateSet>
            <Targets>
                <Binary path=".*\\DevHomeGitHubExtension.r2r.dll" pathType="Regex" />
            </Targets>
        </Mapping>
"@ | Out-File "$FileNameToCreate" -Force -Append

# Append footer information to file
@"
</Mappings>
</APIScanSurrogates>
"@ | Out-File "$FileNameToCreate" -NoNewline -Force -Append