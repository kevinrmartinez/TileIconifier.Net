# TileIconifier.CLI (tileico)
A command-line interface for the [TileIconifier.Core](../TileIconifier.Core) library. 
It aims to create and delete tile icons from the command-line trading off some customization, while still being compatible with the graphic interface,
meaning that the user can create a icon with the CLI, and modify it later with GUI.

--------------

## Instructions
The CLI uses plain options to separate the different operations: 

- First tier option defines the desired operation (`create`, `delete`)
- Second tier option defines the shortcut type (`simple`, `custom`)

**Basic tile creation usage:**

`create simple <arguments>`  
_Not implemented yet._ 

--------------

**Custom shortcut creation:**

`create custom <arguments>`  
```
  -n, --name <name> (REQUIRED)      Name of the shortcut
  -t, --target <target> (REQUIRED)  Path of the shortcut's target
  -a, --arguments <arguments>       Arguments for the shortcut
  --all-users                       Create the shortcut for all users
  -i, --icon <icon>                 Path of the shortcut's icon
  -I, --image <image>               Path of the image to use on the tile icon
  --overwrite                       If the shortcut already exists, it will be overwritten
  --name-on-tile-light              Display the shortcut name on the tile; light text
  --name-on-tile-dark               Display the shortcut name on the tile; dark text
  -?, -h, --help                    Show help and usage information
```

Example: `tileico create custom -n "Super Tilt Bros" -t C:\Retroarch-x64\retroarch.exe -a "-L fceumm D:\Games\tilt_no_network_unrom_(E).nes" -i D:\Games\tilt.ico -I D:\Games\tilt.png --name-on-tile-light`  
***For now, tileico won't convert .ico into image files, so the `--image` option is required and the user will need an existing image file to use there. If it's not passed, the tile icon will be empty.***

If `--all-users` is set, the shortcut will be created for all users (requires admin), otherwise it will be created only for the current user.  
If `--overwrite` is set, the program will look for an Iconified shortcut with same name, if it exists, it will delete it before creating the new one.
Otherwise, the new shortcut will be renamed with an appended number (`Super Tilt Bros` -> `Super Tilt Bros_1`).  
If neither `--name-on-tile-light` or `--name-on-tile-dark` are set, the medium-sized tile icon won't show its name.

--------------

**Custom shortcut deletion:**

`delete custom --name <name>`  
It will look for an icon with that name, delete it if it exists, or return a message if it couldn't find it.

Example: `tileico delete custom --name "Super Titl Bros"`