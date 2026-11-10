# 虛擬遊戲課：資工人出社會模擬

課程：數位孿生與 VR 虛擬場景製作（大三上）。登記在 D:\專案\PROJECTS.md（id: virtual-game）。

## 交期
- 下週（約 11/10）：人物模型——第 7 週（生成＋AI 綁骨，交預覽動作錄影＋FBX）、第 9 週（W 走 A/D 轉，交錄影＋Animator 截圖）
- 2027 年 1 月：完整遊戲
- 只用免費工具；Meshy、Tripo 免費版都不能下載模型，不要再走那兩條

## 角色流水線（已完成，可重跑）
1. `IMG_4683.jpg` → Gemini 改畫成 T-pose 卡通 → `character_tpose.jpg`
2. Hugging Face 上的 TRELLIS（免費、免登入）→ `character/character_trellis.glb`（4,708 面）
3. Blender 4.5（免安裝版在 C:\Users\s1070\tools）：
   - `character/prep_for_mixamo.py`：焊接接縫、1.8m、腳踩地
   - `character/auto_rig.py`：Mixamo 命名骨架＋自動權重 → `character/Player.fbx`
   - Mixamo 線上綁骨一直回「Unknown error while generating motion」，所以改在本機綁
4. Unity：`VirtualGame/`（6.3，URP，Input System）
   - `-executeMethod BatchSetup.SetupCharacter`：設 Humanoid、建 `Assets/Course/Scenes/Week9_Walk.unity`、掛狀態機與 SimpleWalker
   - 預覽：`character/walk_preview.png`

## 還要人工做的
- 第 7 週交付：錄一段角色動起來的影片（可用 Week9_Walk 場景錄）
- 第 9 週交付：開 Week9_Walk 按 ▶，錄 W 走／放開停／A/D 轉，再截 Animator 視窗
- 上課抽問要能逐行解釋 SimpleWalker、ClickSelector

## 遊戲設計：《工程師的一天》
目標：讓非資工系的人「感同身受」資工系畢業後的上班日常，寫實為主。

**玩法**：玩家操作自己的角色，在辦公室過完一天（09:00–18:00 壓縮成約 10 分鐘）。任務會一個一個跳出來，每個選擇影響四個數值：**進度、壓力、主管信任、生活**。

**一天的事件（都取材自真實工作）**
| 時間 | 事件 | 玩家做什麼 |
|---|---|---|
| 09:30 | Daily Standup | 走到會議室，選「老實說卡住了」或「快好了」（說謊會在下午爆炸） |
| 10:00 | 修 Bug 單 | 點自己的電腦 → 小遊戲：從 5 行程式碼挑出錯的那行 |
| 11:00 | Slack 訊息狂跳 | 一邊修 Bug 一邊被打斷，決定回不回 |
| 13:30 | PM 改需求 | 早上做的要重做，選接受、協商或硬推 |
| 15:00 | Code Review | 資深同事留言，選虛心改或爭論 |
| 17:30 | 上線前出事 | 選準時下班（明天被罵）或加班修 |
| 結算 | 一天總結 | 依四個數值給結局：準時下班／加班到 22:00／爆肝；附一句真實數據或心聲 |

**直接沿用課程裡已經寫好的東西**
- 走路：SimpleWalker＋HeroAnimator（第 9 週）
- 點電腦、咖啡機、PM 的座位：ClickSelector＋InfoTag（第 8 週）
- 撞牆、第一人稱：第 10 週的 Character Controller
- 分數、勝利文字：Roll a Ball 的計分 UI

**要新寫的（盡量小）**
- `DayClock`：遊戲時鐘，到時間就觸發事件
- `EventDialog`：事件對話框（題目＋2～3 個選項，選完改數值）
- `Stats`：四個數值＋結局判定
- 事件內容放一個 JSON，改劇情不用改程式

**場景**：辦公室（工位、會議室、茶水間）。先用 Unity 方塊拼出格局，再換成 Asset Store 免費辦公室素材或自己掃描的物件。

## 里程碑
- ✅ 11/3 第一版可玩：`Assets/Course/Scenes/Office.unity`（選單 Course/4. 可重建）
  - 程式：`Scripts/Game/DayManager.cs`（時鐘＋四數值＋事件＋結局）、`EventDialog.cs`、`PlaceZone.cs`
  - 劇情：`Resources/events.json`（9 個事件、4 種結局；說謊會在 16:00 被抓包）
  - 尚未在 Play 模式實測，只驗過編譯和畫面
- ✅ 11/3 改版（依使用者回饋＋參考作品「教師上班日常」系列）：
  - 題目改成不需要程式知識的日常抉擇，專有名詞都用白話解釋；選完會顯示數值變化
  - 開場說明卡、結尾「你不是一個人，辛苦了」＋安心專線 1925
  - 畫面：暖色光、後製（Bloom／暗角／ACES）、桌椅螢幕盆栽白板、出勤卡 HUD（四條數值條）、便條紙任務、地上黃色光圈指路、紙張對話框
- ✅ 11/9 進階版必做 5 項（設計：https://claude.ai/artifact/MqmcnQpX9YQobG8KzgXji7 ）
  1. 鏡頭：Cinemachine 3.1.7，`Follow Cam` 背後斜上方、`Face Cam` 對話時臉部特寫（`CameraDirector.cs`）；對話框移到右邊
  2. 技術債：`DayManager.techDebt`，玩家看不到，貪快選項會加，結局才公開
  3. 半動態事件：`events.json` 分 `main`（09:00 站會、12:00 午餐、16:00 說謊被抓包、17:30 上線出事）、`slots`（7 個抽籤時間）、`pool`（10 件，依數值加權；技術債高 → bug 事件多）
  4. 打程式碼：座位按 E，按任何鍵打出 3～5 字（`TypingTerminal.cs`，內容是遊戲自己的程式）；專案只開新版 Input System，所以用 `Keyboard.current.anyKey` 代替 `Input.anyKeyDown`
  5. 因果時間軸：每個選擇都記錄，後果事件依 `causedBy` 連紅線（`TimelineView.cs`），按「看結局」接 4 種結局
- ✅ 11/10 畫質升級（和 Gemini 討論，見 GRAPHICS-PLAN.md）：SSAO、木地板、材質分層、天花板＋日光燈、特寫景深＋輪廓光、正黑體＋程式碼上色、角色臉平滑
- ✅ 11/10 加分項（參考老師的 JohnLemon 簡報）：
  - 同事 3 人（Mixamo：Lewis／Suzie／Martha＋Typing）坐在座位打字、主管 Joe（Walking In Place）
  - 主管巡邏 `WaypointPatrol.cs`（JohnLemon 的 Ghost：NavMeshAgent＋固定 waypoint）、視線 `Observer.cs`（PointOfView 觸發區＋Raycast）
  - Tab 摸魚 `SlackOff.cs`：迷因畫面降壓力，主管靠近螢幕浮出反光人影，被看到就扣信任
  - 3D 皮鞋聲、同事打字聲、冷氣聲、Slack 叮聲：`ProceduralAudio.cs` 用程式產生（AudioListener 照 JohnLemon 放在玩家身上）
  - `Atmosphere.cs`：壓力越高暗角＋色差越重；太陽依時鐘從晨光轉夕陽
  - `SlackFeed.cs`：右上角訊息卡片排隊彈出，每則壓力 +1
- ✅ 11/10 第三版（GAME-PLAN-v3.md，和 Gemini 討論）：
  - 三天制：週一菜鳥報到 → 週三上線連環爆 → 週五裁員名單，每天 6 分鐘；週一的選擇（`dirty_env`、`monday_lie`）週三爆炸，週三的 `hidden_bug` 週五爆炸
  - 待辦倒數 `TaskBoard.cs`：左上角同時 2～3 件，走到地點或打夠字數；週三機房跳電要跑去右側牆邊的機房
  - 10 個資工天條死罪選項 → `GameOverView.cs`：重來這一天／看廣告復活
  - 詐騙支線 `ScamRoute.cs`：高薪廣告 → 園區（慘綠、鐵窗、編號 9527）→ 三段程式看出真相 → 求救或順從 → 警察攻堅 → 165
  - 摸魚改成 12 則 App 內容（迷因板、購物網、看盤、人力銀行、手遊、刷題網），效果有好有壞；主管 4 公尺內有心跳聲
  - 鏡頭靠牆用 CinemachineDeoccluder（從牆頂繞過去）、緊急事件鏡頭震動（Impulse）、程式碼太長的行截斷
  - 結局：轉正加薪／準時下班的人／通過試用期但⋯⋯／撐過第一週；被開除走詐騙支線：被救回來的人／共犯
- 11/10 玩法再調整（Jason 試玩回饋：一直打字、走來走去很無聊、不知道在做什麼；不懂程式的人要能玩；角色模型一定要在）
  - 網頁原型兩版（prototype/）：ship-it.html 太需要懂程式 ✗；levels.html 闖關小遊戲方向對但沒有角色 ✗
  - ✅ 定案方向「座位工作＋和主管談話」：試玩關卡 `Scenes/Round1.unity`（RoundBuilder.cs 產生）
    - 角色坐在座位打字（同事的 Typing 動畫套到你的角色），鏡頭在肩膀後面；放大的螢幕上玩抓蟲（MonitorBugHunt.cs，滑鼠點）
    - 黑幕轉場 → 走到主管旁邊 → 三回合對話選擇（RoundDirector.cs），鏡頭切兩人中景／你的臉／主管的臉，主管會點頭、搖頭、生氣（Mixamo）
    - 結果：主管信任、星等、明天會發生的事
- ✅ 11/10 Jason 確認方向，擴充成闖關版（`Round1.unity`）：
  - 三天 × 4 關＝12 關，關卡地圖一關一關解鎖；內容在 `Resources/levels.json`（每關：誰來找你、說什麼、選項、小遊戲、小知識）
  - 6 種螢幕小遊戲（`Scripts/Round/`）：BugGame 抓蟲、DiffGame 找不同、SortGame 訊息分類／釣魚信、LoadGame 訂單分流、DeployGame 上線時機
  - 選項每次打亂順序（`CampaignDirector.Shuffle`）；偷懶、說謊累積技術債 → 後面關卡變難（每 2 點難度 +1）
  - 同事坐著邊打字邊跟你說話（側面特寫），主管白天站在你座位旁、下班在辦公區談話
  - 第 10 週：同事和主管頭上的浮空名牌（World Space Canvas、縮放 0.005、Billboard 不用 LookAt），只在對話時出現
  - 結局：轉正加薪／通過試用期但⋯⋯（背鍋）／準時下班的人（拒絕背鍋）／撐過第一週／試用期沒過
- 下一步：Jason 試玩調難度；主管信任目前給得太寬（亂選也有 80 分）；接回詐騙支線、摸魚與主管查哨
- 12 月：找人試玩調數值、錄示範影片
- 1 月：找非資工系的同學試玩、調整、錄示範影片、交件

## 怎麼改、怎麼測（Unity 要先關掉）
- 場景全部由 `Editor/OfficeBuilder.cs` 產生：改完跑 `sh build-scene.sh`（或 Unity 選單 Course/4.）
- 自動試玩：`sh run-tests.sh`，機器人用三種選法玩到結局，順便截圖到 `screenshots/`（臉部特寫、跟隨鏡頭、打字、時間軸）
- 改劇情只要改 `Resources/events.json`；新選項記得給 `id`，`EventScheduleTest` 會檢查 id 重複和 `causedBy` 打錯

## 2026-11-10 回饋修正
- 小遊戲鏡頭改到右肩上方，角色的頭在畫面左下角，不再擋到螢幕按鈕（RoundTest 加自動檢查）
- 1-3 訊息分類只剩兩個選項：馬上處理／晚點再說
- 新增 6 個小遊戲：搶救備份、設定強密碼、排今天的工作、找出壞掉的版本、擋下假流量、上線前檢查；12 關各用不同的小遊戲
