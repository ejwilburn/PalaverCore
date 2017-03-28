## Intial Setup

1) run **npm install**
2) run **bower install**
3) Put a cert named Palaver.pfx in project root with no password.

## Semantic UI Tasks

**gulp build-sui** - rebuild Semantic UI CSS and JS files.
**gulp clean-sui** - clean shit
**gulp watch-sui** - watch for changes in wwwroot/lib/semantic/src and rebuild on the fly

All of those build tasks can also be run from the command pallette (Ctrl+Shift+P) via Tasks Run Task.  I'd recommend not runing watch-sui that way though as the built-in task runner in VSCode can only run one task at a time so you'd have to kill the watch to build or debug Palaver.

~/semantic.json is the base config file for semantic that tells gulp where to find it and which components to include.  The rest of semantic customization is done under wwwroot/lib/semantic/src.