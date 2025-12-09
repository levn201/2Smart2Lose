public IActionResult Index()
        {
            return View();
        }

        private string connectionString =
        "Server = mysql-125d0d80-lev-e3af.h.aivencloud.com; " +
        "Port=25117;" +
        "Database=TestDatabase;" +
        "Uid=avnadmin;" +
        "Pwd=AVNS_yvuiiwChL73T9VSv25P;";
        //"Server=localhost;Database=Bankverbindung;Uid=root;Pwd=;";




        // connection zur Datenbank -> kann immer aufgerufen werden 
        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }

        public string StatusMessage { get; set; }

        //Sachen auslesen
        public void OpenConnection()
        {
            using (MySqlConnection connection = GetConnection())
            {
                try
                {
                    connection.Open();

                    string query = "SELECT spalten FROM tabelle ;";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    MySqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        int id = reader.GetInt32("ID");
                        string name = reader.GetString("Name");
                        decimal kontoauszug = reader.GetDecimal("Kontoauszug");
                        string monat = reader.GetString("Monat");

                        StatusMessage = $"ID: {id} | Name: {name} | Betrag: {kontoauszug} € | Monat: {monat}";
                    }

                    reader.Close();
                }
                catch (Exception ex)
                {
                    StatusMessage = "Fehler:";
                    
                }
            }
        }

        //Einschreiben in die Datenbank
        public void InsertKontoauszug(string name, decimal betrag, string monat)
        {
            using (MySqlConnection connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string insertQuery = "INSERT INTO Kontoauszuege (Name, Kontoauszug, Monat) VALUES (@name, @betrag, @monat);";

                    using (MySqlCommand command = new MySqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        command.Parameters.AddWithValue("@betrag", betrag);
                        command.Parameters.AddWithValue("@monat", monat);

                        int rowsAffected = command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = "Fehler beim Einfügen:";
                }
            }
        }

        // Log -> bei jedem eintrag wird automatisch ein logbeitrag hinzugefügt 
        public void InsertLog(string benutzername, decimal betrag, string monat, string aktion)
        {
            using (MySqlConnection connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string logQuery = "INSERT INTO Logs (Benutzername, Betrag, Monat, Aktion, Zeitstempel) " +
                                      "VALUES (@benutzername, @betrag, @monat, @aktion, @zeitstempel);";

                    using (MySqlCommand command = new MySqlCommand(logQuery, connection))
                    {
                        command.Parameters.AddWithValue("@benutzername", benutzername);
                        command.Parameters.AddWithValue("@betrag", betrag);
                        command.Parameters.AddWithValue("@monat", monat);
                        command.Parameters.AddWithValue("@aktion", aktion);
                        command.Parameters.AddWithValue("@zeitstempel", DateTime.Now);

                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = "Fehler beim Loggen:";
                }
            }
        }


        public void TestConnection()
        {
            using (MySqlConnection connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    StatusMessage = "? Verbindung erfolgreich hergestellt.";
                }
                catch (Exception ex)
                {
                    StatusMessage = "? Fehler bei der Verbindung:";
                    StatusMessage = ex.Message;
                }
            }
        }