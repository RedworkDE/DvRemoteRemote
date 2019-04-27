# DvRemoteRemote
A tool to control trains in Derail Valley via a browser

## Installation
- Download and install either [BepInEx Bleeding Edge](https://github.com/BepInEx/BepInEx) or [Unity Mod Manager](https://github.com/newman55/unity-mod-manager/).
- Download the appropriate release variant.

## Usage
- Open a browser and naviagte to the site on port 6886 on the computer running Derail Valley.
  - From the computer itself visit `http://localhost:6886/`.
  - For access from a device in the local network you need to [get the IP address on the computer](https://lmgtfy.com/?q=get+local+ip+address) and then visit `http://<ip here>:6886/`.
  - Access from a device in the internet is normally not possible.
- Click the link `Remote Remote Control`.
- Once you are inside a loco click the pair loco button.
- You will now seed the various controls available for this type of loco and be able to control it.

## Remarks
- Left is 0 / backwards / off.
- Right is max / forwards / on.
- You need to move at least once after loading the game to be registered as inside a loco.
- The background of Throttle, Break and Sander shows the actual currently applied value for this input.
  This just smooths out changes in the input value except for the sander which will drop to 0 when you run out of sand.
- Uncoupling at position 0 does not work. 1 is the front of the loco and -1 the back. Each position otside of this is a car.
- Auto sander may not get disabled properly, when in doubt fully reload the page.
- Engine Temperature for the shunter has the same scale as ingame with a range from 30°C - 120°C and background color changes at 45°C, 90°C and 105°C
- This currently only properly works with the shunter.
- While actively using the remote for some time you shouldn't have the inspector and network tab of it open as the app can generate 100 000+ web requests per hour
- This program does not include any kind of security. Anyone with network access to your computer will be able to control your trains with this.

## Building
- Create a directory junction from C:\DerailValley to your Derail Valley installation. Run `mklink /J C:\DerailValley "<Path to DV>\Derail Valley"` in a admininstrator level command prompt.
- Install at least on of BepInEx and UMM. *I use BepInEx 5.0.0.121 and UMM 0.13.0, but the exact version don't really matter.*
- Create either the compilation constant `BepInEx` or `UMM` depending on what loader you want to use.
- Install TypeScript `npm install -g typescript` (This is only required for developement).
- The project is setup to build all files into the required locations to run the mod.
- The launch profile `Derail Valley` is setup to run the game directly.
- A debug compile will load the client app (all files in /static) from the source location and recompile as needed.

## Code Setup
- `/static` contains the client app
  - `RemoteControlPage.html` contains the html and css parts (and some minor js)
  - `script.ts` contains all the logic (this file is a mess) 
  - `script.js` is the compiled version of it and should be ignored
  - `Loco.d.ts` contains definitions for the properties / actions send from the server
  - `nouislider.*` is the library used for the slider
- `RemoteControl.cs` is the server logic (this should be cleaned up)
- `Loco*.cs` contain the server side definition for properties / actions and code to load them
- `WebServer.cs` contains code to allow multiple mods to offer a shared local web site without the use of shered libraries
