using MySqlConnector;
var conn = new MySqlConnection("Server=127.0.0.1;Port=3306;Database=traffichunt;Uid=root;Pwd=;");
conn.Open();
var cmd = new MySqlCommand("SELECT Id, Status, LEFT(ErrorMessage, 250) FROM ReplyRecords ORDER BY Id LIMIT 12", conn);
var r = cmd.ExecuteReader();
while (r.Read()) Console.WriteLine(r[0] + " | " + r[1] + " | " + r[2]);
