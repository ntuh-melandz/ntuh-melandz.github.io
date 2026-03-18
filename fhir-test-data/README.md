# FHIR 測試資料 (fhir-test-data)

本資料夾包含用於匯入至 FHIR Server 的測試資料，包括病患、診斷、用藥、生命徵象與眼底鏡影像。

> **注意**: 所有資料皆為虛構，不含真實病患資料。

## 資料結構

### 基礎資料

| 檔案 | 說明 |
|------|------|
| `organization-ntuh.json` | 臺灣大學醫學院附設醫院 Organization 資源 |
| `patients.json` | 5 位測試病患 (Bundle) |
| `conditions.json` | 病患診斷記錄 (高血壓、糖尿病等) |
| `medication-requests.json` | 用藥處方 (脈優錠、降血糖藥等) |
| `observations.json` | 基本檢驗結果 |
| `vital-signs.json` | 生命徵象 (身高、體重、血壓、心率等) |

### 眼底鏡檢查

| 檔案 | 說明 |
|------|------|
| `fundus-diagnostic-reports.json` | DiagnosticReport 資源 (AI 判讀結論) |
| `fundus-media.json` | Media 資源 (眼底影像，含 Base64 編碼) |
| `media-fundus-XXX-update.json` | 各別病患的眼底影像更新檔 |

### 眼底影像 Base64

| 檔案 | 對應影像 |
|------|----------|
| `fundus-normal-base64.txt` | 正常眼底 |
| `fundus-normal-2-base64.txt` | 正常眼底 (變體) |
| `fundus-dr-base64.txt` | 糖尿病視網膜病變 (DR) |
| `fundus-glaucoma-base64.txt` | 青光眼病變 |
| `fundus-cataract-base64.txt` | 白內障病變 |
| `fundus-image-base64.txt` | 通用影像模板 |

### 工具腳本

| 檔案 | 說明 |
|------|------|
| `import-data.ps1` | PowerShell 匯入腳本（逐筆匯入至 FHIR Server） |
| `import-utf8.ps1` | UTF-8 編碼匯入腳本 |
| `cleanup-duplicates.ps1` | 清理重複資源 |
| `view_audit_events.ps1` | 查看 AuditEvent |
| `merge-bundle.py` | Python 合併腳本（產生單一 Bundle ZIP） |

### 輸出檔案

| 檔案 | 說明 |
|------|------|
| `fhir-test-data-bundle.zip` | 合併後的 FHIR Bundle 壓縮檔（供外部提交測試用） |

---

## 快速匯入

### 方式一：逐筆匯入至 FHIR Server

```powershell
cd fhir-test-data
.\import-data.ps1
```

### 方式二：合併為單一 Bundle ZIP

修改個別 JSON 後重新產生合併 Bundle：

```bash
cd fhir-test-data
python merge-bundle.py
```

會自動產生 `fhir-test-data-bundle.zip`，包含所有資源的單一 FHIR transaction Bundle。

---

## 測試病患概覽

| ID | 姓名 | 性別 | 生日 | 主要診斷 | 眼底檢查結果 | AI 建議 |
|:---|:-----|:----:|:----:|:---------|:------------|:--------|
| patient-ntuh-001 | 王小明 | 男 | 1985-03-15 | 高血壓 | 糖尿病視網膜病變 | 建議轉診 |
| patient-ntuh-002 | 李小美 | 女 | 1990-07-22 | 糖尿病 | 正常 | 無需轉診 |
| patient-ntuh-003 | 陳大建 | 男 | 1978-11-08 | 氣喘(已緩解) | 嚴重糖尿病視網膜病變 | 緊急轉診 |
| patient-ntuh-004 | 林小芬 | 女 | 1995-05-30 | 癲癇 | 輕微白內障 | 追蹤觀察 |
| patient-ntuh-005 | 張大偉 | 男 | 1982-09-18 | 慢性阻塞性肺病 | 正常 | 無需轉診 |

---

## 注意事項

1. **FHIR Server**: 預設目標為衛福部測試環境 `https://thas.mohw.gov.tw/v/r4/fhir`
2. **認證**: 部分操作可能需要 Access Token
3. **影像大小**: 眼底影像 Base64 檔案較大 (~700KB-900KB)，匯入時請耐心等待
4. **編碼**: 部分檔案帶有 UTF-8 BOM，`merge-bundle.py` 已處理此問題

---

## 自訂資料

如需修改測試資料：

1. 編輯對應的個別 JSON 檔案
2. 執行 `python merge-bundle.py` 重新產生合併 Bundle
3. 將 `fhir-test-data-bundle.zip` 提交給測試環境

資料格式遵循 FHIR R4 規範與 TW Core IG。

**相關規範**:
- [FHIR R4 官方文件](https://hl7.org/fhir/R4/)
- [TW Core IG](https://twcore.mohw.gov.tw/)
