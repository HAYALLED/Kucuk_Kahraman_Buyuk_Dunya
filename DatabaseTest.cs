using Godot;
using System;
using Microsoft.Data.SqlClient;

public partial class DatabaseTest : Node
{
    public override void _Ready()
    {
        TestConnection();
    }

    private void TestConnection()
    {
        string connectionString = "Server=ULAS\\SQLEXPRESS;Database=KucukKahramanBuyukDunyaDB;Integrated Security=True;TrustServerCertificate=True;";

        try
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                GD.Print("Connected to SQL Server!");

                string sql = "SELECT TOP 1 * FROM Levels;";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var firstColumn = reader.GetValue(0);
                        GD.Print($"First row, first column: {firstColumn}");
                        var dir = ProjectSettings.GlobalizePath("user://");
                        GD.Print(dir);
                    }
                    else
                    {
                        GD.Print("Query succeeded but no rows returned.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("SQL error: " + ex.Message);
        }
    }
}
