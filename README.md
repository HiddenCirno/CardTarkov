# CardTarkov

塔科夫棋牌室 — EFT / SPT 客户端小游戏合集。

## 当前进度

**v0.1（纯客户端引擎验证）**

- 斗地主规则引擎：洗牌/发牌、叫地主、出牌/过牌、炸弹/王炸、胜负判定
- 基础 AI：叫地主评分、跟牌提示、留炸/农民不压队友的简单策略
- 记牌数据：按点数统计未出现牌
- 可复现：种子洗牌；不洗牌模式同样支持种子
- 经济接口预留：`IEntryGate` / `ISettlementSink` / `GameResult`，当前为空实现

**尚未实现**

- 抢地主、加倍、炸弹/春天倍数、最大倍数（接口已保留）
- 服务端结算、入场费、邮件发奖
- 客户端 UI（Tab 注入、牌桌、牌面）
- AI 难度分级（接口已保留，当前只有一档可玩 AI）

## 结构

```
CardTarkov.Core/       纯 C# 规则引擎（netstandard2.0）
  ThirdParty/ProkerUtils/  牌型识别/压牌/提示（上游无 license，见 ThirdPartyNotices.md）
  Game/                发牌、状态机、记牌器、结果
  Ai/                  基础 AI
  Economy/             预留的经济接口
CardTarkov.Tests/      规则表单测 + 无头自对弈（net10.0）
```

## 构建与测试

```bash
dotnet build
dotnet run --project CardTarkov.Tests
```

测试程序返回 0 表示全部通过，1 表示有失败。

## 规则口径（v0.1）

- 3 人：玩家 + 两 AI；17/17/17 + 3 张底牌
- 叫地主：随机首叫；第一个叫地主者成为地主，底牌加入手牌；三家都不叫则重新发牌
- 出牌：地主先出；可出任意合法牌型，或不要；两家都不要后由上家重新领出
- 牌型：单/对/三/三带一/三带二/顺子/连对/飞机/飞机带单/带对/四带二/炸弹/王炸（以 `ProkerUtils` 的识别结果为准）
- 胜负：地主先出完 → 地主胜；任一农民先出完 → 农民胜
- 倍数：v0.1 固定 1（炸弹/王炸可出，但暂不翻倍；最大倍数仅保留接口）

## License

AGPL-3.0。第三方代码与出处见 [ThirdPartyNotices.md](ThirdPartyNotices.md)。
