#!/usr/bin/env python3
"""
將 fhir-test-data 資料夾中的個別 FHIR JSON 檔案合併為一個 Bundle，並打包成 ZIP。

用法:
    python merge-bundle.py

輸出:
    fhir-test-data-bundle.json  - 合併後的 FHIR Bundle
    fhir-test-data-bundle.zip   - 壓縮檔（供提交測試用）
"""

import json
import os
import glob
import zipfile

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))

# 依匯入順序排列（Organization 先，Patient 次之，其餘依賴在後）
SOURCE_FILES = [
    "organization-ntuh.json",
    "patients.json",
    "conditions.json",
    "observations.json",
    "vital-signs.json",
    "medication-requests.json",
    "fundus-diagnostic-reports.json",
    "fundus-media.json",
]

def load_entries(filepath):
    """從 FHIR JSON 檔案載入 entry，支援 Bundle 或單一 Resource。"""
    with open(filepath, "r", encoding="utf-8-sig") as f:
        data = json.load(f)

    if "entry" in data:
        return data["entry"]
    else:
        # 單一 resource，包成 entry
        return [{
            "resource": data,
            "request": {
                "method": "PUT",
                "url": f"{data['resourceType']}/{data['id']}"
            }
        }]

def main():
    all_entries = []

    # 載入主要檔案
    for filename in SOURCE_FILES:
        filepath = os.path.join(SCRIPT_DIR, filename)
        if not os.path.exists(filepath):
            print(f"  [SKIP] {filename} (not found)")
            continue
        entries = load_entries(filepath)
        all_entries.extend(entries)
        print(f"  {filename}: {len(entries)} entries")

    # 載入 media-fundus-*-update.json
    update_files = sorted(glob.glob(os.path.join(SCRIPT_DIR, "media-fundus-*-update.json")))
    for filepath in update_files:
        filename = os.path.basename(filepath)
        entries = load_entries(filepath)
        all_entries.extend(entries)
        print(f"  {filename}: {len(entries)} entries")

    # 組合 Bundle
    bundle = {
        "resourceType": "Bundle",
        "type": "transaction",
        "entry": all_entries
    }

    # 輸出 JSON
    json_path = os.path.join(SCRIPT_DIR, "fhir-test-data-bundle.json")
    with open(json_path, "w", encoding="utf-8") as f:
        json.dump(bundle, f, ensure_ascii=False, indent=2)

    json_size = os.path.getsize(json_path)
    print(f"\nTotal entries: {len(all_entries)}")
    print(f"Output JSON: {json_path} ({json_size / 1024:.0f} KB)")

    # 打包 ZIP
    zip_path = os.path.join(SCRIPT_DIR, "fhir-test-data-bundle.zip")
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
        zf.write(json_path, "fhir-test-data-bundle.json")

    zip_size = os.path.getsize(zip_path)
    print(f"Output ZIP:  {zip_path} ({zip_size / 1024:.0f} KB)")

    print(f"\nDone. Output: fhir-test-data-bundle.json + fhir-test-data-bundle.zip")

if __name__ == "__main__":
    main()
