---
name: ebo-design-request
description: Use when a change request for the EBO design canvas arrives — from the EBO development session via SendMessage or from the user — asking to move, resize, add or reword something on a board, or when a board change is done and must be reported back with its canvas version
---

# 接改稿需求

開發 session 不改畫布，有需求會用 SendMessage 交給設計 session。這份是設計 session 從收到需求到回報的流程。工具用法見 `ebo-canvas`，規則見 `ebo-design-rules`。

## 1. 讀需求

- 逐條列出要改的地方和給的數字。
- 對照 `ebo-design-rules` 和 memory（尤其 `ebo-screen-ratio`），每條歸到一類：
  - **違反 `ebo-design-rules`**（例：要加操作說明文字）：這條不做，其他照做，回報寫原因。
  - **疑似打錯**（方向、數字跟上下文或先前的稿子對不上）：照原文做，回報指出來，請對方確認。
  - **推翻 memory 裡記的使用者決定**（例：要加回已拿掉的倍率上限）：這條先不做，回訊問來訊者是不是使用者改了主意；其他條照做。
  - 其他：照做。
- 需求說「只改這個」時，其他地方不動。

## 2. 改

- 先備份、再用腳本改，不手改板子；備份怎麼命名見 `ebo-canvas` 的「改板子」。
- 板子的說明區要一起改：數字、公式、範例都換成新的；新的狀態要加進 `startView` 和切換 chip。
- 新增會出現在畫面上的文字前，先過一遍 `ebo-design-rules` 的文字規則。

## 3. 驗證

每項都要做，不要只看預設畫面：

- 每個外框都截圖（624、683、780、細長、扁）。
- 極端狀態：捲到最底、最長的文字（長錯誤訊息、長名字）、停用、空資料。
- 需求裡給的數字用 `test` 量出實際位置，對得上才算數。
- 碰到重疊、折行、被蓋住，就算不是這次要改的也一起修，並寫進回報。
- 有互動的改動寫 `test` 腳本，用 `tap` 點。原本的正常流程也要再跑一次（例：加了確認密碼，一般註冊仍要能進建角）。

## 4. 發布

照 `ebo-canvas` 的發布步驟，記下結果裡的畫布版本。基準規格變了（比例、倍率、版位），更新對應的 memory。

## 5. 回報

用 SendMessage 回到來訊的位址，依序寫：

1. **畫布版本**：例「v463」。
2. **改了什麼**：每項給最後的數字（位置、大小、公式），講到能直接照著實作。
3. **跟需求不一樣的地方**：改了什麼、為什麼（例「『兩次輸入的密碼不一樣』最後一個字會折行，改成『兩次密碼不一樣』」）。違反規則而沒做的條目也寫在這裡。
4. **順手修的**：原本就有的問題。
5. **怎麼看**：`startView` 名稱與預覽指令，例
   `python3 $S/canvas.py play TitleFit -o tf -s "this.startView = 'title'=>this.startView = 'relogin'" && python3 $S/canvas.py shot tf --size 2690x970`
6. **等確認的事**：疑似打錯、要推翻先前決定而先停下的條目。沒有就不寫。

最後跟使用者說一段同樣的摘要（中文、短）。
