# LagGridBroadcaster
A [Torch](https://torchapi.com/) plugin that can broadcast most lag grid to all players(add gps to player).

Torch link: https://torchapi.com/plugins/view/?guid=dd316db4-5d89-4db2-aa47-dac2a1a0ea64

Github link: https://github.com/czp3009/LagGridBroadcaster

# Dependency
This plugin is base on [Profiler](https://torchapi.com/plugins/view/?guid=da82de0f-9d2f-4571-af1c-88c7921bc063), please make sure you have that plugin installed.

# Command
`!laggrids help` Show help message

`!laggrids send [seconds]` Send the top X most lag grids to all players(argument `seconds` for setting measure time, default is 15)

`!laggrids list` List latest measure results

`!laggrids get` Get latest result of the grid you're currently controlling

`!laggrids cleangps` Cleans GPS markers created by LagGridBroadcaster

# Note
When command `!laggrids send` executed, things below will happen:

* Global top x grids will be broadcast(add gps to players), configured by `Top`, `MinUs` and `FactionMemberDistance`

* Faction top x grids will be send to faction members via chat message, configured by `FactionTop`

* Send the result of the grid currently in control to player, configured by `SendResultOfControllingGrid`

* Write measure result to file, configured by `WriteToFile` and `ResultFileName`

# License
Apache License 2.0
