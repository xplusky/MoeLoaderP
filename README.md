[![Pull Request Welcome](https://img.shields.io/badge/Pull%20request-welcome-brightgreen.svg)](#)

# MoeLoader-P

> 多站点图片浏览、下载工具

-----
## 安装 & 使用

> **http://leaful.com/moeloader-p**

## 关于 MoeLoader-P 

MoeLoader-P 全称为 MoeLoader PlusOneSecond，诞生于 2018-09 ，为 Moeloader 图片浏览和收集工具的衍生分支续命版，对原 MoeLoader 代码进行了大量重构，精简了部分功能，增强了实用功能，美化了界面。 MoeLoader 原作者为 esonic ，项目地址为 https://github.com/esonic/moe-loader-v7 ，作者已销声匿迹失多年，本项目也参考了非官方版 MoeLoader Δ 项目代码，作者为 YIU ，项目地址为 https://github.com/usaginya/MoeLoader-Delta 。


### 支持站点：
  
- **Pixiv.net**
- Bilibili.com
- **Konachan.com**
- Yander.re
- Behoimi.org
- Safebooru.org
- **danbooru.Donmai.us**
- **Gelbooru.com**
- SankakuComplex.com
- Kawainyan.com
- MiniTokyo.net
- E-shuushuu.net
- Zerochan.net 
- WorldCosplay.net
- Yuriimg.com
- Kawaiinyan.com
- lolibooru.moe
- atfbooru.ninja
- and more.

### 预览图
![avatar](http://alicdn.leaful.com/wp-content/uploads/2020/04/SNAG-2020-4-26-8.26.49.jpg)

-----
## MoeLoader-P 特点

### 代码优化
代码完全重构，将原来的自定义站点的项目 MoeSite 、SitePack 整合到原项目中，使用新的语法与类库，降低代码耦合度，更符合语法规范，使用 Task 代替 Thred ， HttpClient 代替 WebClient 等，尽量减少不必要的静态类的使用。booru 等类型站点引擎已重构

### 界面优化
界面重构，使用了更多美化元素，使用了 Storyboard 、Effect 、VisualStateManager 、Fontawesome 、FluentWpf 等组件对软件进行美化，支持 Win10 亚克力效果，图片自动对齐，大小可调整。

### 功能增加
增加了页码导航、标签识别、预览图缩放等功能，对搜索参数、过滤条件（分辨率、格式、评级等）进行了整合优化。优化右键菜单，支持标签大爆炸，搜索作者等功能，显示日期，点击后直接上搜索栏；下载支持多页子项目、动态显示进度。

### 站点增加
增加了B站bilibili画友、摄影站点，改进Pixiv站点，支持搜索作者，支持登录P站，优化排行显示。

### 持续更新
欢迎提供建议，软件持续更新，其他功能陆续添加中，Pull Request welcome。

### 其他问题
e-shuushuu、ani-pics 无法用关键字搜索（能力有限，需要大神）。软件神秘代码： ( hit F8 ten times )

-----

## 更新记录

### 2020-4-24 9.4.6

- 支持b站搜索
- 美化图片显示
- 支持gelbooru 的artist，copyright等字段

### 2020-4-23 9.4.5

- pixiv新增动图排行；
- 支持pixiv动图下载，并自动转换为gif格式；
- 改进文件夹分类以及文件名格式，可以自定义；
- 修复只下载前几张bug；
- 增加copyright artist character 命名方式；
- 修复自定义文件名点击bug；
- 改进点击下载自动滚动条滑到底部；
- 软件架构更新 .net standard 2.0，
- 新增导出下载地址，收集箱功能，改善显示效果，背景效果更新。

### 2020-3-29 9.3.5
- 改进：增加组图数量设置
- 改进：支持p站右键搜索作者
- 改进：支持p站排行右键选择新作
- 改进：支持下载状态
- 改进：支持软件背景图片
- 改进：支持下一页清除图片
- 改进：支持多个站点右键显示日期
- 改进：增加IE代理；
- 修复：部分下载问题，删除全部任务问题

### 2020-3-18 9.3.2

- 图片对齐模式，自动调整边距，分散对齐，右边不留空；
- 新增网站登录功能，支持Pixiv，需登录后搜索；
- 改进显示模式，去除分页，全部显示在一页中(可支持选项控制切换分页)
- 新增按照关键字建立文件夹（K ）
- 新增右键显示日期（索尼）
- 右键标签可右键复制（azsxjkl123）
- 新增反馈按钮（K ）
- 新增p站排行显示模式（守望者）
- 修复Pixiv、sankakuchan、idol、yuriimg搜索功能；（波旬、家骏の世界、 yfzxyey、S-S、Sion小懒貓、aming854、lovecino、陈二- 狗、周世玲 、绯红）
- 修复b站部分图片加载不出的问题；
- 修复minitokyo 搜索重复问题（我是fsd、Ichinose）
- 修复下载部分问题
- 修复自定义文件名遇到多图的问题
- e-shuushuu、ani-pics 无法用关键字搜索（能力有限，需要大神）；
- 20200316 经测试，所有站点均可联通获取，若无法获取请自行挂梯子；下一步计划增加其他网站登录功能（如worldcos），根据反馈继续修复功能

### 2019-4-6 9.1.0
- 更新背景Fluent磨砂设计
- 新增自定义名称功能（zzh8362反馈）
- 下载重复的会话跳过而不是重新命名下载（秋山若牧反馈）
- 键盘右键下一页（武林是咸鱼 反馈）
- 增加重新开始功能（苍瞳猫反馈）

### 2018-10-13 9.0.8
- 修正搜索途中取消问题
- 修正gelbooru 详情页
- 修正donmai下载原图问题（鸦酱 反馈）
- 添加Ctrl+A全选功能
- 添加下载图片类型选择（Coolkid_Conan0 反馈）
- 新增图片选择数量

### 2018-10-03 9.0.6
- 修正pixiv.net,可以搜索作者ID和作品Tag
- 修正safebooru(感谢Parco94反馈)

### 2018-10-01 9.0.5
- 修正pixiv、sankaku站点
- 修正选择框bug

### 2018-09-29 9.0.4
- 新增图片框选功能，（superxxx8反馈）

### 2018-09-28 9.0.3
- 修正几个网站显示及下载功能

### 2018-09-24 9.0.2
- 修复部分BUG
- 修复代理设置
- （已知bug：文件名自定义还没有实现）

### 2018-09-22 9.0.1
- 第一测试版
