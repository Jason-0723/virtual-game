# 課程 Unity 套件（Roll a Ball + 第 6／8／9 週）

## 內容
```
Assets/Course/
  Scripts/RollABall/   PlayerController, CameraController, Rotator, RandomGen
  Scripts/Interaction/ InfoTag, ClickSelector, Spin          ← 第 8 週
  Scripts/Character/   SimpleWalker                           ← 第 9 週
  Editor/              CourseSceneBuilder（上方選單 Course/...）
Blender/clean_scan.py  ← 第 6 週：刪飛面 → 補洞 → 減面 → 匯出 + 面數報告
```

## 安裝（Unity 6.3，Universal 3D 專案）
1. Unity Hub 開一個新專案：選 6000.3 的 **Universal 3D**
2. `Window → Package Manager → Unity Registry`，安裝 **Input System**，跳出的視窗按 **Yes**（重啟編輯器）
3. 把 `Assets/Course` 整個資料夾拖進專案的 `Assets` 裡
4. 上方選單會出現 **Course**

## 使用
### Roll a Ball
`Course → 1. 建立 Roll a Ball 場景`
- 第一次按會先匯入 TMP Essentials，匯入完成後**再按一次**
- 場景會存在 `Assets/Course/Scenes/RollABall.unity`，按 ▶ 後用 WASD／方向鍵操作
- 建好的內容跟投影片一致：地板、球、四面牆、PickUp prefab（Trigger + Kinematic）、PickUpGen、計分與 WinText

### 第 8 週：點選 → 高亮 → 資訊面板
1. 把你的掃描模型（.glb/.fbx）拖進場景
2. 在 Hierarchy 選取**至少 3 個**物件
3. `Course → 2. 把點選互動加到目前場景`
   - 沒有 Collider 的物件會自動包一個 Box Collider
   - 每個物件會掛上 InfoTag，在 Inspector 填寫名稱和描述
   - 會自動建立 Selector 和右上角的資訊面板
4. 按 ▶，點物件會變黃並顯示資訊；點空白處取消選取
5. 進階題：在物件上掛 `Spin`，勾選 `onlyWhenSelected`

**中文顯示成方塊？** TMP 內建字型沒有中文字。下載 Noto Sans TC 字型 → 拖進 Assets → 對字型檔按右鍵
`Create → TextMeshPro → Font Asset`（Atlas Population Mode 設成 **Dynamic**）→ 把產生的字型資源指定到面板的 Title 和 Description。

### 第 9 週：角色走路（W 走、放開停、A/D 轉）
1. Mixamo（用自己的 Adobe 帳號）下載 **Idle**、**Walking**：FBX for Unity・Without Skin・30 FPS；Walking 要勾 **In Place**
2. 把第 7 週的角色 FBX 和兩個動作檔拖進 Assets，**三個都要** Rig → Humanoid → Create From This Model → Apply
3. 兩個動作檔 Animation 頁籤勾 **Loop Time** → Apply
4. 角色拖進場景、在 Hierarchy 選取 → `Course → 3. 角色接上 Idle／Walking 狀態機`
   - 自動建 `Assets/Course/Animation/HeroAnimator.controller`（isWalking、取消 Has Exit Time、Duration 0.2）
   - 角色掛上 Animator（取消 Apply Root Motion）和 SimpleWalker
   - 第 2、3 步沒做會在 Console 出黃色警告
5. 按 ▶，腳在地上滑就調 SimpleWalker 的 moveSpeed

### 第 6 週：Blender 自動清理
```bash
blender -b --python Blender/clean_scan.py -- 我的門口.glb 門口_clean.glb 50000
```
最後會印出作業要交的三個數字：清理前面數、清理後面數、減幅。
參數依序為：輸入檔、輸出檔、目標三角面數（預設 50000）、補洞的最大邊數（預設 40，太大的洞就留著，請重掃）。

## 注意
- 第 8 週規定程式**必須能逐行解釋**，所以每支腳本都有中文註解，交作業前請自己讀過一次
- 清理前後對照截圖、操作錄影要自己在電腦上錄
