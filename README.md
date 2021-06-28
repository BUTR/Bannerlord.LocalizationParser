# Bannerlord.LocalizationParser

## Using as Tool
### Installation
1. Install the latest [.NET 5.0 Runtime](https://dotnet.microsoft.com/download/dotnet/5.0/runtime).
2. Run `dotnet tool install --global Bannerlord.LocalizationParser` to install the tool.

### Usage
To run, use `bllocparser --game-folder "H:\\SteamLibrary\\steamapps\\common\\Mount & Blade II Bannerlord" --output LocalizationStrings.csv`

## Using as a Standalone Executable
### Installation
1. Download the latest [release](https://github.com/BUTR/Bannerlord.LocalizationParser/releases/latest).
2. If a non `self-contained` executable is downloaded, install the latest [.NET 5.0 Runtime](https://dotnet.microsoft.com/download/dotnet/5.0/runtime).
3. Extract the file somewhere.

### Usage
To run, use `Bannerlord.LocalizationParser.exe --game-folder "H:\\SteamLibrary\\steamapps\\common\\Mount & Blade II Bannerlord" --output LocalizationStrings.csv`

## Output
It will output a .csv file with the following format:
|Assembly                     |Text                                                                                                                               |
|-----------------------------|-----------------------------------------------------------------------------------------------------------------------------------|
|TaleWorlds.CampaignSystem.dll|{=TauRjAud}{NAME} of the {FACTION}                                                                                                 |
|TaleWorlds.CampaignSystem.dll|{=vvCwVo7i}{DAMAGE} {DAMAGE_TYPE}                                                                                                  |
|TaleWorlds.CampaignSystem.dll|{=0M6ApEr2}Surely you know that {FIRST_NAME} is {RELATIONSHIP} as well as my liege, and will always be able to count on my loyalty.|
