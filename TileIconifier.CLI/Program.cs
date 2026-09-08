using System.CommandLine;
using TileIconifier.Core.Custom;
using TileIconifier.Core.Custom.Builder;
using TileIconifier.Core.Shortcut;
using TileIconifier.Core.TileIconify;

namespace TileIconifier.CLI;

internal static class Program
{
    static int Main(string[] args)
    {
        // Console.WriteLine("Hello, World!");
        
        // Requieres admin when installing for all users
#if REMOTE
        Console.WriteLine("Press any key when ready...");
        Console.ReadKey();
#endif
        
        var rootCommand = GetRootCommand();
        return rootCommand.Parse(args).Invoke();
    }

    #region ParserSetUp
    
    // TileIconifier/Forms/Shared/FrmIconSelector.cs:86
    private static readonly List<string> _supportedImageFileTypes = [
        ".jpeg",
        ".jpg",
        ".png",
        ".bmp"
    ];
    
    private enum NameOnTile { off, light, dark }
    
    // == Options ==
    private static readonly Option<string> OptShortcutName = new("--name", "-n") {
        Description = "Name of the shortcut",
        Required = true
    };
    
    // Create Shortcut
    private static readonly Option<FileInfo> OptShortcutTarget = new("--target", "-t") {
        Description = "Path of the shortcut's target",
        Required =  true
    };
    private static readonly Option<string[]?> OptShortcutArguments = new("--arguments", "-a") {
        Description = "Arguments for the shortcut",
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
            var nameOnTile = NameOnTile.off;
            if (parseResult.GetValue(OptNameOnTileLight)) nameOnTile = NameOnTile.light;
            if (parseResult.GetValue(OptNameOnTileDark)) nameOnTile = NameOnTile.dark;
            CreateCustomShortcut(
                parseResult.GetRequiredValue(OptShortcutName),
                parseResult.GetRequiredValue(OptShortcutTarget),
                parseResult.GetValue(OptShortcutArguments),
                parseResult.GetValue(OptForAllUsers),
                parseResult.GetValue(OptShortcutIcon),
                parseResult.GetValue(OptShortcutTileImage),
                nameOnTile
            );
        });
        // customShortcut.Aliases.Add("-c");
        createCommand.Add(customShortcut);

#if DEBUG
        Command customTestShortcut = new("custom-test", "Tests the 'create custom' command")
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
            var nameOnTile = NameOnTile.off;
            if (parseResult.GetValue(OptNameOnTileLight)) nameOnTile = NameOnTile.light;
            if (parseResult.GetValue(OptNameOnTileDark)) nameOnTile = NameOnTile.dark;
            CreateCustomShortcut_Test(
                parseResult.GetRequiredValue(OptShortcutName),
                parseResult.GetRequiredValue(OptShortcutTarget),
                parseResult.GetValue(OptShortcutArguments),
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

    private static Command GetCheckSubcommand()
    {
        Command checkCommand = new Command("check", "Checks a shortcut");
        
        // End Setup
        return checkCommand;
    }
    
    private static Command GetDeleteSubcommand()
    {
        Command deleteCommand = new Command("delete", "Deletes an existing shortcut");
        Command customShortcut = new Command("custom", "Deletes a custom shortcut") {
            OptShortcutName
        };
        customShortcut.SetAction(result => DeleteCustomShortcut(result.GetRequiredValue(OptShortcutName)));
        deleteCommand.Add(customShortcut);

#if DEBUG
        Command testCustomShortcut = new Command("custom-test", "Tests the 'deletes custom' command") {
            OptShortcutName
        };
        testCustomShortcut.SetAction(result => DeleteCustomShortcut_Test(result.GetRequiredValue(OptShortcutName)));
        deleteCommand.Add(testCustomShortcut);
#endif
        
        // End Setup
        return deleteCommand;
    }

    private static RootCommand GetRootCommand()
    {
        RootCommand rootCommand = new("Command Line Interface for TileIconifier") {
            GetCreateSubcommand(),
            GetCheckSubcommand(),
            GetDeleteSubcommand()
        };
        // rootCommand.Subcommands.Add(GetCreateSubcommand());

        return rootCommand;
    }

    #endregion

    #region CreateFunctions

    private static void CreateCustomShortcut(string shortcutName, FileInfo shortcutTarget, string[]? shortcutArguments,  
        bool forAllUsers, FileInfo? shortcutIcon, FileInfo? shortcutTileImage, NameOnTile nameOnTile)
    {
        // TileIconifier.Core can handle shortcuts with the same name by appending _n, where n is an incremental number
        var rootPath = (!forAllUsers) 
            ? CustomShortcutGetters.CustomShortcutCurrentUserPath 
            : CustomShortcutGetters.CustomShortcutAllUsersPath;
        var arguments = (shortcutArguments is not null) ? string.Join(" ", shortcutArguments) : string.Empty;
        arguments = arguments.TrimEnd();
        var shortcutParams = new GenerateCustomShortcutParams(shortcutTarget.FullName, arguments, rootPath) {
            IconPath = shortcutIcon?.FullName
        };
        
        var shortcutBuilder = new OtherCustomShortcutBuilder(shortcutParams);
        var customShortcut = shortcutBuilder.GenerateCustomShortcut(shortcutName);
        var newShortcutItem = customShortcut.ShortcutItem;
        newShortcutItem.Properties.CurrentState.ShowNameOnSquare150X150Logo = nameOnTile switch {
            NameOnTile.off => false,
            _ => true
        };
        // TileIconifier/Controls/IconifierPanel/ColorPanel.cs
        newShortcutItem.Properties.CurrentState.ForegroundText = nameOnTile switch {
            NameOnTile.dark => NameOnTile.dark.ToString("G"),
            _ => NameOnTile.light.ToString("G")
        };
        
        if ((shortcutTileImage is { Exists: true }) && _supportedImageFileTypes.Contains(shortcutTileImage.Extension))
        {
            var iconBytes = Core.Utilities.ImageUtils.LoadFileToByteArray(shortcutTileImage.FullName);
            if (iconBytes is not null) {
                newShortcutItem.Properties.CurrentState.MediumImage.SetImage(iconBytes, ShortcutConstantsAndEnums.MediumShortcutDisplaySize);
                newShortcutItem.Properties.CurrentState.SmallImage.SetImage(iconBytes, ShortcutConstantsAndEnums.SmallShortcutDisplaySize);
            }
        }
        
        newShortcutItem.Properties.CommitChanges();
        var iconify = new TileIcon(newShortcutItem);
        iconify.RunIconify();
    }

    private static void CreateCustomShortcut_Test(string shortcutName, FileInfo shortcutTarget, string[]? shortcutArguments, 
        bool forAllUsers, FileInfo? shortcutIcon, FileInfo? shortcutTileImage, NameOnTile nameOnTile)
    {
        var strCheck = "O";
        var strCross = "X";
        
        Console.WriteLine(shortcutName);
        var exist = (shortcutTarget.Exists) ? strCheck : strCross;
        Console.WriteLine($"{shortcutTarget.FullName} ({exist})");
        if (shortcutArguments?.Length > 0) {
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
        
#if REMOTE
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
#endif
    }

    #endregion

    #region DeleteFunctions

    private static void DeleteCustomShortcut(string shortcutName)
    {
        void NotFoundException() => throw new ApplicationException("No custom shortcuts found.");
        
        var probableDirectory = new DirectoryInfo(Path.Combine(CustomShortcutGetters.CustomShortcutVbsPath, shortcutName));
        if (!probableDirectory.Exists) NotFoundException();
        var validCustomShortcuts = probableDirectory.GetFiles("*.vbs", SearchOption.AllDirectories);
        if  (validCustomShortcuts.Length == 0) NotFoundException();
        var customShortcut = CustomShortcut.Load(validCustomShortcuts.First().FullName);
        customShortcut.Delete();
    }

    private static void DeleteCustomShortcut_Test(string shortcutName)
    {
        var probableDirectory = new DirectoryInfo(Path.Combine(CustomShortcutGetters.CustomShortcutVbsPath, shortcutName));
        var itExists = probableDirectory.Exists;
        var itExistsStr = itExists ? "O" : "X";
        Console.WriteLine($"{probableDirectory.FullName} [{itExistsStr}]");
        if (itExists)
        {
            var validCustomShortcuts = probableDirectory.GetFiles("*.vbs", SearchOption.AllDirectories);
            if (validCustomShortcuts.Length > 0) Console.WriteLine($"{validCustomShortcuts.First().FullName}");
            else Console.WriteLine("No .vbs files found.");
        }
        
#if REMOTE
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
#endif
    }

    #endregion
}