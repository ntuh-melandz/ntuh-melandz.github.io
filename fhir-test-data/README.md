# FHIR 測試資料 (fhir-test-data)

本資料夾包含用於匯入至 FHIR Server 的測試資料，包括病患、診斷、用藥、生命徵象與眼底鏡影像。

## 📁 資料結構

### 📋 基礎資料

| 檔案 | 說明 |
|------|------|
| `organization-ntuh.json` | 臺灣大學醫學院附設醫院 Organization 資源 |
| `patients.json` | 5 位測試病患 (Bundle) |
| `conditions.json` | 病患診斷記錄 (高血壓、糖尿病等) |
| `medication-requests.json` | 用藥處方 (脈優錠、降血糖藥等) |
| `observations.json` | 基本檢驗結果 |
| `vital-signs.json` | 生命徵象 (身高、體重、血壓、心率等) |

### 👁️ 眼底鏡檢查

| 檔案 | 說明 |
|------|------|
| `fundus-diagnostic-reports.json` | DiagnosticReport 資源 (AI 判讀結論) |
| `fundus-media.json` | Media 資源 (眼底影像，含 Base64 編碼) |
| `media-fundus-XXX-update.json` | 各別病患的眼底影像更新檔 |

### 🖼️ 眼底影像 Base64

| 檔案 | 對應影像 |
|------|----------|
| `fundus-normal-base64.txt` | 正常眼底 |
| `fundus-normal-2-base64.txt` | 正常眼底 (變體) |
| `fundus-dr-base64.txt` | 糖尿病視網膜病變 (DR) |
| `fundus-glaucoma-base64.txt` | 青光眼病變 |
| `fundus-cataract-base64.txt` | 白內障病變 |
| `fundus-image-base64.txt` | 通用影像模板 |

---

## 🚀 快速匯入

執行以下指令將所有資料匯入至 FHIR Server：

```powershell
# 進入資料夾
cd fhir-test-data

# 執行匯入腳本
.\import-data.ps1
```

### 腳本功能

`import-data.ps1` 會依序執行：

1. ✅ 建立 Organization (臺大醫院)
2. ✅ 建立 5 位測試病患
3. ✅ 匯入診斷、用藥、生命徵象
4. ✅ 匯入眼底鏡檢查報告與影像

### 清理重複資料

如需清理重複建立的資源：

```powershell
.\cleanup-duplicates.ps1
```

---

## 👥 測試病患概覽

| ID | 姓名 | 性別 | 年齡 | 眼底檢查結果 | AI 建議 |
|:---|:-----|:----:|:----:|:------------|:--------|
| patient-001 | 王小明 | 男 | 40 | 糖尿病視網膜病變 | ⚠️ 建議轉診 |
| patient-002 | 李美華 | 女 | 35 | 正常 | ✅ 無需轉診 |
| patient-003 | 陳建國 | 男 | 47 | 嚴重糖尿病視網膜病變 | 🚨 **緊急轉診** |
| patient-004 | 林淑芬 | 女 | 30 | 輕微白內障 | 📅 追蹤觀察 |
| patient-005 | 張志偉 | 男 | 43 | 正常 | ✅ 無需轉診 |

---

## 📝 注意事項

1. **FHIR Server**: 預設目標為衛福部測試環境 `https://thas.mohw.gov.tw/v/r4/fhir`
2. **認證**: 部分操作可能需要 Access Token
3. **影像大小**: 眼底影像 Base64 檔案較大 (~700KB-900KB)，匯入時請耐心等待

---

## 🔧 自訂資料

如需自訂測試資料，請修改對應的 JSON 檔案。資料格式遵循 FHIR R4 規範與 TW Core IG。

**相關規範**:
- [FHIR R4 官方文件](https://hl7.org/fhir/R4/)
- [TW Core IG](https://twcore.mohw.gov.tw/)
