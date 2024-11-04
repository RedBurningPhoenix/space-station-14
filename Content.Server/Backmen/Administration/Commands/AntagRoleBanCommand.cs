﻿using System.Linq;
using System.Text;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Backmen.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class AntagantagbanCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IBanManager _bans = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public string Command => "antagban";
    public string Description => Loc.GetString("cmd-antagban-desc");
    public string Help => Loc.GetString("cmd-antagban-help");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        string target;
        string job;
        string reason;
        uint minutes;
        if (!Enum.TryParse(_cfg.GetCVar(CCVars.RoleBanDefaultSeverity), out NoteSeverity severity))
        {
            Logger.WarningS("admin.antagban", "Role ban severity could not be parsed from config! Defaulting to medium.");
            severity = NoteSeverity.Medium;
        }

        switch (args.Length)
        {
            case 3:
                target = args[0];
                job = args[1];
                reason = args[2];
                minutes = 0;
                break;
            case 4:
                target = args[0];
                job = args[1];
                reason = args[2];

                if (!uint.TryParse(args[3], out minutes))
                {
                    shell.WriteError(Loc.GetString("cmd-antagban-minutes-parse", ("time", args[3]), ("help", Help)));
                    return;
                }

                break;
            case 5:
                target = args[0];
                job = args[1];
                reason = args[2];

                if (!uint.TryParse(args[3], out minutes))
                {
                    shell.WriteError(Loc.GetString("cmd-antagban-minutes-parse", ("time", args[3]), ("help", Help)));
                    return;
                }

                if (!Enum.TryParse(args[4], ignoreCase: true, out severity))
                {
                    shell.WriteLine(Loc.GetString("cmd-antagban-severity-parse", ("severity", args[4]), ("help", Help)));
                    return;
                }

                break;
            default:
                shell.WriteError(Loc.GetString("cmd-antagban-arg-count"));
                shell.WriteLine(Help);
                return;
        }

        var located = await _locator.LookupIdByNameOrIdAsync(target);
        if (located == null)
        {
            shell.WriteError(Loc.GetString("cmd-antagban-name-parse"));
            return;
        }

        var targetUid = located.UserId;
        var targetHWid = located.LastHWId;

        _bans.CreateAntagban(targetUid, located.Username, shell.Player?.UserId, null, targetHWid, job, minutes, severity, reason, DateTimeOffset.UtcNow);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var durOpts = new CompletionOption[]
        {
            new("0", Loc.GetString("cmd-antagban-hint-duration-1")),
            new("1440", Loc.GetString("cmd-antagban-hint-duration-2")),
            new("4320", Loc.GetString("cmd-antagban-hint-duration-3")),
            new("10080", Loc.GetString("cmd-antagban-hint-duration-4")),
            new("20160", Loc.GetString("cmd-antagban-hint-duration-5")),
            new("43800", Loc.GetString("cmd-antagban-hint-duration-6")),
        };

        var severities = new CompletionOption[]
        {
            new("none", Loc.GetString("admin-note-editor-severity-none")),
            new("minor", Loc.GetString("admin-note-editor-severity-low")),
            new("medium", Loc.GetString("admin-note-editor-severity-medium")),
            new("high", Loc.GetString("admin-note-editor-severity-high")),
        };

        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(),
                Loc.GetString("cmd-antagban-hint-1")),
            2 => CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<AntagPrototype>(),
                Loc.GetString("cmd-antagban-hint-2")),
            3 => CompletionResult.FromHint(Loc.GetString("cmd-antagban-hint-3")),
            4 => CompletionResult.FromHintOptions(durOpts, Loc.GetString("cmd-antagban-hint-4")),
            5 => CompletionResult.FromHintOptions(severities, Loc.GetString("cmd-antagban-hint-5")),
            _ => CompletionResult.Empty
        };
    }
}