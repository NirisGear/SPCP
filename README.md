# Steam Player Count Plugin for MSI Afterburner

Displays the live player count of any Steam game directly in your in-game overlay. SPCP is a monitoring plugin for MSI Afterburner that adds a real-time Steam Player Count sensor. The sensor appears in the on-screen display (the RivaTuner Statistics Server overlay) next to FPS, temperatures, and other hardware metrics.

## Features

- Live concurrent player count for any Steam game, shown in the MSI Afterburner overlay.
- Game name resolved automatically from its AppID, or a custom label of your choice.
- Configurable refresh interval; most settings are reloaded on the fly, with no restart.
- Lightweight: one small request every few minutes. If a request fails, the last valid value stays on screen.
- Optional log file for troubleshooting.

## Requirements

- MSI Afterburner with RivaTuner Statistics Server.
- An internet connection.

## Installation

1. Extract `SPCP.dll` and [`SPCP.ini`](https://github.com/NirisGear/SPCP/blob/main/files/SPCP.ini) to the `Plugins\Monitoring` folder of your MSI Afterburner installation. For example: `C:\Program Files (x86)\MSI Afterburner\Plugins\Monitoring`.
2. *(Optional)* Extract [`SPCP`](https://github.com/NirisGear/SPCP/blob/main/files/SPCP) and place it in `Help\Plugins\Monitoring`. It adds a description of the plugin to the MSI Afterburner settings window.
3. Restart MSI Afterburner.

## Uninstalling

Close MSI Afterburner, then delete:

- `SPCP.dll` and `SPCP.ini` from `MSI Afterburner\Plugins\Monitoring`.
- `SPCP` from `MSI Afterburner\Help\Plugins\Monitoring` (if you installed it).
- The log file, if you enabled logging.

## Getting Started

1. Open `SPCP.ini` in a text editor and set **AppId** to the Steam AppID of your game (see [Finding an AppID](#finding-an-appid)).
2. In MSI Afterburner, open **Settings → Monitoring** and click **[...]** next to **Active hardware monitoring graphs**.
3. In the list of plugin modules, tick the checkbox next to `SPCP.dll`.
4. Select the **Steam Player Count** graph and enable **Show in On-Screen Display**.

Changes to `SPCP.ini` are applied automatically while MSI Afterburner is running, except for the game name (see [Limitations](#limitations)).

### Finding an AppID

The AppID is the number in the game's Steam store URL. For example, in `https://store.steampowered.com/app/730/`, the AppID is `730`.

## Limitations

- **The AppID must be set manually** in `SPCP.ini`. The plugin does not detect the game you are playing.
- **The game name only updates when MSI Afterburner starts.** Plugin names and labels are read once at startup; thereafter, MSI Afterburner accepts only numeric (float) values from the plugin. After changing `AppId` or `Name`, restart MSI Afterburner to update the game name label. **The player count itself updates without a restart**.
- The count reflects what the Steam Web API reports and is refreshed only at the interval you configure.

## Build SPCP from source

Requires Visual Studio 2022 with the .NET desktop development workload and the .NET Framework 4.8 targeting pack. Run the following from a Developer Command Prompt for VS 2022:

```
git clone https://github.com/NirisGear/SPCP.git
cd SPCP
msbuild SPCP.sln -t:Rebuild -p:Configuration=Release -p:Platform=x86
```

## Credits

- Based on [LibreHardwareMonitorAfterburnerPlugin](https://github.com/ts-korhonen/LibreHardwareMonitorAfterburnerPlugin) by [ts-korhonen](https://github.com/ts-korhonen).
- Native exports provided by [DllExport](https://github.com/3F/DllExport) by [3F](https://github.com/3F).
- Player counts and game names are provided by the Steam Web API and the Steam Store API. Please review the [Steam Web API Terms of Use](https://steamcommunity.com/dev/apiterms).

## Disclaimer

This is an unofficial, community project. It is not affiliated with, endorsed by, or sponsored by Valve Corporation, Micro-Star International (MSI), or Guru3D/RivaTuner. Steam and the Steam logo are trademarks of Valve Corporation. MSI Afterburner is a product of MSI. RivaTuner Statistics Server is a product of Guru3D. All trademarks are the property of their respective owners.

## License

This project is licensed under the MIT License.