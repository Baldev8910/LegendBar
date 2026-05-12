#define MyAppName "LegendBar (Stable Release)"
#define MyAppVersion "1.1.6.4"
#define MyAppPublisher "Baldev8910"
#define MyAppURL "https://github.com/Baldev8910/LegendBar"
#define MyMsixBundle "F:\LegendBar\AppPackages\LegendBar_1.1.6.4_Test\LegendBar_1.1.6.4_x86_x64_arm64.msixbundle"
#define MyCertFile "F:\LegendBar\AppPackages\LegendBar_1.1.6.4_Test\LegendBar_1.1.6.4_x86_x64_arm64.cer"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=F:\LegendBar\Installer
OutputBaseFilename=LegendBarSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
MinVersion=10.0.17763
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\LegendBar.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "F:\LegendBar\WindowsAppRuntimeInstall-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "{#MyCertFile}"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "{#MyMsixBundle}"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Run]
; Step 1 — Install Windows App Runtime silently
Filename: "{tmp}\WindowsAppRuntimeInstall-x64.exe"; \
    Parameters: "--quiet"; \
    StatusMsg: "Installing Windows App Runtime..."; \
    Flags: runhidden waituntilterminated

; Step 2 — Install certificate to Trusted Root
Filename: "certutil.exe"; \
    Parameters: "-addstore ""Root"" ""{tmp}\LegendBar_1.1.6.4_x86_x64_arm64.cer"""; \
    StatusMsg: "Installing certificate..."; \
    Flags: runhidden waituntilterminated

; Step 3 — Uninstall previous version if exists
Filename: "powershell.exe"; \
    Parameters: "-ExecutionPolicy Bypass -Command ""Get-AppxPackage *e6524d5f* | Remove-AppxPackage"""; \
    StatusMsg: "Removing previous version..."; \
    Flags: runhidden waituntilterminated

; Step 4 — Install MSIX bundle
Filename: "powershell.exe"; \
    Parameters: "-ExecutionPolicy Bypass -Command ""Add-AppxPackage -Path '{tmp}\LegendBar_1.1.6.4_x86_x64_arm64.msixbundle'"""; \
    StatusMsg: "Installing LegendBar..."; \
    Flags: runhidden waituntilterminated

; Step 5 — Create desktop shortcut
Filename: "powershell.exe"; \
    Parameters: "-ExecutionPolicy Bypass -Command ""$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut([Environment]::GetFolderPath('Desktop') + '\LegendBar.lnk'); $s.TargetPath = 'shell:AppsFolder\e6524d5f-966a-4e69-8120-134df47dc634_3e5d12425mc5r!App'; $s.Save()"""; \
    StatusMsg: "Creating shortcuts..."; \
    Flags: runhidden waituntilterminated

[UninstallRun]
Filename: "powershell.exe"; \
    Parameters: "-ExecutionPolicy Bypass -Command ""Get-AppxPackage *e6524d5f* | Remove-AppxPackage"""; \
    Flags: runhidden waituntilterminated; \
    RunOnceId: "RemoveApp"

Filename: "certutil.exe"; \
    Parameters: "-delstore ""Root"" ""LegendBar"""; \
    Flags: runhidden waituntilterminated; \
    RunOnceId: "RemoveCert"

[Messages]
WelcomeLabel1=Welcome to LegendBar Setup
WelcomeLabel2=This will install LegendBar {#MyAppVersion} on your computer.%n%nLegendBar is a custom top bar for Windows 11 with acrylic glass, auto-hide, and media controls.%n%nClick Next to continue.
FinishedLabel=LegendBar has been installed successfully.%n%nA shortcut has been added to your Desktop. You can also find LegendBar in the Start Menu.