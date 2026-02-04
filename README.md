# PlanetWormhole (Updated version)  

Original mod: [PlanetWormhole](https://thunderstore.io/c/dyson-sphere-program/p/essium/PlanetWormhole/) by essium    
Automatically transfer cargos to each machines within a planet. You do not need planetary logistics now!

## Configurations

||Default|Description|
|:---|:---|:---|
|`EnableInterstellar`|false|enable auto interstellar transportation|

## **Technical Implementation**

PlanetWormhole operates by intercepting the game’s logic at the end of each factory frame. Its internal mechanics can be broken down into three main processes:

1.  **Systematic Scanning:** The mod scans all active components on a planet—including assemblers, labs, power generators, silos, and turrets. It identifies which buildings have surplus output and which are awaiting input.
2.  **Programmatic Distribution:** Rather than simulating physical travel, the mod calculates the total supply and demand within a "virtual buffer." It then directly adjusts the internal inventories of the buildings. If an assembler needs iron ingots and a storage tank has them, the mod decrements the tank and increments the assembler simultaneously.
3.  **Virtual Proliferation:** The mod includes an automated Proliferator system. It checks for the presence of **Proliferator MK3** in your global storage. If available, it "consumes" the required amount virtually and applies the production speed or extra product bonuses to the transferred items automatically.
4.  **Performance Optimization:** To ensure game stability, the mod utilizes multi-threading. By offloading these calculations to the `ThreadPool`, it minimizes the impact on the game's main simulation thread (UPS/FPS).

---
# 行星虫洞(更新版)  

原mod: [PlanetWormhole](https://thunderstore.io/c/dyson-sphere-program/p/essium/PlanetWormhole/) 作者: essium    
自动运输分派行星内的货物至各个机器，你不再需要行星内物流系统了!

## 配置
||默认值|描述|
|:---|:---|:---|
|`EnableInterstellar`|false|启动自动星际物流|

## 功能介绍

**「行星级别的物资共享」**
在原版游戏中，如果你想把铁块从 A 工厂运到 B 工厂，你需要拉传送带，或者盖两个物流塔并配备运输机。  
安装这个模组后，**全行星的所有建筑物会共享一个「虚拟仓库」**：
*   **自动供需：** 只要 A 建筑生产了东西，B 建筑如果需要，物资就会「瞬间移动」过去。
*   **取代物流塔：** 你不再需要为了星球内的物资调度而盖满地物流塔。
*   **自动喷涂（增产剂）：** 它会自动消耗你储存的「增产剂 MKIII」，并为所有转运的物资补上增产效果，省去了盖喷涂机的麻烦。
*   **星际运输（可选）：** 它甚至能跨星球自动平衡物流塔里的物资（需在设定中开启）。

## 技术原理

1.  **扫描清点：** 模组会定期扫描你星球上所有的建筑（制造台、熔炉、研究站、发电厂等）。
2.  **登记需求与产出：**  
  它会看哪些建筑「多出了产品」（例如熔炉里的铁块）。   
  它会看哪些建筑「缺少原材料」（例如制造台需要铁块）。  
3.  **瞬间匹配：** 模组在后台直接修改数据，把 A 建筑多出来的数字减掉，加到 B 建筑的原材料栏位里。
4.  **虚拟喷涂：** 模组会检查你的储藏箱里有没有「增产剂 MKIII」。如果有，它会直接扣除增产剂的数量，并给所有「瞬移」的物资加上增产点数。
5.  **多线程优化：** 为了不让游戏卡顿，这些复杂的计算是利用电脑的多核心 CPU 并行处理的（ThreadPool）。


## ChangeLog

### 2.0.2

Fix for game 0.10.34.28392

### 2.0.1

Fix for game 0.10.33.27026  

<details>
<summary>Original Mod Changelog</summary>

### 2.0.0

Add support for turrets.

### 1.0.10

Fix storage connection with sorter and fractionator logic.

### 1.0.9

Fix trash logic.

### 1.0.8

Fix the trash system warning info.

### 1.0.7

Auto interstellar transportation.
Fix multithread bug for trash.
Computation cost for this plugin is added to belt.

### 1.0.6

Support fractionator. Use multiplethread to do the computation.

### 1.0.5

Fix exception when remove a station.

### 1.0.4

Get cargo from trash.
Add internal product buffer to avoid factories with multiple products got stucked.

### 1.0.3

Do not get cargo from requirement slot

### 1.0.2

Fix spray logic

### 1.0.1

Refresh storage

### 1.0.0

Initial version of this plugin

</details>