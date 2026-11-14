# St. Mark's Bulletin Generator

An automatic church bulletin generator designed to minimize the amount of manual input required each week.

Developed for **St. Mark Evangelical-Lutheran Church (Lutheran Church–Missouri Synod)** in West Henrietta, New York.

The pastor fills out a short form each week, and the application automatically generates a print-ready PDF church bulletin.

> **Status: Work in Progress — Under Development. Do Not Use.**

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Requirements](#requirements)
- [Installation and Running](#installation-and-running)
- [Using the Application](#using-the-application)
  - [Calendar](#calendar)
  - [Church-Year Colors](#church-year-colors)
  - [Bulletin Form](#bulletin-form)
  - [Church Settings](#church-settings)
  - [Light and Dark Themes](#light-and-dark-themes)
- [USB Flash Drive Setup](#usb-flash-drive-setup)
- [Data Storage](#data-storage)
- [Hymn Database](#hymn-database)
- [Project Structure](#project-structure)
- [Technical Details](#technical-details)
- [Known Limitations and Future Work](#known-limitations-and-future-work)

---

## Overview

The application generates a folded church bulletin consisting of two landscape Letter-size pages. Each page contains two 5.5 × 8.5-inch panels.

| Page | Left Panel | Right Panel |
|---|---|---|
| 1 | Order of Service | Psalm and Gloria Patri |
| 2 | Back Cover: Welcome, Announcements, Contact Information, and Acknowledgements | Front Cover: Church Name, Image, Divine Service, and Date |

Hymn information comes from the Lutheran Service Book (LSB). Only hymn numbers and titles are printed; music and lyrics are not reproduced.

The Sunday name, appointed psalm, and church-year color are automatically determined from the selected date.

**The application does not use the internet.** All data is read from and written to local or shared files.

## Features

- **Automatic PDF Generation:** Produces a print-ready, two-page folded church bulletin.
- **Sunday Calendar:** Navigate between Sundays and identify previously saved bulletins.
- **Automatic Liturgical Information:** Determines the Sunday name, appointed psalm, and church-year color.
- **Hymn Lookup:** Automatically fills in hymn titles based on their LSB numbers.
- **Customizable Church Settings:** Edit recurring church wording without modifying the source code.
- **Rich Text Editing:** Format announcements and acknowledgements using bold, italic, underline, and color.
- **Automatic Saving:** Saves bulletin changes without requiring manual save operations.
- **Conflict Detection:** Detects changes made on another computer and preserves conflicting versions.
- **Shared Storage:** Store bulletin data on a USB flash drive or shared folder.
- **Light and Dark Themes:** Switch between parchment-inspired light and candlelit dark appearances.

---

## Requirements

- Windows (the only operating system currently built and tested).
- .NET 10 SDK.

Linux has not been tested.

## Installation and Running

Open a terminal in the project directory and run:

```powershell
dotnet run
```

If the build encounters issues after replacing or modifying files, clean the build directories and rebuild:

```powershell
Remove-Item -Recurse -Force bin, obj
dotnet build
dotnet run
```

---

## Using the Application

### Calendar

The calendar is the application's main window.

- Only Sundays are selectable.
- Sundays with saved bulletins are highlighted in gold.
- The currently selected Sunday is highlighted in burgundy.
- **Previous** and **Next** navigate one Sunday at a time.
- **This Sunday** jumps to the coming Sunday.
- The arrows beside the month name navigate between months.
- A panel beneath the calendar displays the selected Sunday and whether a bulletin has been saved.
- **Settings** opens the church settings window.
- **Light / Dark** switches between appearance themes. The selection is remembered on the current computer.

### Church-Year Colors

Each Sunday is automatically assigned a color based on its place in the church year. The color appears along the Sunday in the calendar and as a stripe beneath the bulletin form header.

| Color | Meaning | Used For |
|---|---|---|
| White (displayed as gold) | Joy and the festivals of Christ | Christmas, Epiphany, Baptism of Our Lord, Transfiguration, Easter, Holy Trinity, All Saints' Day (when on a Sunday), and the Last Sunday of the Church Year |
| Green | Growth in the faith | Sundays after the Epiphany and after Pentecost |
| Violet | Preparation and repentance | Advent and Lent |
| Red | The Holy Spirit and the Church's witness | Pentecost, Reformation Day (when on a Sunday), and the Sunday of the Passion |

The following liturgical conventions are used in `Liturgical/ChurchYear.cs`:

- Advent is violet (blue is also permitted).
- The Sunday of the Passion is red (violet is also permitted).
- Sundays between the Epiphany and Lent are green.
- Saints' days and transferred observances are not assigned colors.

### Bulletin Form

Selecting a Sunday opens its bulletin form.

| Section | Contents |
|---|---|
| Service Information | Service time, Sunday name, presiding minister, and organist |
| Worship Details | Service setting and creed |
| Hymns & Music | Organ prelude, opening hymn, hymn of the day, closing hymn, and organ postlude |
| Psalm | Psalm number, verses, Bible translation, optional verses, appointed psalm, and lectionary selection |
| Announcements | Rich-text announcements printed as bullet points under "Item of Interest" |
| Acknowledgements | Rich-text thank-you messages printed centered on the back cover |
| Cover Image | Front-cover image selection or random image selection |

**Hymn Lookup**

Entering an LSB hymn number automatically fills in its title. The title can be manually corrected if necessary.

**Psalm Selection**

The appointed psalm is automatically populated based on the selected Sunday and lectionary.

- Supported translations: ESV, KJV, and NKJV.
- A different psalm number can be entered manually.
- Leaving the verses field empty prints the entire psalm.
- Optional verses can be included.
- The three-year and one-year lectionaries are supported.
- If the requested psalm cannot be found, PDF generation stops and displays an error message.

**Automatic Saving**

Changes are automatically saved:

- A moment after typing stops.
- When switching to another Sunday.
- When closing the bulletin form.
- When pressing **Generate PDF**.

Wait for the **Saved** indicator before closing the application.

**Creating a New Sunday**

A new Sunday starts as a copy of the nearest earlier saved Sunday. The following information is carried over:

- Service time
- Presiding minister
- Organist
- Service setting
- Announcements
- Acknowledgements
- Other recurring bulletin information

The appointed psalm is not copied. It is automatically populated based on the new Sunday.

A new Sunday is not saved until something is edited. The application also indicates which earlier Sunday was used as the source.

**Conflict Detection**

If the same Sunday is modified on another computer, an amber warning banner appears with two options:

- **Keep mine:** Retain the current computer's version.
- **Use their version:** Replace the current version with the other computer's changes.

The conflicting version is preserved as a `.conflict-` file so that no changes are permanently lost.

**Generating a PDF**

Pressing **Generate PDF** creates a file named:

```text
bulletin_yyyy-MM-dd_HHmm.pdf
```

The PDF is saved in:

```text
Documents\StMarkBulletins
```

The generated PDF is automatically opened after creation.

### Church Settings

The **Settings** window controls recurring wording used throughout the bulletin.

It contains four sections:

| Section | Contents |
|---|---|
| Front Cover | Church name, town, anniversary wording, synod, and website |
| Inside Left Page | Welcome text, order-of-service sentence, and Holy Communion text |
| Back Cover | Welcome message and section headings |
| Contact Box | Six lines of church contact information |

Settings behavior:

- Every field starts with the original church wording.
- Empty fields are omitted from the generated bulletin.
- If all contact fields are empty, the contact box disappears.
- `{page}` and `{setting}` are replaced with the appropriate page number and service setting.
- `{year}` is replaced with the church's calculated anniversary wording (125th, 131st, etc.), based on the configured founding year.
- **Restore standard wording** restores the original default text.
- Changes are only saved after pressing **Save**.

Settings are stored in `church_settings.json` within the shared bulletin folder. Every computer using that folder will therefore use the same church wording.

If the settings file cannot be read, it is renamed using the following format:

```text
church_settings.json.unreadable-<date>
```

The application then uses the standard wording and displays a warning. The unreadable file is never overwritten.

**Developer Configuration**

Individual settings can be made read-only by adding their names to the list in:

```text
Models/ChurchSettingsFields.cs
```

The entire settings window can be locked using `AllReadOnly` in:

```text
Views/SettingsWindow.axaml.cs
```

The "Years of the church" wording is already read-only.

### Light and Dark Themes

The application supports two visual themes:

- **Light:** Parchment, ivory, burgundy, and gold.
- **Dark:** Wine-black, parchment-colored text, and gold.

The selected theme is remembered separately on each computer.

Theme colors and shared styles are defined in `App.axaml`.

---

## USB Flash Drive Setup

The application supports shared bulletin storage through a USB flash drive. This allows multiple computers to access the same bulletin data.

The flash drive stores the bulletin files, while generated PDFs remain on the computer used to create them.

### First-Time Setup

Perform these steps once on each computer:

1. Insert the USB flash drive.
2. Open File Explorer and create a folder named `StMarkBulletins` on the drive.
3. Start the application.
4. Press **Choose folder...** at the top of the calendar window.
5. Select the `StMarkBulletins` folder on the flash drive.
6. Confirm that the application displays the correct path under "Bulletins are kept in:".

**Important:** Complete this setup before entering any bulletin information.

### Daily Usage

1. Insert the USB flash drive.
2. Start the application.
3. Select a Sunday to open or create its bulletin.
4. Make the necessary changes.
5. Wait for the **Saved** indicator at the bottom of the form.
6. Close the application.
7. Safely eject the USB flash drive.

### Troubleshooting

If the application cannot access the shared folder:

- **Flash drive disconnected:** Reinsert the drive and press **Try again**.
- **Drive letter changed:** Press **Choose folder...** and select the `StMarkBulletins` folder again.

Changing the drive letter does not delete or lose the existing bulletins.

### Generated PDF Location

PDFs are saved locally on the computer that generated them:

```text
Documents\StMarkBulletins
```

They are not automatically saved to the USB flash drive. Copy them manually if they need to be accessed from another computer.

### Backups

USB flash drives can be lost or damaged.

It is recommended to:

- Back up the entire `StMarkBulletins` folder at least once a month.
- Create an additional backup before making major changes.
- Store backups on a computer or cloud storage service.

---

## Data Storage

The application only reads and writes files. It does not independently synchronize data between computers.

Multiple computers can share the same bulletin data through:

- USB flash drives
- OneDrive
- Dropbox
- Network-shared folders

### Shared Folder

By default:

```text
Documents\StMarkBulletins
```

The shared folder contains:

| File / Directory | Description |
|---|---|
| `Bulletins/yyyy-MM-dd.json` | One JSON file per Sunday containing the saved bulletin |
| `Bulletins/yyyy-MM-dd.json.conflict-...` | Preserved conflicting versions |
| `church_settings.json` | Shared church wording and settings |
| `hymn_corrections.tsv` | Shared hymn title corrections |

### Local Computer Storage

Stored in:

```text
%LocalAppData%\StMarkBulletin
```

| File | Description |
|---|---|
| `settings.json` | Computer-specific settings, including shared folder location and theme |
| `hymns.db` | Local SQLite hymn database |
| `hymn_corrections_local.tsv` | Local hymn corrections, merged with shared corrections |

### Generated PDFs

PDFs are stored in:

```text
Documents\StMarkBulletins\bulletin_yyyy-MM-dd_HHmm.pdf
```

Each saved Sunday retains the hymn titles as they were printed. Service setting and creed page numbers are looked up each time a PDF is generated rather than being stored with the bulletin.

---

## Hymn Database

The application uses SQLite to store hymn numbers and titles.

The original hymn data comes from:

```text
Data/hymns.tsv
```

At application startup:

1. The TSV file is loaded into the local SQLite database (`hymns.db`).
2. New hymns are added to the database.
3. Duplicate entries are ignored.
4. The database is used for hymn number and title lookups.

If a hymn number is not found, the user can manually enter its title. The application then adds the hymn to the database.

**Hymn Corrections**

If a hymn title is incorrect, the pastor can correct it directly in the bulletin form.

Corrections are saved to:

- The local corrections file: `hymn_corrections_local.tsv`
- The shared corrections file: `hymn_corrections.tsv`

Other computers retrieve the shared corrections the next time they open a Sunday.

---

## Project Structure

```text
st_marks_bulletin_generator/
│
├── Program.cs
├── App.axaml
├── App.axaml.cs
│
├── Data/
│   ├── AppSettings.cs
│   ├── BulletinStore.cs
│   ├── ChurchSettingsStore.cs
│   ├── HymnRepository.cs
│   ├── Hymn.cs
│   ├── HymnCorrections.cs
│   └── hymns.tsv
│
├── Models/
│   ├── Bulletins.cs
│   ├── SavedBulletin.cs
│   ├── ChurchSettings.cs
│   └── ChurchSettingsFields.cs
│
├── Pdf/
│   ├── BulletinDocument.cs
│   └── HtmlText.cs
│
├── Liturgical/
│   ├── SundayNames.cs
│   ├── PsalmLookup.cs
│   ├── LectionaryChoice.cs
│   └── ChurchYear.cs
│
├── Views/
│   ├── CalendarWindow.axaml
│   ├── CalendarWindow.axaml.cs
│   ├── MainWindow.axaml
│   ├── MainWindow.axaml.cs
│   ├── SettingsWindow.axaml
│   ├── SettingsWindow.axaml.cs
│   ├── CoverImagePickerWindow
│   ├── RichBox.cs
│   └── SeasonBrushes.cs
│
├── lectionary/
│   └── ...
│
└── imgs/
    ├── 1.jpg
    ├── 2.jpg
    └── ...
```

### Important Files

| File | Responsibility |
|---|---|
| `Program.cs` | Application entry point and QuestPDF Community license configuration |
| `App.axaml` / `App.axaml.cs` | Application colors, styles, icons, and theme switching |
| `Views/CalendarWindow.axaml(.cs)` | Calendar navigation, bulletin folder management, and window navigation |
| `Views/MainWindow.axaml(.cs)` | Bulletin form, input controls, hymn lookup, saving, and PDF generation |
| `Views/SettingsWindow.axaml(.cs)` | Church settings interface |
| `Pdf/BulletinDocument.cs` | QuestPDF layout and PDF generation |
| `Models/Bulletins.cs` | Bulletin data used during PDF generation |
| `Data/BulletinStore.cs` | Saves and loads individual Sunday bulletins |
| `Data/HymnRepository.cs` | SQLite hymn database management |
| `Liturgical/SundayNames.cs` | Determines the Sunday name |
| `Liturgical/PsalmLookup.cs` | Retrieves appointed psalms and psalm text |
| `Liturgical/ChurchYear.cs` | Determines church-year seasons and colors |

---

## Technical Details

### Technologies and Libraries

| Technology | Purpose |
|---|---|
| C# | Primary programming language |
| .NET 10 | Application framework |
| Avalonia 12 | Cross-platform desktop UI framework |
| QuestPDF | PDF generation |
| QuestPDF.HTML | Rich-text rendering in generated PDFs |
| AvaloniaRichEditor | Rich-text editing in the application |
| Microsoft.Data.Sqlite | SQLite database access for hymns |

QuestPDF is configured under its Community license in `Program.cs`.

### PDF Generation Workflow

PDF generation is initiated by the `Generate_Click` method in:

```text
Views/MainWindow.axaml.cs
```

The process is:

1. Read the current form inputs into a `Bulletin` object (`Models/Bulletins.cs`).
2. Retrieve the church settings.
3. Pass the bulletin, hymn repository, and church settings to `BulletinDocument`.
4. Generate the PDF at the specified output path.
5. Open the generated PDF.

The document is generated using:

```csharp
new BulletinDocument(bulletin, _repo, church).GeneratePdf(path);
```

### Lectionary Library

The `lectionary/` directory contains the church-year calendar, readings, and psalm text.

It uses the `LiturgyTools` namespace and includes:

- `lectionary/Data/*.json`
- `Psalm_ESV.csv`
- `Psalm_KJV.csv`
- `Psalm_NKJV.csv`

This library was developed by a teammate and is only read by the application.

The application interacts with it through two primary files:

- `Liturgical/SundayNames.cs`
- `Liturgical/PsalmLookup.cs`

This separation means changes to the lectionary library generally only require modifications to one or two application files.

### Theme and Styling System

Application colors are defined centrally in:

```text
App.axaml
```

Colors that change between light and dark themes have two versions:

- `<Name>`
- `Dark<Name>`

These are registered in `ThemedBrushes` within `App.axaml.cs`.

Windows reference colors using dynamic resources, for example:

```xml
{DynamicResource CardBrush}
```

This allows theme changes to be handled centrally rather than individually modifying every window.

Shared card, heading, and line styles are also defined in `App.axaml`.

Application icons (cross, gear, sun, moon, arrows, warning, and picture) are implemented as small drawings in the same file.

---

## Known Limitations and Future Work

### Current Limitations

- Very long psalms or announcement sections may cause the PDF text to shrink to fit. Printed output should be reviewed to ensure readability.
- The front-cover image selection is not saved with the Sunday. It resets to **Random** when the application restarts.
- On first launch, the application automatically uses `Documents\StMarkBulletins` without prompting the user.

### Planned Improvements

- Export functionality.
- Import functionality.
- A **Back up now** button.
- A packaged release with a self-contained build for the pastor's computer.

### Before Deployment

Before handing the application over for regular church use:

- Remove all test bulletin data.
- Delete the test `Bulletins` folder.
- Remove test hymn correction files.
- Verify service setting and creed page numbers against the printed Lutheran Service Book.
- Verify any remaining hymn titles against the LSB.
- Test the generated PDFs by printing and folding them.

---

**Developed for St. Mark Evangelical-Lutheran Church**  
West Henrietta, New York