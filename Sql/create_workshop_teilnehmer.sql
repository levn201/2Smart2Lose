CREATE TABLE IF NOT EXISTS WorkshopTeilnehmer (
    GamePin       INT          NOT NULL,
    Nickname      VARCHAR(100) NOT NULL,
    AktuelleOffset INT         NOT NULL DEFAULT 0,
    QuestionCount  INT         NOT NULL DEFAULT 0,
    Punkte        INT          NOT NULL DEFAULT 0,
    LetztesUpdate DATETIME     NOT NULL,
    PRIMARY KEY (GamePin, Nickname)
);
