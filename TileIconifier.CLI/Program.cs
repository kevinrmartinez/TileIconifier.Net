using System.CommandLine;
using TileIconifier.Core.Custom;
using TileIconifier.Core.Custom.Builder;
using TileIconifier.Core.Shortcut;
using TileIconifier.Core.TileIconify;

namespace TileIconifier.CLI;

class Program
{
    static int Main(string[] args)
    {
        // Console.WriteLine("Hello, World!");
        
        // Custom Shortcut
        Option<string> shortcutName = new("--name", "-n") {
            Description = "Name of the shortcut",
            Required = true
        };
        Option<FileInfo> shortcutTarget = new("--target", "-t") {
            Description = "Path of the shortcut's target",
            Required =  true
        };
        Option<string[]> shortcutArguments = new("--arguments", "-a") {
            Description = "Arguments for the shortcut",
            DefaultValueFactory = parsing => [],
            Arity = ArgumentArity.ZeroOrMore
        };
        Option<bool> forAllUsers = new("--all-users") {
            Description = "Create the shortcut for all users",
            DefaultValueFactory = parsing => false
        };
        Option<FileInfo?> shortcutIcon = new("--icon", "-i") {
            Description = "Path of the shortcut's icon",
            DefaultValueFactory = parsing => null
        };
        Command customShortcut = new("custom", "Creates a custom shortcut")
        {
            shortcutName,
            shortcutTarget,
            shortcutArguments,
            forAllUsers,
            shortcutIcon
        };
        customShortcut.SetAction(parseResult => CreateCustomShortcut(
            parseResult.GetValue(shortcutName)!,
            parseResult.GetValue(shortcutTarget)!,
            parseResult.GetValue(shortcutArguments)!,
            parseResult.GetValue(forAllUsers),
            parseResult.GetValue(shortcutIcon)
        ));

        Command customTestShortcut = new("custom-test", "Tests a custom shortcut")
        {
            shortcutName,
            shortcutTarget,
            shortcutArguments,
            forAllUsers,
            shortcutIcon
        };
        customTestShortcut.SetAction(parseResult => TestCustomShortcutOptions(
            parseResult.GetValue(shortcutName)!,
            parseResult.GetValue(shortcutTarget)!,
            parseResult.GetValue(shortcutArguments)!,
            parseResult.GetValue(forAllUsers),
            parseResult.GetValue(shortcutIcon)
        ));

        RootCommand rootCommand = new("Command Line Interface for TileIconifier");
        rootCommand.Subcommands.Add(customShortcut);
        rootCommand.Subcommands.Add(customTestShortcut);
        
        return rootCommand.Parse(args).Invoke();
    }

    private static void CreateCustomShortcut(string shortcutName, FileInfo shortcutTarget, string[] shortcutArguments,  
        bool forAllUsers, FileInfo? shortcutIcon)
    {
        var rootPath = (forAllUsers) 
            ? CustomShortcutGetters.CustomShortcutCurrentUserPath 
            : CustomShortcutGetters.CustomShortcutAllUsersPath;
        var arguments = string.Join(" ", shortcutArguments);
        arguments = arguments.TrimEnd();
        var shortcutParams = new GenerateCustomShortcutParams(shortcutTarget.FullName, arguments, rootPath) {
            IconPath = shortcutIcon?.FullName ?? string.Empty
        };
        
        var shortcutBuilder = new OtherCustomShortcutBuilder(shortcutParams);
        var customShortcut = shortcutBuilder.GenerateCustomShortcut(shortcutName);
        var newShortcutItem = customShortcut.ShortcutItem;
        if (!string.IsNullOrEmpty(shortcutParams.IconPath)) {
            var iconBytes = Core.Utilities.ImageUtils.LoadFileToByteArray(shortcutParams.IconPath);
            newShortcutItem.Properties.CurrentState.MediumImage.SetImage(iconBytes, ShortcutConstantsAndEnums.MediumShortcutDisplaySize);
            newShortcutItem.Properties.CurrentState.SmallImage.SetImage(iconBytes, ShortcutConstantsAndEnums.SmallShortcutDisplaySize);
        }
        
        var iconify = new TileIcon(newShortcutItem);
        iconify.RunIconify();
    }

    private static void TestCustomShortcutOptions(string shortcutName, FileInfo shortcutTarget,
        string[] shortcutArguments,
        bool forAllUsers, FileInfo? shortcutIcon)
    {
        Console.WriteLine(shortcutName);
        Console.WriteLine(shortcutTarget.FullName);
        if (shortcutArguments.Length > 0) {
            foreach (var argument in shortcutArguments)
                Console.WriteLine("\t" + argument);
        }
        else Console.WriteLine("NONE");
        Console.WriteLine(forAllUsers);
        Console.WriteLine(shortcutIcon?.FullName ?? "NULL");
    }
}