Create Database KahootDatabase;
use KahootDatabase; 

Create Table test(
	id int primary key auto_increment,
    Name varchar(255),
    ErstelltAm Datetime
);

Create Table AdminUser(
	User_ID int primary key auto_increment,
    Username varchar(255),
    password varchar(255)
);

INSERT INTO AdminUser (Username, Password)
VALUES 
('admin', 123),
('levin', 123);


Create Table CreaterUser(
	Creater_ID int primary key auto_increment,
    Username varchar(255),
    password varchar(255)
);

INSERT INTO CreaterUser (Username, Password)
VALUES 
('User', 222);

-- Drop table AdminUser;