# Max Players Plus

BepInEx mod for Cheese Rolling. Raises the host's max-players slider past the
vanilla cap of 12 (Steam allows up to 250), and fixes the player spawn line so
it doesn't stretch across the map when you have more players.

## What it does

- Raises the max value on the Host Settings max-players slider. Configurable,
  default 64.
- Vanilla spawns players in a line 12 units apart. With enough players that
  line gets absurdly long. This mod scales the spacing down so the line never
  exceeds a configured width (default 132, same width vanilla uses for 12
  players).
- Logs a warning if entity IDs get close to the 255 cap, since that's a hard
  engine limit and can break maps with too many players/entities.

Config is written to BepInEx's config folder after first run
(`MaxPlayersPlus.cfg`).

## Building

Requirements:
- .NET SDK (netstandard2.1 target)
- A copy of Cheese Rolling with BepInEx already installed

1. Clone the repo.
2. Set `GameDir` to your Cheese Rolling install if it's not at the default
   Steam path. Either edit `MaxPlayersPlus.csproj` or pass it on the command
   line:
   ```
   dotnet build -p:GameDir="C:\Path\To\Cheese Rolling"
   ```
3. Build:
   ```
   dotnet build
   ```

The build copies the compiled DLL straight into
`<GameDir>\BepInEx\plugins\MaxPlayersPlus`. Launch the game and it's loaded.
