# SplitHands 3D — Procedural Voxel Sandbox Prototype

## Overview

SplitHands 3D is a Unity-based procedural voxel sandbox prototype inspired by survival and block-building games.

The project demonstrates chunk-based terrain generation, interaction systems, inventory mechanics, crafting logic, enemy AI behaviour, and world persistence systems.

This prototype was built as a technical portfolio project to showcase gameplay systems architecture and modular Unity scripting skills.

---

## Features

- Procedural chunk-based voxel world generation
- Grass / Dirt / Stone terrain layering
- Underground iron ore spawning
- Tree and rock structure generation
- Inventory and hotbar system
- Block interaction and breaking system
- Loot chest interaction with progress timer
- Furnace auto-smelting system
- Enemy skeleton AI behaviour
- Day/Night cycle with night spawning logic
- Chat and command console system
- Save / Load world system
- Tool-based interaction logic

---

## Controls

| Key | Action |
|-----|--------|
| WASD | Movement |
| Mouse | Camera look |
| LMB | Break / interact / place block |
| F | Pick up item |
| G | Drop item |
| Enter | Open chat |
| / | Open command console |

---

## Systems Architecture

Core gameplay systems are separated into modular scripts:

**WorldGenerator.cs**  
Handles procedural terrain generation and biome structure logic

**Chunk.cs**  
Stores voxel data and rebuilds chunk meshes dynamically

**HandGamePlayer.cs**  
Controls player interaction with world objects and blocks

**HotbarManager.cs**  
Handles item selection and usage from hotbar

**InventoryCraftUI.cs**  
Crafting interface and recipe processing

**LootChest.cs**  
Timed chest opening with reward spawning

**Furnace.cs**  
Automatic resource smelting logic

**SkeletonAI.cs**  
Enemy behaviour and player tracking

**WorldSaveSystem.cs**  
World persistence and player save/load system

---

## My Responsibilities

Designed and implemented:

- chunk-based voxel terrain generation system
- inventory and hotbar interaction pipeline
- procedural underground resource spawning
- block breaking logic
- loot chest interaction mechanics
- furnace smelting automation
- enemy skeleton behaviour logic
- save/load world persistence system
- command/chat interaction system

---

## Technologies Used

- Unity (C#)
- Procedural mesh generation
- Chunk-based voxel storage
- Script-driven modular gameplay architecture

---

## Project Purpose

This project was created as a technical gameplay systems prototype for portfolio demonstration.

Focus areas:

- procedural world generation
- modular system architecture
- interaction mechanics
- survival sandbox gameplay logic implementation

---

# SplitHands 3D — Процедурный voxel sandbox прототип

## О проекте

SplitHands 3D — это прототип sandbox-игры на Unity с процедурной генерацией voxel-мира.

Проект демонстрирует генерацию чанков, систему взаимодействия игрока с миром, инвентарь, крафт, AI противников и систему сохранения мира.

Этот проект создан как портфолио-прототип для демонстрации навыков разработки игровых систем на Unity.

---

## Возможности проекта

- Процедурная генерация voxel-мира чанками
- Слои terrain: трава / земля / камень
- Генерация железной руды под землёй
- Генерация деревьев и камней
- Система инвентаря и хотбара
- Система разрушения блоков
- Сундуки с таймером открытия
- Автоматическая переплавка ресурсов в печи
- AI скелетов-противников
- Система смены дня и ночи
- Чат и система команд
- Система сохранения и загрузки мира
- Взаимодействие через инструменты

---

## Архитектура систем

Основные игровые системы разделены на отдельные скрипты:

**WorldGenerator.cs** — генерация мира  
**Chunk.cs** — хранение voxel-данных чанка  
**HandGamePlayer.cs** — взаимодействие игрока  
**HotbarManager.cs** — управление хотбаром  
**InventoryCraftUI.cs** — система крафта  
**LootChest.cs** — логика сундуков  
**Furnace.cs** — логика печи  
**SkeletonAI.cs** — поведение противников  
**WorldSaveSystem.cs** — сохранение мира

---

## Что реализовано лично

В рамках проекта реализованы:

- система генерации мира чанками
- система взаимодействия игрока с блоками
- инвентарь и хотбар
- генерация ресурсов под землёй
- система сундуков
- система печи
- AI противников
- сохранение и загрузка мира
- чат и команды игрока

---

## Назначение проекта

Проект создан как технический sandbox-прототип для демонстрации архитектуры игровых систем и навыков разработки на Unity.