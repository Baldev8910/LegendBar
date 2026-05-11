# LegendBar Release Checklist

Use this checklist every time you build and release a new version of LegendBar.

---

## Step 1 — Prepare the Build

- [ ] Open the solution in Visual Studio (`F:\LegendBar\LegendBar.sln`)
- [ ] Switch build configuration to **Release** and platform to **x64** (top toolbar)
- [ ] **Build → Clean Solution**
- [ ] **Build → Build Solution** — confirm **0 errors** before proceeding
- [ ] Fix any errors before continuing

---

## Step 2 — Update Version Number

- [ ] Open `Package.appxmanifest`
- [ ] Update the `Version` attribute in `<Identity>`:

  ```xml
  <Identity Version="1.1.X.0" ... />
  ```

- [ ] Save the file

---

## Step 3 — Create MSIX Package

- [ ] Right-click project → **Package and Publish → Create App Packages**
- [ ] Select **Sideloading**
- [ ] Select **Yes, select a certificate** → choose your existing `.pfx`
- [ ] Leave installer location blank → click **Create**
- [ ] On the package configuration screen:
  - [ ] Check **Release** for all architectures (x86, x64, ARM64)
  - [ ] Uncheck **Debug** for all architectures
- [ ] Click **Create** and wait for packaging to complete
- [ ] Confirm output folder exists:

  ```shell
  F:\LegendBar\AppPackages\LegendBar_1.1.X.0_Test\
  ```

- [ ] Confirm these files exist in the output folder:
  - `LegendBar_1.1.X.0_x86_x64_arm64.msixbundle`
  - `LegendBar_1.1.X.0_x86_x64_arm64.cer`

---

## Step 4 — Update Inno Setup Script

Open `F:\LegendBar\LegendBarSetup.iss` and update the following:

- [ ] Line 2 — version number:

  ```ini
  #define MyAppVersion "1.1.X.0"
  ```

- [ ] Line 5 — MSIX bundle path:

  ```ini
  #define MyMsixBundle "F:\LegendBar\AppPackages\LegendBar_1.1.X.0_Test\LegendBar_1.1.X.0_x86_x64_arm64.msixbundle"
  ```

- [ ] Line 6 — certificate path:

  ```ini
  #define MyCertFile "F:\LegendBar\AppPackages\LegendBar_1.1.X.0_Test\LegendBar_1.1.X.0_x86_x64_arm64.cer"
  ```

- [ ] Step 2 in `[Run]` — certificate filename:

  ```ini
  Parameters: "-addstore ""Root"" ""{tmp}\LegendBar_1.1.X.0_x86_x64_arm64.cer"""
  ```

- [ ] Step 4 in `[Run]` — MSIX bundle filename:

  ```ini
  Parameters: "... Add-AppxPackage -Path '{tmp}\LegendBar_1.1.X.0_x86_x64_arm64.msixbundle' ..."
  ```

- [ ] Save the file

---

## Step 5 — Compile Installer

- [ ] Open **Inno Setup Compiler**
- [ ] **File → Open** → select `F:\LegendBar\LegendBarSetup.iss`
- [ ] **Build → Compile** (or press F9)
- [ ] Confirm **0 errors, 0 warnings**
- [ ] Confirm `F:\LegendBar\Installer\LegendBarSetup.exe` was created/updated

---

## Step 6 — Test the Install

- [ ] Uninstall the previous version:

  ```powershell
  Get-AppxPackage *e6524d5f* | Remove-AppxPackage
  ```

- [ ] Run `F:\LegendBar\Installer\LegendBarSetup.exe` as Administrator
- [ ] Follow the installer prompts
- [ ] Confirm desktop shortcut is created
- [ ] Launch LegendBar from the desktop shortcut

---

## Step 7 — Test Features

- [ ] Bar appears at top of screen
- [ ] Auto-hide works (hover to show, move away to hide)
- [ ] Pin/unpin works
- [ ] Clock and date display correctly
- [ ] Settings popup opens and saves correctly
- [ ] Media controls work (play/pause, next, previous)
- [ ] Volume scroll works (hover media widget and scroll)
- [ ] Click title brings media app to focus
- [ ] Volume indicator appears and fades out
- [ ] Media widget fades out when no media playing
- [ ] Launch on startup toggle works (if tested)

---

## Step 8 — Upload to GitHub

- [ ] Go to `https://github.com/Baldev8910/LegendBar/releases`
- [ ] Click **Draft a new release**
- [ ] Set tag to `v1.1.X.0`
- [ ] Set title to `LegendBar v1.1.X.0`
- [ ] Write release notes describing what changed
- [ ] Upload `F:\LegendBar\Installer\LegendBarSetup.exe`
- [ ] Click **Publish release**

---

## Important Notes

> **Trimming must stay disabled** — `<PublishTrimmed>False</PublishTrimmed>` in `.csproj`. Never re-enable this or NAudio volume/COM features will silently break in the installed version.
> **Always test the installed version** — features that work in VS Debug/Release may not work in MSIX if the code isn't packaged correctly.
> **Certificate** — the same `.pfx` certificate is reused across all versions. Never delete it.
> **DPI** — only 100% display scale is supported on all monitors. Do not change this without testing.
