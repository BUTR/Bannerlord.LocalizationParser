# Bannerlord.LocalizationParser

## Installation
1. Install the latest [.NET 5.0 Runtime](https://dotnet.microsoft.com/download/dotnet/5.0/runtime).
2. Run `dotnet tool install --global Bannerlord.LocalizationParser` to install the tool.

## Usage
To run, use `Bannerlord.LocalizationParser --game-folder "H:\\SteamLibrary\\steamapps\\common\\Mount & Blade II Bannerlord" --output LocalizationStrings.csv`

## Output
It will output a .csv file with the following format:
|Assembly                     |Text                                                                                                                               |
|-----------------------------|-----------------------------------------------------------------------------------------------------------------------------------|
|TaleWorlds.CampaignSystem.dll|{=TauRjAud}{NAME} of the {FACTION}                                                                                                 |
|TaleWorlds.CampaignSystem.dll|{=vvCwVo7i}{DAMAGE} {DAMAGE_TYPE}                                                                                                  |
|TaleWorlds.CampaignSystem.dll|{=0M6ApEr2}Surely you know that {FIRST_NAME} is {RELATIONSHIP} as well as my liege, and will always be able to count on my loyalty.|
