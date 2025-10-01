Create Database KahootDatabase;
use KahootDatabase; 


-- Drop table AdminUser; 							Tabelle Löschen
-- Delete from fragebogen where Join_ID = 1212;		Inhalt löschen 
                        
create table dasboarduser (
	ID_USER int primary key auto_increment, 
    password varchar(255),
    User varchar(255),
    role varchar(255)
);

Create Table PlayerPoints(
	ID_Session int primary key auto_increment,
    User_Nickname varchar(255),
    SessionPints int, 
    GamePin int ,
    CorrectAnswered int, 
    PossibleAnswers int, 
    saveTime DATETIME DEFAULT CURRENT_TIMESTAMP
    );

CREATE TABLE Fragen (
    ID INT AUTO_INCREMENT PRIMARY KEY,
    FragebogenID INT NOT NULL,
    Fragestellung TEXT NOT NULL,
    Antwort1 TEXT NOT NULL,
    IstAntwort1Richtig TINYINT(1),
    Antwort2 TEXT NOT NULL,
    IstAntwort2Richtig TINYINT(1),
    Antwort3 TEXT NOT NULL,
    IstAntwort3Richtig TINYINT(1),
    Antwort4 TEXT NOT NULL,
    IstAntwort4Richtig TINYINT(1),
    CONSTRAINT chk_mindestens_eine_richtig
        CHECK (
            IstAntwort1Richtig = 1
            OR IstAntwort2Richtig = 1
            OR IstAntwort3Richtig = 1
            OR IstAntwort4Richtig = 1
        )
);


CREATE TABLE Fragebogen (
    ID INT AUTO_INCREMENT PRIMARY KEY,
	Join_ID INT,
    Titel TEXT NOT NULL,
    Kategorie TEXT NOT NULL, 
    Autor TEXT NOT NULL,
    ErstelltAm DATETIME DEFAULT CURRENT_TIMESTAMP
);


