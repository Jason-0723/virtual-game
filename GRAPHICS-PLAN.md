# 畫質升級計畫（Claude × Gemini，2026-11-10）

Gemini 對話：https://gemini.google.com/app/33d2040200b218ae （Gemini Argon 模式，附了 follow／face／typing 三張截圖）

## Gemini 說看起來廉價的三個原因
1. 沒有角落接觸陰影、材質全是一樣光滑的純色 → 東西像浮在空中的塑膠積木
2. 室內卻用戶外太陽打光，特寫時背景露出天空 → 不像在辦公室
3. 角色臉有一格一格的稜角，照片的陰影被烤進貼圖，眼鏡和臉糊在一起

## 要做的（依「投入少、效果大」排序）
| # | 做法 | Unity 設定 |
|---|---|---|
| 1 | SSAO 角落陰影 | URP Renderer 加 Screen Space Ambient Occlusion：Intensity 1.3、Radius 0.3、Direct Lighting Strength 0.3 |
| 2 | 材質分層＋反射 | 一個 Reflection Probe（Box Projection、包住辦公室）；螢幕 Smoothness 0.85、玻璃 0.95 透明 0.25、隔板布 0.08、牆 0.15 暖灰 #EAE6DF |
| 3 | 室內光 | 天花板用朝下的 Plane（單面，上方跟隨鏡頭看不到、特寫看得到）＋發光燈管；環境光 Sky #D8E2DC／Equator #B8B4AC／Ground #5A554C；陰影距離 25、Soft Shadows |
| 4 | 特寫質感 | 相機背景改實心暖灰 #C5C0B8；特寫時開 Bokeh 景深（對焦 1.3 m、f/2.8）；角色斜後方一盞微弱輪廓光 |
| 5 | 字型與程式碼上色 | 保留 uGUI，字型換微軟正黑體、程式碼用 Consolas；打字畫面用 Rich Text 上 VS Code 配色（關鍵字藍、註解綠、型別青、字串橘） |
| 6 | 角色臉 | Player.fbx 的 Normals 改 Calculate、Smoothing Angle 70；材質 Smoothness 0.1＋Emission 用自己的貼圖 #3a3a3a（和補光燈二選一，選 Emission） |
| 7 | 同事 | 放 4～6 個坐著打字的同事（Mixamo 免費角色＋Typing 動畫）—— Gemini 認為這比任何材質調整都更能去掉「空城 Demo 感」 |

需要你自己動手的（要登入或下載）：
- 7 的同事模型要你登入 Mixamo 下載
- 辦公室小物件（Asset Store「Office Pack Free」類）要你用 Unity 帳號加入
- 臉部貼圖放大（Upscayl）、眼鏡換成 3D 模型：可選，效果好但要手動修圖

## 不做的
- 烘 Lightmap（程式產生的方塊沒拆 UV，等很久還會黑斑）
- 重拓樸、臉部表情動畫（改用頭頂漫畫符號＋鏡頭震動表現情緒）
- 螢幕空間反射、很多盞帶陰影的燈（筆電會掉幀）
- 換衣服貼圖

## 玩法參考（《今天也要平安下班》，只參考玩法，畫面不參考）
- 時間一直走；等你的人頭上有提示、地上有指路 → 我們改成 Slack 風格的深色氣泡＋地上淡青色發光虛線（Gemini：黃色「!」和金色點點太像手遊，會破壞寫實感）
- 可以沖咖啡分給同事、坐下回精力、精力太低撐太久會昏倒 → 可以接到我們的壓力和茶水間
- 紅色緊急事件要先處理 → 對應我們的 Bug 事件
- 「事情一定做不完，練習拒絕、找人合作、照顧自己」→ 跟我們的主題一致
