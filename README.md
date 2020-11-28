# LagGridBroadcaster
A [Torch](https://torchapi.net/) plugin that can broadcast most lag grid to all players.

# Dependency
This plugin is base on [Profiler](https://torchapi.net/plugins/item/da82de0f-9d2f-4571-af1c-88c7921bc063), please make sure you have that plugin installed.

# Command
`laggrids help` Show help message

`laggrids send [ticks]` Send the top X most lag grids to all players

`laggrids list` List latest measure results

`laggrids get` Get latest result of the grid you're currently controlling

`laggrids cleangps` Cleans GPS markers created by LagGridBroadcaster

# Note
When command `laggrids send` executed, things below will happen:

* Global top x grids will be broadcast(add gps to players), configured by `Top`, `MinUs` and `FactionMemberDistance`

* Faction top x grids will be send to faction members via chat message, configured by `FactionTop`

* Send the result of the grid currently in control to player, configured by `SendResultOfControllingGrid`

* Write measure result to file, configured by `WriteToFile` and `ResultFileName`

# License
Apache License 2.0
