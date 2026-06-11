# MeiChu 100th AR

一款以第 100 屆梅竹賽為背景發想的手機 AR 對戰遊戲。玩家將在現實空間中放置並挑戰清大熊貓 Boss，透過 AR 互動、技能攻擊、閃避與隨機事件，扮演交大陣營取得梅竹賽的最終勝利。

## Demo Video

[![MeiChu 100th AR Demo](https://img.youtube.com/vi/MGoLa6ZaCTo/0.jpg)](https://www.youtube.com/watch?v=MGoLa6ZaCTo&feature=youtu.be)

- [Demo Video](https://www.youtube.com/watch?v=MGoLa6ZaCTo&feature=youtu.be)
- [Presentation](https://www.canva.com/design/DAHMECs4wME/Hf2pp57WinVhjv9ufUU1zg/edit)
- [Proposal](https://docs.google.com/document/d/1fG2cSzUZmHsKM9LGJF5av6QZazml-hH8ptSDQdjM91U/edit?tab=t.0)

## Game Overview

**MeiChu 100th AR** 是一款結合 Unity 與 AR 技術的魂系風格對戰遊戲。遊戲以「梅竹賽」與清交迷因為核心主題，玩家需要在手機 AR 場景中操作角色，面對熊貓 Boss 的多種攻擊，並利用武器、技能與環境機制完成戰鬥。

* **遊戲人數**：1 人
* **遊玩平台**：手機 AR
* **遊戲時長**：約 1–2 分鐘
* **遊戲特色**：AR、Boss Battle、清交迷因、隨機事件、技能對戰

## Features

### AR Gameplay

玩家可以在現實環境中透過手機鏡頭進行遊戲，並將熊貓 Boss 放置在 AR 世界中進行戰鬥。

主要包含：

* AR 平面偵測
* AR 世界中的 Boss 放置
* 手機視角戰鬥
* 現實空間中的閃避與攻擊互動

### Player System

玩家擁有多種攻擊與技能：

#### 百年工程之刃

近戰武器，根據拖曳距離與蓄力程度造成不同傷害。

* 基礎傷害：6
* 拖曳距離可提升傷害
* 蓄力可增加原地攻擊次數
* 高蓄力命中可使目標短暫停頓

#### 天穹百竹之弓

遠距離攻擊武器，玩家可以發射竹子攻擊 Boss。

* 基礎傷害：2
* 蓄力倍率最高可提升至 5 倍
* 速度與衝量會隨蓄力提升
* 高蓄力命中可使目標暫停

#### 狐狸大招

隨機觸發的高傷害技能。

* 60 秒內隨機出現一次
* 觸發後狐狸會自動衝向熊貓
* 對 Boss 造成大量傷害

### Boss System

Boss 是清大熊貓，具有多種攻擊模式與行為。

Boss HP：1000

Boss 攻擊方式包含：

#### 近戰攻擊：Oloo

當玩家距離 Boss 較近時，熊貓會使用近戰攻擊。

* 扇形攻擊範圍
* 近距離壓迫玩家
* 搭配攻擊動畫與判定

#### 遠距離攻擊：梅花攻擊

當玩家距離較遠時，Boss 會射出梅花攻擊。

* 遠距離投射物
* 需要玩家移動閃避
* 增加戰鬥節奏壓力

#### 範圍攻擊：貢丸雨

Boss 會召喚貢丸隕石攻擊玩家所在區域。

* 隨機落點
* 落地前出現警示範圍
* 玩家需要離開危險區域避免受傷

### Random Events

遊戲中會觸發隨機事件，增加戰鬥變化。

#### 題目系統

當熊貓扣除一定血量後，可能觸發題目事件。

* 畫面會被題目 UI 遮擋
* 玩家需要在時間內完成作答
* 題目為小學加減乘除
* 若放棄或超時，玩家會受到懲罰

#### 風系統

遊戲中會隨機產生風，影響玩家攻擊的彈道。

* 隨機風向
* 隨機風力
* 影響竹子子彈加速度
* 低機率出現強力颱風

### UI System

遊戲包含基本戰鬥 UI：

* 玩家血量
* Boss 血量
* 技能冷卻
* 題目事件 UI
* 勝利 / 失敗畫面

## Game Flow

```text
Start
  ↓
AR Plane Detection
  ↓
Place Boss in AR World
  ↓
Fight
  ├── Player wins → Victory
  └── Player loses → Lose Menu
```

## Tech Stack

* Unity
* C#
* AR Foundation
* Niantic AR 
* Git / GitHub

## Project Structure

```text
Meichu_100th/
├── Assets/              # Unity assets, scripts, prefabs, scenes
├── Packages/            # Unity package dependencies
├── ProjectSettings/     # Unity project settings
├── .gitignore
├── .gitattributes
└── README.md
```

## Team Members

| Member | Responsibility                        |
| ------ | ------------------------------------- |
| 曾紹幃 | Boss 系統、風特效、大招特效、整合     |
| 何昊駿 | AR 環境架設、玩家系統、玩法設計、整合 |
| 林彥佑 | 題目系統、風系統、音效、平衡、報告    |

## Development Notes

目前專案已完成 AR Boss 對戰的主要功能，包含玩家攻擊、Boss 攻擊、隨機事件、風系統與實際遊玩流程。

已知可改進項目：

* 貢丸隕石警示可以更加明顯
* AR 平面偵測可能導致 Boss 位置些微偏移
* 部分字體顯示需要修正
* 首頁 Tutorial 可加入 Demo 影片連結
* 遊戲開始前與結束後的風特效需要關閉
