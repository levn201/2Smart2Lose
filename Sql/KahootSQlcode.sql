SELECT * FROM kahootdatabase.aspnetusers;


-- Bild- und Link-URL pro Frage hinzufügen
ALTER TABLE Fragen
    ADD COLUMN BildUrl VARCHAR(500) NULL,
    ADD COLUMN LinkUrl VARCHAR(500) NULL;
