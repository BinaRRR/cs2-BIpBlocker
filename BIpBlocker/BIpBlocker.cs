using System.Data;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using Dapper;

namespace BIPblocker;

public class BIpBlocker : BasePlugin
{
    public override string ModuleName => "bIPBlocker";
    public override string ModuleAuthor => "BinaR";
    public override string ModuleVersion => "1.0";
    public override string ModuleDescription => "Block users IP using database";
    
    private const char Default = '\x01';
    private const char Green = '\x04';

    public static string PluginTag => $" {Green}[{Default}IpBlocker{Green}]{Default} ";

    private IDbConnection _DB = null!;

    public override void Load(bool hotReload)
    {
        DatabasesConnectionConfig dbConf = new DatabasesConnectionConfig("BIpBlocker");
        _DB = dbConf.ConnectToDatabase();
        _DB.Open();
        RegisterListener<Listeners.OnClientAuthorized>(OnClientConnected);
        
        Task.Run(async () =>
        {
            await _DB.ExecuteAsync("CREATE TABLE IF NOT EXISTS `banned_ips` (`ID` int not null AUTO_INCREMENT PRIMARY KEY, `IpAddress` varchar(24),`LastKnownSteamID` BIGINT UNSIGNED , `LastKnownNickname` varchar(64))");
        });
        
    }

    public override void Unload(bool hotReload)
    {
        _DB.Close();
    }
    
    public void OnClientConnected(int playerSlot, SteamID steamId)
    {
        CCSPlayerController client = Utilities.GetPlayerFromSlot(playerSlot);
        if (!client.IsValid || client.IsBot || client.UserId == -1 || client.IsHLTV)
        {
            return;
        }

        string? tempIP = client.IpAddress;
        string? clientIp = tempIP?.Split(':')[0];
        
        if (clientIp == "127.0.0.1")
        {
            return;
        }

        ulong steamId64 = steamId.SteamId64;
        string nickname = client.PlayerName;

        //Check for banned IP
        Task.Run(async () =>
        {
            var clientDbData = await _DB.QueryFirstOrDefaultAsync("SELECT `ID` FROM `banned_ips` WHERE `IpAddress`= @ClientIP",
                new
                {
                    ClientIP = clientIp
                });

            if (clientDbData != null)
            {
                int clientDbId = clientDbData.ID;
                Server.NextFrame(() =>
                {
                     Console.WriteLine("FOUND BANNED IP!");
                });

                await _DB.ExecuteAsync(
                    "UPDATE `banned_ips` SET `LastKnownSteamID`= @SteamId, `LastKnownNickname`= @Nickname WHERE `ID`= @ClientID",
                    new
                    {
                        SteamId = steamId64,
                        Nickname = nickname,
                        ClientID = clientDbId
                    }
                );
                
                Server.NextFrame(() =>
                {
                    Console.WriteLine("UPDATED BANNED CLIENT DATA");
                    Server.ExecuteCommand($"kickid {client.UserId} \"{Localizer["bannedip.ban.message"]}\"");
                });
            }
        });

        return;
    }
}