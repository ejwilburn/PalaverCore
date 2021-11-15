## Initial Setup

1) run `npm install`
2) install libman CLI with `dotnet tool install -g Microsoft.Web.LibraryManager.Cli`, then install libraries with `libman install`
3) copy example.appsettings.json to appsettings.json and customize
4) copy example.appsettings.Environment.json to appsettings.Development.json (or whatever environment name you need) and customize.
5) install dotnet-ef: `dotnet tool install --global dotnet-ef`
6) build (command line: `dotnet build`) then create the database with `dotnet-ef database update`

## Publishing

1) Modify the publish task in .vscode/tasks.json to fit your environment.
2) Open the command pallette and choose Tasks: Run Task then choose publish.  The output will be placed in the release dir, deploy as needed.

## Semantic UI Tasks - Ignore Currently

**gulp build-sui** - rebuild Semantic UI CSS and JS files.
**gulp clean-sui** - clean shit
**gulp watch-sui** - watch for changes in wwwroot/lib/semantic/src and rebuild on the fly

All of those build tasks can also be run from the command pallette (Ctrl+Shift+P) via Tasks Run Task.  I'd recommend not running watch-sui that way though as the built-in task runner in VSCode can only run one task at a time so you'd have to kill the watch to build or debug Palaver.

~/semantic.json is the base config file for semantic that tells gulp where to find it and which components to include.  The rest of semantic customization is done under wwwroot/lib/semantic/src.
