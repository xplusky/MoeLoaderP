[![Pull Request Welcome](https://img.shields.io/badge/Pull%20request-welcome-brightgreen.svg)](#)

# MoeLoader-P

> 多站点图片浏览、下载工具

-----
## 安装 & 使用 & 反馈

> **http://leaful.com/moeloader-p**

## 注意事项

本软件支持的系统为Windows 7以上64位系统。
需要系统安装.Net 5版本的框架。
> **https://dotnet.microsoft.com/download/dotnet/5.0**

## 关于 MoeLoader-P 

MoeLoader-P 全称为 MoeLoader PlusOneSecond，诞生于 2018-09 ，为 Moeloader 图片浏览和收集工具的衍生分支续命版，对原 MoeLoader 代码进行了大量重构，精简了部分功能，增强了实用功能，美化了界面。 MoeLoader 原作者为 esonic ，项目地址为 https://github.com/esonic/moe-loader-v7 ，作者已销声匿迹失多年，本项目也参考了非官方版 MoeLoader Δ 项目代码，作者为 YIU ，项目地址为 https://github.com/usaginya/MoeLoader-Delta 。

### 站点支持情况：
  
- **Pixiv.net**  支持最新图片、Tag搜索、作者Id搜索、排行搜索（支持日期）、支持下载动图转为GIF格式、支持登录
- Bilibili.com 支持画友最新、最热搜索
- **Konachan.com** 支持关键字搜索、图片Tag、日期、作者、图源显示
- Yander.re 支持关键字搜索、图片Tag、日期、作者、图源显示
- Behoimi.org 支持关键字搜索、图片Tag、日期、作者、图源显示
- Safebooru.org 支持关键字搜索、图片Tag、日期、作者、图源显示
- **danbooru.Donmai.us** 支持关键字搜索、图片Tag、日期、作者、图源显示
- **Gelbooru.com** 支持关键字搜索、图片Tag、日期、作者、图源显示
- SankakuComplex.com 支持关键字搜索、图片Tag、日期、作者、图源显示、支持Chan及Idol，支持Chan登录
- Kawainyan.com 支持搜索、最新图片
- MiniTokyo.net 支持分类搜索、最新图片
- E-shuushuu.net 支持最新图片，目前无法用关键字搜索
- Zerochan.net 支持搜索、最新图片
- WorldCosplay.net 支持搜索、最新图片
- Yuriimg.com 支持搜索、最新图片
- Kawaiinyan.com 支持搜索、最新图片
- lolibooru.moe 支持关键字搜索、图片Tag、日期、作者、图源显示
- atfbooru.ninja 支持关键字搜索、图片Tag、日期、作者、图源显示
- 自定义站点....（支持通过Json文件创建自定义站点。默认自带4~5个）

### 预览图
![avatar](http://alicdn.leaful.com/wp-content/uploads/2020/04/SNAG-2020-4-26-8.26.49.jpg)

-----
## MoeLoader-P 特点

### 代码优化
代码完全重构，将原来的自定义站点的项目 MoeSite 、SitePack 整合到原项目中，使用新的语法与类库，降低代码耦合度，更符合语法规范，使用 Task 代替 Thred ， HttpClient 代替 WebClient 等，尽量减少不必要的静态类、接口的使用。booru 等类型站点引擎已重构

### 界面优化
界面重构，使用了更多美化元素，使用了 Storyboard 、Effect 、VisualStateManager 、Fontawesome 、FluentWpf 等组件对软件进行美化，支持 Win10、Win11 亚克力效果，图片自动对齐，大小可调整。

### 功能增加
增加了页码导航、标签识别、预览图缩放等功能，对搜索参数、过滤条件（分辨率、格式、评级等）进行了整合优化。优化右键菜单，支持标签大爆炸，搜索作者等功能，显示日期，点击后直接上搜索栏；下载支持多页子项目、动态显示进度。

### 站点增加及改进
增加了B站bilibili画友、摄影站点，改进Pixiv站点，支持搜索作者，支持登录P站，优化排行显示。

### 支持自定义站点
10.0版本新增自定义站点支持（右键点击LOGO进入自定义模式），只需要编辑Json文件，加上亿点点耐心，就可以拥有自己的图片站点。如果嫌麻烦，可以通过某种渠道获得现成Json。
> **https://github.com/xplusky/MoeLoaderP-CustomSite**

### 持续更新
欢迎提供建议，软件持续更新，其他功能陆续添加中，Pull Request welcome。

### 其他问题
软件神秘代码： ( hit F8 ten times )

### 交流群
QQ群：173707488 验证：MoeLoaderP

-----

## 更新记录

### 待定 10.0.1-(正在施工中，敬请期待)
- 更新至.Net5框架（可能需要下载.net5 runtime才能运行软件，以后考虑升级到.Net6后通过MAUI来实现手机端）
- 登录功能切换至WebView2（首次使用需下载），不再使用CefSharp
- 新增自定义站点功能（右键LOGO进入），通过JSON文件就可以自定义自己站点（目前支持Html模式），详细教程等有空再弄（默认有数个自定义站点可用、可用作参考）
- 完善登录功能，支持Pixiv、SankakuComplex登录搜索
- 新增站点全名、作者ID等名称命名格式(via 咸鱼)
- 设置完善、支持亚克力效果开关选项。
- 更新FluentWPF，支持暗黑模式（根据系统颜色设置）
- 优化打开文件位置功能，可以默认选中图片了
- 优化下载列表，可以平滑滚动
- pixiv增加镜像站点功能，可以免梯子搜索下载（在参数里面）
- 搜索参数中的数量、镜像站点设置可以自动保存，下次搜索自动读取
- 可以自定义背景图片了，通过设置可以快速换背景图片
- 修复sankaku chan 登录，可以登录后搜索(via Mr3)
- sankaku chan 支持在线收藏按钮了（会同步到在线账户中）
- sankaku chan 支持搜索搜藏图片了（via 周子）
- 支持收藏数量显示，已收藏显示为红色（目前支持sankaku chan）
- 修复zerochan，可以继续搜索图片了
- 可以框选右键批量刷新图片了(via xx.p)
- sankaku chan 支持多关键字搜索了，只要在录入关键字后打空格就可以添加进关键字中
- 历史记录改为分别不同站点存储

### 2021-4-12 9.5.1
- 修复文件名规则框
- 修复pixiv
- 修复konachan
- 修复Minitokyo

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
