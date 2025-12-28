# MetaMystia - ResourceEx Module

ResourceEx 支持创作者们为游戏添加自定义资源，目前实现了自定义角色立绘，以及会使用到他们的对话包

## 演示

（发视频后会贴在这里）

## 使用方法

### 目录结构

在 `游戏根目录/ResourceEx/YourResName/` 下创如下目录结构：

```
./ResourceEx/
├── YourResName/
│   ├── assets/
│   │   ├── YourCharacter_0.png
│   │   ├── YourCharacter_1.png
│   │   ├── AnotherCharacter_0.png
│   │   ├── AnotherCharacter_1.png
│   │   └── ...
│   └── ResourceEx.json
├── YourResName2/
│   ├── assets/
│   │   ├── OneMoreCharacter_0.png
│   │   ├── OneMoreCharacter_1.png
│   │   └── ...
│   └── ResourceEx.json
└── ...

```


例如:

```
./ResourceEx/
└── MetaMystia/
    ├── assets/
    │   ├── AkiMinoriko_0.png
    │   ├── AkiSizuha_0.png
    │   └── YakumoRan_0.png
    └── ResourceEx.json
```

### ResourceEx.json 配置文件

可以参考项目根目录下的 ./ResourceEx/MetaMystia/ResourceEx.json 文件，编写 ResourceEx.json 配置文件，目前一共有两个字段，`characters` 和 `dialogPackages`

```json
{
  "characters": [
    {
      "id": 10000,
      "name": "秋静叶",
      "label": "AkiSizuha",
      "descriptions": [
          "红叶之神",
          "八百万之神",
          "掌管红叶程度的能力"
    ],
    "type": "Special",
      "portraits": [
        { "pid": 0, "path": "assets/AkiSizuha_0.png" }
      ]
    },
    {
      "id": 10001,
      "name": "秋穰子",
      "label": "AkiMinoriko",
      "descriptions": [
        "丰收之神",
        "八百万之神",
        "掌管丰收程度的能力"
      ],
      "type": "Special",
      "portraits": [
        { "pid": 0, "path": "assets/AkiMinoriko_0.png" }
      ]
    },
    {
      "id": 10002,
      "name": "八云蓝",
      "label": "YakumoRan",
      "descriptions": [
        "使役式神程度的能力",
        "",
        ""
      ],
      "type": "Special",
      "portraits": [
        { "pid": 0, "path": "assets/YakumoRan_0.png" }
      ]
    }
  ],
  "dialogPackages": [
    {
      "name": "Test",
      "dialogList": [
        {
          "characterId": 10000,
          "characterType": "Special",
          "pid": 0,
          "position": "Left",
          "text": "马上就要到秋天了呢。树叶啊，总让人觉得到了秋天才会变成红色，但其实从发芽的时候，它们就开始带上了美丽而虚幻的鲜红哦。只不过，叶子们在整个夏天都会把它隐藏起来……像这样害羞的落叶正在努力舞蹈着。"
        },
        {
          "characterId": 10001,
          "characterType": "Special",
          "pid": 0,
          "position": "Right",
          "text": "秋穰子 Testing..."
        },
        {
          "characterId": 10002,
          "characterType": "Special",
          "pid": 0,
          "position": "Left",
          "text": "八云蓝 Testing..."
        }
      ]
    },
  ]
}

```

**使用 [ResourceEx 小助手](https://meta-mystia-editor.izakaya.cc/) 来自动生成 `ResourceEx.json` 文件！**

**特别感谢 [东方也雀食堂小助手](https://izakaya.cc/) 的支持！**

注意，
1. `Character` 中 0~999 段为游戏原有资源，1000~5999 段为 DLC 资源，6000~9999 段为本项目 MetaMystia 为游戏保留的扩展资源 ID 段，10000 及以上为创作者自定义资源 ID 段，请避免使用 0~9999 的 ID 段，并遵循游戏内规范，每 1000 为一个创作者自由使用的 ID 段，尽量不要与其他创作者冲突。（p.s. 10000~10999 段就留给我吧 >_<）
2. `portraits` 中的 `pid` 字段对应角色的不同表情立绘，需要保证图片文件路径存在且对应正确。
3. `Character` 中 `type` 理论可选 `Self` `Special` `Normal` `Unknown`，但目前仅支持 `Special` 类型角色的立绘扩展。
4. `Character` 中 `descriptions` 为角色的描述信息，目前游戏内并无使用场景，可留空字符串占位，建议保持三条描述信息。
5. `dialogPackages` 中的 `characterId`, `characterType` 以及 `pid` 需保证能够在 **全局** ResourceEx 中或 **游戏原有资源** 中找到对应的角色配置，否则可能导致游戏挂起。（如不确定游戏原有资源，以及各 `pid` 对应的表情立绘，可以查看 Release 给出的参考截图）
6. `dialogPackages` 中的 `position` 可选 `Left` `Right`，对应立绘显示位置。
7. `dialogPackages` 中的 `text` 为对话文本内容，理论支持 Unity [富文本](https://docs.unity.cn/cn/2021.3/Manual/StyledText.html)。

<s> p.s. `AkiMinoriko_0.png` 的 `AM` 水印是因为恰好盖在手部，而我又没学过画画，所以 p 不掉</s>


## 对话展示

由于只是载入了对话数据，并支持注入对话包，目前并没有实现对话触发逻辑，对于重新自定义构建代码的创作者来说，可能唯一的方法是通过 `WebDebugger` 的简易 `Console` 中手动触发对话包:

```c#
MetaMystia.Dialog.ShowResourceExPackage("YourDialogPackageName", null)
```

## 关于版权

本仓库及其开发者不拥有 `/ResourceEx/MetaMystia/` 目录下的全部资源的版权，这些资源归 [东方夜雀食堂](https://store.steampowered.com/app/1584090/) 及 [东方秋神牧场](https://www.bilibili.com/video/av115196056114538) 的创作团队 [**二色幽紫蝶**](https://space.bilibili.com/86865890) 所有。部分文案来自 [东方夜雀食堂动画版EX短篇预览，顺便开预售啦~](https://www.bilibili.com/video/av115295746265179)

如您希望成为新的资源包创作者，请确保您拥有所使用资源的合法版权或已获得原作者的二次创作授权，不侵害 **二色幽紫蝶** 及其他任何第三方创作者的合法权益，并严格遵循 [东方Project使用规定](https://www.bilibili.com/opus/400555526272745308)