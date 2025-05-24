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
    passwd varchar(255)
);

