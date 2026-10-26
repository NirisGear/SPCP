# Steam Player Count Plugin for MSI Afterburner

SPCP is a monitoring plugin for MSI Afterburner that adds a real-time Steam Player Count sensor.

![SPCP displaying the Steam player count in the in-game overlay](https://raw.githubusercontent.com/NirisGear/SPCP/refs/heads/main/assets/SPCP_overlay.jpg)


## Features

- Live concurrent player count for any Steam game, shown in the MSI Afterburner overlay.
- Game name resolved automatically from its AppID, or a custom label (see [Limitations](#limitations)). 
- Configurable refresh interval (see [Limitations](#limitations)).
- Optional log file for troubleshooting.

## Requirements

- MSI Afterburner with RivaTuner Statistics Server.
- An internet connection.

## Installation

1. Copy `SPCP.dll` and [`SPCP.ini`](https://github.com/NirisGear/SPCP/blob/main/files/SPCP.ini) to `MSI Afterburner\Plugins\Monitoring`. For example, `C:\Program Files (x86)\MSI Afterburner\Plugins\Monitoring`.
2. *(Optional)* Copy [`SPCP`](https://github.com/NirisGear/SPCP/blob/main/files/SPCP) to `MSI Afterburner\Help\Plugins\Monitoring`. This adds a description of the plugin to the MSI Afterburner settings window.
3. Restart MSI Afterburner.

## Uninstalling

Close MSI Afterburner, then delete:

- `SPCP.dll` and `SPCP.ini` from `MSI Afterburner\Plugins\Monitoring`.
- `SPCP` from `MSI Afterburner\Help\Plugins\Monitoring` (if installed).
- The log file, if logging was enabled.

## Getting Started

1. Open `SPCP.ini` in a text editor and set `AppId` to the Steam AppID of the target game (see [Finding an AppID](#finding-an-appid)).
2. In MSI Afterburner, open `Settings → Monitoring` and click `[...]` next to `Active hardware monitoring graphs`.
3. In the list of plugin modules, tick the checkbox next to `SPCP.dll`.
4. Select the `Steam Player Count` graph and enable `Show in On-Screen Display`.

Changes to `SPCP.ini` are applied automatically while MSI Afterburner is running, except for the game name (see [Limitations](#limitations)).

### Finding an AppID

The AppID is the number in the game's Steam store URL. For example, in `https://store.steampowered.com/app/730/`, the AppID is `730`.

## Limitations

- **Manual AppID configuration**: The game's Steam `AppID` must be specified in `SPCP.ini`. The plugin does not automatically detect the currently running game.
- **Game name updates**: The game name is read only when MSI Afterburner starts. After changing `AppId` or `Name`, restart MSI Afterburner to update the displayed label. **The player count continues to update without restarting.**
- **Steam statistics update frequency**: Steam updates player statistics according to its own schedule. A shorter refresh interval in `SPCP.ini` does not guarantee newer data.

## Build SPCP from source

Requires Visual Studio 2022 with the .NET desktop development workload and the .NET Framework 4.8 targeting pack. Run the following from a Developer Command Prompt for VS 2022:

```
git clone https://github.com/NirisGear/SPCP.git
cd SPCP
msbuild SPCP.sln -t:Rebuild -p:Configuration=Release -p:Platform=x86
```

After a successful build, `SPCP.dll` is generated in `SPCP\bin\x86\Release\net48\`.

## Credits

- Based on [LibreHardwareMonitorAfterburnerPlugin](https://github.com/ts-korhonen/LibreHardwareMonitorAfterburnerPlugin) by [ts-korhonen](https://github.com/ts-korhonen).
- Native exports provided by [DllExport](https://github.com/3F/DllExport) by [3F](https://github.com/3F).
- Player counts and game names are provided by the Steam Web API and the Steam Store API. Please review the [Steam Web API Terms of Use](https://steamcommunity.com/dev/apiterms).

## Disclaimer

This is an unofficial, community project. It is not affiliated with, endorsed by, or sponsored by Valve Corporation, Micro-Star International (MSI), or Guru3D/RivaTuner. Steam and the Steam logo are trademarks of Valve Corporation. MSI Afterburner is a product of MSI. RivaTuner Statistics Server is a product of Guru3D. All trademarks are the property of their respective owners.

## License

This project is licensed under the MIT License.