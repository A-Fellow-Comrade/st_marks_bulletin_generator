# st_marks_bulletin_generator
An automatic church bulletin generator with minimal user input.



WORK IN PROGRESS. UNDER DEVELOPMENT. DO NOT USE.

## Using a USB flash drive
The bulletins are stored as pdf / json files in one folder. Put that folder on a
USB flash drive and any computer running the app can use it.

### First-time setup (once per computer)
1. Plug the flash drive into the computer.
2. Open File Explorer, open the flash drive, and create a folder named
   `StMarkBulletins` (the folder only has to exist once).
3. Start the app. At the top of the window, press **Choose folder...** and
   pick that `StMarkBulletins` folder on the flash drive.
4. The grey box should now say "Bulletins are kept in:" followed by the
   flash drive's path. Do this BEFORE entering any bulletin.

### Every day
1. Plug in the flash drive, then start the app.
2. Click a Sunday to open or start its bulletin. Gold Sundays have been saved.
3. Edits save by themselves a moment after you stop typing. Wait for
   "Saved" to appear at the bottom of the form before you close the app.
4. When you are done, close the app, then eject the flash drive.

### If the app says it can't reach the folder
- The flash drive may not be plugged in: plug it in and press **Try again**.
- The drive may have a new letter (for example E: became F:): press
  **Choose folder...** and pick the folder on the flash drive again.
  Your bulletins are not lost.

### Where PDFs go
Generated PDFs are saved in `Documents\StMarkBulletins` on the computer you
are using, not on the flash drive. Copy them to the flash drive if you need
them on another computer.

### Backups
A flash drive can be lost or fail. Once a month (and before any big change),
copy the whole `StMarkBulletins` folder from the flash drive to the computer
or to cloud storage.


# Description of Hymn part of the project
    This project uses SQLite to store the Hymn numbers and titles in a database. The numbers are imported via a TSV file. If the hymns are not in the database, the user can type in the title and the program will add it to the database.
    Additionally, uploading another TSV will import new hymns and update the database, duplicates will be ignored.

    The input window:
        Views/MainWindow.axaml defines the layout of the window. It contains the text boxes, dropdowns, and the Generate PDF button.

        Views/MainWindow.axaml.cs is the behavior behind it, such as the hymn title lookup and the button’s click handler. It also handles the importing new Hymns feature.

        App.axaml.cs is what actually opens the window. It creates a MainWindow at startup, and Program.cs is the entry point that starts the whole app.

    The Bulletin PDF:

        Pdf/BulletinDocument.cs generates the PDF. It contains the QuestPDF layout for both pages, and it takes the filled-in Bulletin and looks up hymn titles in the database.

        The PDF is triggered from Generate_Click in Views/MainWindow.axaml.cs. That method reads the form into a Bulletin (Models/Bulletin.cs), then calls new BulletinDocument(bulletin, _repo).GeneratePdf(path) to write the file and open it.

#TONY TODO

    The Psalm currently just fetches the nearest Sunday. You need to change it to so that it works for the calendar.

    Also, for the UI, the psalm needs a UI. The user should be able to choose 1) the version, 2) whether or not to use the extended psalm version (if available). Also, they should be able to see the reference for the psalm, and change the verses.