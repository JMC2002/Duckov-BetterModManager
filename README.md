**🌐[ 中文 | [English](README_en.md) ]**

[📝更新日志](CHANGELOG.md)

[📦 Releases](https://github.com/JMC2002/Duckov-Sort/releases)

# 更好的MOD菜单
##  0. 安装
Steam版本直接在创意工坊订阅即可，👉 [创意工坊页面](https://steamcommunity.com/sharedfiles/filedetails/?id=3600174953)

其他版本可以自行编译，或者在[📦 Releases](https://github.com/JMC2002/Duckov-BetterModManager/releases)界面下载BetterModManager.zip后解压到游戏安装目录下的Mods
目录下（没有就新建一个），** 需要安装前置Mod [JmcModLib](https://github.com/JMC2002/JmcModLib) **，文件结构如下：

```sh
-- Escape from Duckov
    |-- Duckov.exe
    |-- Duckov_Data
         |-- Mods
              |-- JmcModLib
              |-- BetterModManager
                   |-- BetterModManager.dll
                   |-- info.ini
                   |-- preview.png
```
> ⚠️ release界面会随着创意工坊更新，最新版本以源代码为准，项目文件夹里的那个DLL也是最新的，但开发阶段不一定能用

## 🧠 1. 简介
是否厌倦了为MOD排序或者启用/禁用MOD时总是点一年？用这个MOD一键置顶/置底/启用/禁用！首次启用需要重启游戏，同时支持拖拽排序。

[演示视频（B站）](https://www.bilibili.com/video/BV18g2TBQErE/)

[Github仓库](https://github.com/JMC2002/Duckov-BetterModManager)
## ⚙️ 2. 功能
- 在MOD管理页面为每个条目增添了两个按钮，分别代表将MOD扔到最上面/扔到最下面，理论上等价于按n次上移/下移
- 在MOD条目的左侧添加一个选择框，勾选代表全部启用MOD，取消勾选代表全部禁用MOD
- 鼠标按住MOD条目可以拖动该条目排序
- 鼠标点击MOD条目后选中该条目，然后按**W**或者**方向上**上移MOD，按**S**或**方向下**下移MOD，按**Enter**或**ESC**或鼠标点击**任意处**取消选中
- 右键拖动界面可滚动界面（行为与无MOD时左键拖动相同）
 


## 🔔 3. 提醒
- 本 MOD 需要前置：[JmcModLib](https://github.com/JMC2002/JmcModLib)
  需要安装此MOD才能正常使用，但是不依赖彼此加载顺序，仅需将这两个MOD都启用即可；若未启用或未订阅前置，会弹窗提醒，弹窗参考[前置依赖无序化模板](https://steamcommunity.com/sharedfiles/filedetails/?id=3624342813)
- 本MOD可安全卸载，理论上完全不会影响存档
- 首次启动MOD需要重启游戏

- 全部启用/禁用的原理是从第一条MOD开始从上至下依次启用，遇到MOD条目过多的情况卡一下是正常现象
- 如果你想全部禁用但发现选择框未勾选，请先勾选
 
## 🧩 4. 兼容性
- 本MOD修改了MOD菜单的UI，可能与其他修改该处UI的MOD冲突
- 本MOD使用Harmony框架，其他与Harmony冲突的MOD可能会与本MOD冲突
- 本MOD大面积使用反射，可能会随着游戏版本更新而失效

## 🧭 5. TODO
- 增添一键全选/全部取消的功能  ✔️

**如果你喜欢这个 Mod 的话，希望可以点一个star~**