using System.Data;
using System.Text.Json;
using MySqlConnector;

namespace BIPblocker;

public class DatabasesConnectionConfig
{
    private string _pluginName;
    private string _configFilePath;
    private DbGroup _usedDatabase;

    private string[] validDatabaseDrivers = { "mysql" };

    public DatabasesConnectionConfig(string pluginName)
    {
        _pluginName = pluginName;
        
        _configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "../../csgo/addons/counterstrikesharp/configs/databases.json");
        
        var databases = JsonSerializer.Deserialize<List<DbGroup>>(File.ReadAllText(_configFilePath));
        if (databases == null)
        {
            throw new Exception("[DatabasesConnection] Can't find databases.json file.");
        }
        
        DbGroup? usedDatabase = databases.FirstOrDefault(
            db => db.ConnectedPlugins!.Contains(pluginName)
        );
        if (usedDatabase != null)
        {
            _usedDatabase = usedDatabase;
        }
        else
        {
            throw new Exception("[DatabasesConnection] Can't locate the entry for the plugin.");
        }
    }

    public MySqlConnection ConnectToDatabase()
    {
        return new MySqlConnection(
            $"Server={_usedDatabase.Hostname};User={_usedDatabase.Login};Password={_usedDatabase.Password};Database={_usedDatabase.Database};");
    }
}

public class DbGroup
{
    public string? GroupName { get; set; }
    public string Hostname { get; set; }
    public string Login { get; set; }
    public string Password { get; set; }
    public string Database { get; set; }
    public string[]? ConnectedPlugins { get; set; }
    public string? Driver { get; set; }
}