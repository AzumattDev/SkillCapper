Ever wanted to cap...or uncap your skills?! Looking to make the PvE harder by forcing low skills? Looking to help with
AFK skill grinding through macros for PvP?

Introducing, SkillCapper. This mod prevents a player from leveling past a certain skill level while simultaniously
setting the max skill to the level set in the config file. A.k.a "UnCapping" the skill to the new max.

Server config syncs to client on server connect, file change, or file updates.


> ## Installation Instructions
***You must have BepInEx installed correctly! I can not stress this enough.***

#### Local Install

1. Locate your game folder by starting Steam client and :
   a. Right click the Valheim game in your steam library
   b. Go to "Manage" -> "Browse local files"
   c. Steam should open your game folder
2. Extract the contents of the archive into the BepInEx\plugins folder.
3. Launch the game at least once to generate the config file.
4. Locate Azumatt.SkillCapperConfig.yaml under BepInEx\config and configure the mod to your needs

#### Server

`Must be installed on both the client and the server if the mod is on the server. If the client doesn't have the mod, the client will disconnect from the server due to the mod's version checking.`

1. Locate your main folder manually and :
   a. Extract the contents of the archive into the main folder that contains BepInEx
   b. Launch your game at least once to generate the config file needed if you haven't already done so.
   c. Locate Azumatt.SkillCapperConfig.yaml under BepInEx\config on your machine and configure the mod to your needs
2. Reboot your server. All clients will now sync to the server's config file even if theirs differs. Config Manager mod
   changes will only change the client config, not what the server is enforcing.

Feel free to reach out to me on discord if you need manual download assistance.


> Where is the configuration file?

- The Config file's name is "Azumatt.SkillCapperConfig.yaml" it will be located in "BepInEx\config" the file is created
  for you upon launching the game once.

# Author Information

### Azumatt

`DISCORD:` Azumatt#2625

`STEAM:` https://steamcommunity.com/id/azumatt/﻿

For Questions or Comments, find me in the Odin Plus Team Discord:
[![https://i.imgur.com/XXP6HCU.png](https://i.imgur.com/XXP6HCU.png)](https://discord.gg/Pb6bVMnFb2)

Special thanks

Thank you to KG for the help on uncapping skills and teaching me transpilers!
***
> # Update Information (Latest listed first)

| `Version` | `Update Notes`                                                                                                                                                                                                                                                                                                                                                                                                       |
|-----------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 3.0.0     | - Rewrite a lot of the patches in this mod.<br/> - Changed the name of the configuration file to Azumatt.SkillCapperConfig.yaml<br/> - Internal change to the GUID, this will result in the mod thinking it's different from the original. Make sure you remove the original mod manually if you have it installed.<br/> - `PLEASE REGENERATE ALL CONFIGS AND MAKE SURE YOU DO NOT HAVE THE ORIGINAL MOD INSTALLED!` |
| 2.0.0     | - Major overhaul and rewrite of the mod. Move to yaml configuration so you can add your own skills if needed. `DELETE OLD CONFIG FILE!`<br/>Now can un-cap skills based on the max level you have set in the yaml file. If the value is above 100, they can now level to that and the skills will be rebalanced appropriately.                                                                                       |
| 1.0.0     | - Initial Release                                                                                                                                                                                                                                                                                                                                                                                                    |
