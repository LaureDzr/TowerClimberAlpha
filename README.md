# This is an uncompleted game project, purpose for learn Database programming in game development

# Game Design document:
# Database Programming\_Tower Climber Alpha

# 1\. Overview

Tower Climber Alpha is a **grid-based roguelike tower climbing game** inspired by the classic Magic Tower.

The project is developed using:

* Unity 6
* SQLite Database

The goal of this project is to demonstrate a **database-driven game architecture**, where most gameplay data is stored and managed inside a database.

# 2\. Gameplay

Players climb floors of a tower by:

* exploring maps
* fighting monsters
* collecting keys
* opening doors
* entering treasure rooms
* descending to the next floor

Each floor is procedurally generated.

# 3\. Map System

Map size:
15 × 15 grid (changable by inspector)

Tile types:

-- Floor
-- Wall
-- Door
-- Key
-- Monster
-- Merchant
-- Treasure
-- StairsUp
-- StairsDown

# 4\. Treasure Room

Each floor contains a treasure room.

Features:

-- surrounded by walls
-- single entrance
-- contains treasure
-- contains next floor portal



# 5\. Floor Generation

Floor generation process:

1. Create base floor tiles
2. Generate boundary walls
3. Create treasure room
4. Place treasure item
5. Place stair portal
6. Spawn monsters
7. Spawn merchant
8. Spawn keys

# 6\. Monster System

Monster types:

-- Normal
-- Elite
-- Boss
-- Merchant

Monster attributes:

-- HP
-- ATK
-- DEF
-- Gold reward

All values are configurable in the Unity Inspector.

# 7\. Combat System

Combat rule:



player ATK > monster DEF



Damage formula:



playerDamage = playerATK - monsterDEF
monsterDamage = monsterATK - playerDEF



Each click triggers one combat round.



# 8\. Merchant System

Players can purchase upgrades.

|Trade|Result|
|-|-|
|Gold → HP|heal|
|Gold → ATK|attack increase|
|Gold → DEF|defense increase|

# 

9. UI System

Layout:
Top Left => Player Stats
Bottom Left => Monster Info
Right Side => Combat Prediction
Top Right => Current Floor



# 10\. Save System

The game supports **3 save slots**.

Saved data includes:

-- player stats
-- player position
-- floor number
-- tile states
-- monster states

All data is stored in SQLite.



# 11\. Database Tables

### save\_profile

Stores player data and position.

### floor\_tiles\_v2

Stores tile states.

### monster\_def

Stores monster template definitions.

# 12\. Controls

|Input|Action|
|-|-|
|W A S D|movement|
|Mouse Left|attack / interact|
|ESC|pause menu|
|F11|quick save|

# 13\. Technical Architecture

Main game systems:

-- PlayerController
-- GridManager
-- FloorGenerator
-- SqliteDb
-- BattleController
-- MerchantUI
-- HUDController

# 14\. Future Work

Possible improvements:
-- equipment system
-- skills
-- events
-- NPC dialogue
-- dungeon variations
-- map editor

