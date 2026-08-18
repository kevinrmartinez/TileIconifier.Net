using System.CommandLine;
using System.CommandLine.Invocation;
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
        
        // Requieres admin when installing for all users 

        var rootCommand = GetRootCommand();
        return rootCommand.Parse(args).Invoke();
    }

    #region ParserSetUp
    
    private enum NameOnTile { Off, Light, Dark }
    
    // Options
    // Create Shortcut
    private static readonly Option<string> OptShortcutName = new("--name", "-n") {
        Description = "Name of the shortcut",
        Required = true
    };
    private static readonly Option<FileInfo> OptShortcutTarget = new("--target", "-t") {
        Description = "Path of the shortcut's target",
        Required =  true
    };
    private static readonly Option<string[]> OptShortcutArguments = new("--arguments", "-a") {
        Description = "Arguments for the shortcut",
        DefaultValueFactory = _ => [],
        Arity = ArgumentArity.OneOrMore,
        AllowMultipleArgumentsPerToken = true
    };
    private static readonly Option<bool> OptForAllUsers = new("--all-users") {
        Description = "Create the shortcut for all users",
        DefaultValueFactory = _ => false
    };
    private static readonly Option<FileInfo?> OptShortcutIcon = new("--icon", "-i") {
        Description = "Path of the shortcut's icon",
        DefaultValueFactory = _ => null
    };
    private static readonly Option<FileInfo?> OptShortcutTileImage = new("--image", "-I") {
        Description = "Path of the image to use on the tile icon",
        DefaultValueFactory = _ => null
    };
    private static readonly Option<bool> OptNameOnTileLight = new("--name-on-tile-light") {
        Description = "Display the shortcut name on the tile; light text",
        DefaultValueFactory = _ => false,
    };
    private static readonly Option<bool> OptNameOnTileDark = new("--name-on-tile-dark") {
        Description = "Display the shortcut name on the tile; dark text",
        DefaultValueFactory = _ => false
    };
    

    private static Command GetCreateSubcommand()
    {
        Command createCommand = new Command("create", "Creates a new shortcut");
        
        // Custom shortcut
        Command customShortcut = new("custom", "Creates a custom shortcut")
        {
            OptShortcutName,
            OptShortcutTarget,
            OptShortcutArguments,
            OptForAllUsers,
            OptShortcutIcon,
            OptShortcutTileImage,
            OptNameOnTileLight,
            OptNameOnTileDark
        };
        
        customShortcut.SetAction(parseResult =>
        {
            var nameOnTile = NameOnTile.Off;
            if (parseResult.GetValue(OptNameOnTileLight)) nameOnTile = NameOnTile.Light;
            if (parseResult.GetValue(OptNameOnTileDark)) nameOnTile = NameOnTile.Dark;
            CreateCustomShortcut(
                parseResult.GetValue(OptShortcutName)!,
                parseResult.GetValue(OptShortcutTarget)!,
                parseResult.GetValue(OptShortcutArguments)!,
                parseResult.GetValue(OptForAllUsers),
                parseResult.GetValue(OptShortcutIcon),
                parseResult.GetValue(OptShortcutTileImage),
                nameOnTile
            );
        });
        // customShortcut.Aliases.Add("-c");
        createCommand.Add(customShortcut);

#if DEBUG
        Command customTestShortcut = new("custom-test", "Tests a custom shortcut")
        {
            OptShortcutName,
            OptShortcutTarget,
            OptShortcutArguments,
            OptForAllUsers,
            OptShortcutIcon,
            OptShortcutTileImage,
            OptNameOnTileLight,
            OptNameOnTileDark
        };
        customTestShortcut.SetAction(parseResult =>
        {
            var nameOnTile = NameOnTile.Off;
            if (parseResult.GetValue(OptNameOnTileLight)) nameOnTile = NameOnTile.Light;
            if (parseResult.GetValue(OptNameOnTileDark)) nameOnTile = NameOnTile.Dark;
            TestCustomShortcutOptions(
                parseResult.GetValue(OptShortcutName)!,
                parseResult.GetValue(OptShortcutTarget)!,
                parseResult.GetValue(OptShortcutArguments)!,
                parseResult.GetValue(OptForAllUsers),
                parseResult.GetValue(OptShortcutIcon),
                parseResult.GetValue(OptShortcutTileImage),
                nameOnTile
            );
        });
        createCommand.Add(customTestShortcut);
#endif

        // End Setup
        // createCommand.Aliases.Add("-C");
        return createCommand;
    }

    private static RootCommand GetRootCommand()
    {
        /*
         * TileIconifier can handle shortcuts with the same name by appending _n, where n is an incremental number
         * TODO: give an option to check if a shortcut exist, and another to delete it (filtering by name);
         * maybe call those options from CreateCustomShortcut
         */
        RootCommand rootCommand = new("Command Line Interface for TileIconifier") {
            GetCreateSubcommand()
        };
        // rootCommand.Subcommands.Add(GetCreateSubcommand());

        return rootCommand;
    }

    #endregion

    #region CreateFunctions

    private static void CreateCustomShortcut(string shortcutName, FileInfo shortcutTarget, string[] shortcutArguments,  
        bool forAllUsers, FileInfo? shortcutIcon, FileInfo? shortcutTileImage, NameOnTile nameOnTile)
    {
        var rootPath = (!forAllUsers) 
            ? CustomShortcutGetters.CustomShortcutCurrentUserPath 
            : CustomShortcutGetters.CustomShortcutAllUsersPath;
        var arguments = string.Join(" ", shortcutArguments);
        arguments = arguments.TrimEnd();
        var shortcutParams = new GenerateCustomShortcutParams(shortcutTarget.FullName, arguments, rootPath) {
            IconPath = shortcutIcon?.FullName
        };
        
        var shortcutBuilder = new OtherCustomShortcutBuilder(shortcutParams);
        var customShortcut = shortcutBuilder.GenerateCustomShortcut(shortcutName);
        var newShortcutItem = customShortcut.ShortcutItem;
        newShortcutItem.Properties.CurrentState.ShowNameOnSquare150X150Logo = true;
        if (!string.IsNullOrEmpty(shortcutParams.IconPath)) {
            /*
             * If the icon is missing: the shortcut will use the executable, but the tile will be empty
             * If the icon is not an .ico: the shortcut will have an invalid icon, but the tile should show it correctly
             *
             * TODO:
             * This segment allow us to handle two icons: an .ico for 'shortcutParams.IconPath', and an image file for 'iconBytes'
             * and RetroLinker can serve us both!
             */
            var iconBytes = Core.Utilities.ImageUtils.LoadFileToByteArray(shortcutParams.IconPath);
            if (iconBytes is not null) {
                newShortcutItem.Properties.CurrentState.MediumImage.SetImage(iconBytes, ShortcutConstantsAndEnums.MediumShortcutDisplaySize);
                newShortcutItem.Properties.CurrentState.SmallImage.SetImage(iconBytes, ShortcutConstantsAndEnums.SmallShortcutDisplaySize);
            }
        }
        
        var iconify = new TileIcon(newShortcutItem);
        iconify.RunIconify();
    }

    private static void TestCustomShortcutOptions(string shortcutName, FileInfo shortcutTarget, string[] shortcutArguments, 
        bool forAllUsers, FileInfo? shortcutIcon, FileInfo? shortcutTileImage, NameOnTile nameOnTile)
    {
        var strCheck = "O";
        var strCross = "X";
        
        Console.WriteLine(shortcutName);
        var exist = (shortcutTarget.Exists) ? strCheck : strCross;
        Console.WriteLine($"{shortcutTarget.FullName} ({exist})");
        if (shortcutArguments.Length > 0) {
            foreach (var argument in shortcutArguments)
                Console.WriteLine("\t" + argument);
        }
        else Console.WriteLine("NONE");
        Console.WriteLine(forAllUsers);
        string iconPrint;
        if (shortcutIcon is not null) {
            exist = (shortcutIcon.Exists) ? strCheck : strCross;
            iconPrint = $"{shortcutIcon.FullName} ({exist})";
        }
        else iconPrint = "[NULL]";
        Console.WriteLine(iconPrint);
        string imagePrint;
        if (shortcutTileImage is not null) {
            exist = (shortcutTileImage.Exists) ? strCheck : strCross;
            imagePrint = $"{shortcutTileImage.FullName} ({exist})";
        }
        else imagePrint = "[NULL]";
        Console.WriteLine(imagePrint);
        Console.WriteLine(nameOnTile.ToString("G"));
    }

    #endregion
}