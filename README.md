# LagGridBroadcaster
A Torch plugin that can broadcast most lag grid to all players.

# Dependency
This plugin is base on [Profiler](https://torchapi.net/plugins/item/da82de0f-9d2f-4571-af1c-88c7921bc063), please make sure you have that plugin installed.

# Command
`laggrids send [ticks]` Send the top X most lag grids to all players

`laggrids list` List latest measure results

`laggrids get` Get latest result of the grid you're currently controlling

`laggrids cleangps` Cleans GPS markers created by LagGridBroadcaster

# Configuration
`Top` Broadcast top X grids

`MinUs` Only broadcast grids which take time greater than(us)

`FactionMemberDistance` Only broadcast grids when faction member is in radius of(m)(zero for infinity)

`FactionTop` Send faction top X grids to faction members(zero for disable)

`PlayerMinUs` Broadcast most lag grid player owned, if all of his grids take more than(us)(zero for disable)

`WriteToFile` Write measure result to file

`ResultFileName` Result file name

# License
Apache License 2.0
