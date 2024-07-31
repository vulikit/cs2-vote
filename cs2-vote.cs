using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities; // Added namespace
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using static CounterStrikeSharp.API.Utilities;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Core.Translations;
using System.Text;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace cs2vote;

public class cs2voteConfig : BasePluginConfig
{
    [JsonPropertyName("Prefix")]
    public string Prefix { get; set; } = "[CS2-Vote]";

}

[MinimumApiVersion(244)]
public partial class cs2votePlugin : BasePlugin, IPluginConfig<cs2voteConfig>
{
    public override string ModuleName => "Counter Strike 2 - Vote";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "varkit";
    public cs2voteConfig Config { get; set; } = new cs2voteConfig();
    public string prefix = "";

    public void OnConfigParsed(cs2voteConfig config)
    {
        prefix = $" {ChatColors.Blue}{Config.Prefix}{ChatColors.Default}";
    }

    public bool voteActive = false;
    public string question = string.Empty;
    public List<string> voteDatax = new();
    public Dictionary<string, int> voteData = new Dictionary<string, int>();
    public List<CCSPlayerController> votedPlayers = new();


    //Commands Start
    [ConsoleCommand("css_vote", "Start a vote")]
	[CommandHelper(3, "[question] [answer1] [answer2] [answerx]")]
	[RequiresPermissions("@css/vote")]
	public void OnVoteCommand(CCSPlayerController? caller, CommandInfo command)
	{
        if(voteActive)
        {
            reply(caller,$" {prefix} {ChatColors.Red}There is a vote in progress.");
            return;
        }
        question = command.GetArg(1);
        for (int i = 2; i < command.ArgCount; i++)
        {
            voteDatax.Add(command.GetArg(i));
        }
        startVote(caller);
	}

    [ConsoleCommand("css_cancelvote", "Cancel a vote")]
	[RequiresPermissions("@css/vote")]
	public void OnCancelVoteCommand(CCSPlayerController? caller, CommandInfo command)
	{
        if(!voteActive)
        {
            reply(caller,$" {prefix} {ChatColors.Red}There is no vote in progress.");
            return;
        }
        votedPlayers.Clear();
        voteData.Clear();
        voteActive = false;
        question = string.Empty;
        voteDatax.Clear();
        broadcast($" {ChatColors.Red}Voting ended by {caller.PlayerName}.");
	}

    public void startVote(CCSPlayerController player)
    {
        foreach (var item in voteDatax)
        {
            voteData.Add(item, 0);
        }
        float duration = 20; //20 secs
        voteActive = true;
        broadcast($" {ChatColors.Silver}\"{question}\" named vote started by {ChatColors.Lime}{player.PlayerName}{ChatColors.Silver}!");
        ChatMenu voteMenu = new ChatMenu($" {ChatColors.Red}=-=-=-=-= {ChatColors.Lime}{question} {ChatColors.Red}=-=-=-=-=");
        foreach (var item in voteDatax)
        {
            voteMenu.AddMenuOption(item, (x,i) =>
            {
                if(votedPlayers.Contains(x))
                {
                    reply(x, $" {ChatColors.Red}You already voted!");
                }
                reply(x,$" {prefix} {ChatColors.Lime}You voted for {ChatColors.Red}{item}{ChatColors.Lime}!");
                AddVote(item);
                votedPlayers.Add(x);
                MenuManager.CloseActiveMenu(x);
            });
        }

        Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false}).ToList().ForEach(player =>
        {
            MenuManager.CloseActiveMenu(player);
            MenuManager.OpenChatMenu(player, voteMenu);
        });

        AddTimer(duration, () =>
        {
            broadcast($" {ChatColors.Red}{question} {ChatColors.Lime}named vote is end.");
            broadcast($" {ChatColors.Red}=-=-=-=-= {ChatColors.Lime}{question} {ChatColors.Red}=-=-=-=-=");
            foreach (KeyValuePair<string, int> vote in voteData)
            {
                broadcast($" {ChatColors.Silver}{vote.Key} {ChatColors.White}- {ChatColors.Lime}{vote.Value}");
            }
            votedPlayers.Clear();
            voteData.Clear();
            voteActive = false;
            question = string.Empty;
            voteDatax.Clear();
        });
    }

    void AddVote(string candidate)
    {
        if (voteData.ContainsKey(candidate))
        {
            voteData[candidate]++;
        }
        else
        {
            voteData[candidate] = 1;
        }
    }

    public void reply(CCSPlayerController player, string message)
    {
        player.PrintToChat(message);
    }

    public void broadcast(string message)
    {
        Server.PrintToChatAll($" {prefix} {message}");
    }
    //Commands End
    //Events Starts
    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(() =>
        {
            if(voteActive)
            {
                int choice = 1;
                string message = $"<font color='White'>{question}</font>";
                foreach (KeyValuePair<string, int> vote in voteData)
                {
                    message += $"<br>!<font color='#a5feff'>{choice}</font> " + vote.Key + $" <font color='#a5feff'>[{vote.Value}]</font>";
                    choice++;
                }
                Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false}).ToList().ForEach(player =>
                {
                    player.PrintToCenterHtml(message);
                });
            }
        });
    }
    //Events End
}