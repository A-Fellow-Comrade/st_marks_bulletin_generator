# st_marks_bulletin_generator
An automatic church bulletin generator with minimal user input.




WORK IN PROGRESS. UNDER DEVELOPMENT. DO NOT USE.

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