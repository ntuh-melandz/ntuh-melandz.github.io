課程設計規劃說明

 本課程及SMART平台由衛福部資訊處李建璋處長設計規劃。
 工研院團隊執行及教學。
 透過SMART on FHIR的導入，加值FHIR生態系之應用。
 感謝國內學協會對於FHIR標準的推動。

引用工具 :
本網站所提供之 Sandbox 測試環境使用以下開源套件
 SMART-ON-FHIR/SMART-LAUNCHER-V2

Copyright © Copyright 2018 Boston Children's Hospital
License: Apache-2.0 license

 HAPIFHIR/HAPI-FHIR

Copyright © Copyright 2015 Smile CDR Inc.
License: Apache-2.0 license

 SMART-ON-FHIR/GENERATED-SAMPLE-DATA

Copyright © Copyright 2018 Boston Children's Hospital
License: Apache-2.0 license

線上資源 :
 SMART -- 教學課程 – JavaScript
 SMART on FHIR JavaScript 程式庫 |SMART JS 用戶端程式庫
 SMART on FHIR Python Client: SMART FHIR Client

1


Outline

1. SMART on FHIR : 可替代的醫療應用程式和可重複使用的技術

 如何開發一個SMART App
 SMART App測試
 Launch環境
 EMR中啟動

2. App提案及上架流程

3. SMART App實作練習

2


SMART on FHIR : 可替代的醫療應用程式和可重複使用的技術
- 如何開發一個SMART App

3


可替代的醫療應用程式和可重複使用的技術

SMART和FHIR代表了一種開放、標準化和實用的方式

 應用程式有各自專屬的數據庫、數據模型和介面
。由於缺乏標準化，部署的每個新應用程式都須
投入資源進行維護。

 如果應用程式過時或停止維護，則會遺留下數據

孤島。

 SMART on FHIR允許在同一基礎設施和同一數據模型(FHIR)
上存在各種應用，使用者可以針對通用數據後端構建所有應用
程式。

 FHIR提供了一組模型來標準化EHR或其他臨床數據，SMART
則標準化了第三方應用程式插入數據存儲並訪問臨床資訊的流
程。

FHIR 標準化數據框架

SMART on FHIR

SMART 標準化數據訪問

Picture Reference : smile digital health

AS IS

TO BE

4


SMART on FHIR 是在 FHIR 基礎上加入 OAuth2 + OpenID Connect 的
安全框架，讓 App 可以安全地從 EHR 或 FHIR Server 存取病人資料。

5


App Galleries
•SMART App Gallery:
http://apps.smarthealthit.org/

•Cerner App Gallery
https://code.cerner.com/apps

•Epic App Orchard
https://apporchard.epic.com/Gallery

•Allscripts Application Store
https://allscriptsstore.cloud.prod.iapps.com/

7


8


Steps to Build a SMART on FHIR App

選擇應用程式類型

使用者安全性驗證

使用 SMART 沙盒進行測試

 面向提供者或患者的應用程

式

 行動應用程式
 網路應用程式

確保提供者可以在整合應用
程式之間切換，而毋需每次
都輸入其憑證。 也不需要在
第三方解決方案中輸入密碼，
因為授權是透過他們的EHR
系統進行的。

常規的符合 HIPAA 的應用程式開發程
式（包括資料加密、安全連線的使用等）
也適用於此。

對於所有SMART on FHIR的
開發者，建議使用 SMART
沙盒來測試應用程式的功能。
Epic 等 EHR 供應商也提供
單 獨 的 沙 盒 ， 透 過 使 用 其
EHR系統測試開發者的產品。

9


10


SMART官方提供的開發者資源

啟動器實施指南
(授權及身分驗證)

11


SMART官方提供的開發者資源

12


SMART on FHIR – 認證流程(I)

 iss是 SMART on FHIR 啟動流程的關鍵參數，全名是「FHIR Server Base URL」，用來讓 App 知

道「要跟哪一個 FHIR Server 通訊、做 OAuth2 授權與資料查詢。」，查詢 FHIR Server
的 .well-known 端點。

 EHR Launch：由醫療系統帶入 iss 與 launch。
 App 拿到 iss 後，會去查詢該 FHIR Server 的.well-known/smart-configuration endpoint。

 launch : 用於 Session 交換的 Launch Context ID（由 EHR 產生）。
 App 向 FHIR Server 要求 OAuth2 Metadata，取得 Authorization endpoint、Token

endpoint。

13


SMART on FHIR – 認證流程(II)

 App 發送 Authorization Request，導向使用者登入頁面（使用者授權）。

14


SMART on FHIR – 認證流程(III)

 重新導向App/index.html。
 取得 Authorization Code，使用 code 換取 access_token（與 refresh_token）。

15


SMART on FHIR – 認證流程(IV)

 使用 Access Token 呼叫 FHIR API。
 App取得FHIR Data。

16


支援 SMART App Launch 的應用程式庫

client-js
用戶端和伺服器端

client-py
伺服器端

17


建置 JavaScript 應用程式

SMART 應用程式在 EHR 中運行，須支援 EHR 啟動流程。執行邏輯分成兩部份：
•
•

launch.htm - 由 EHR 呼叫以啟動授權流程。
index.html - 授權成功後，EHR 會將使用者重新定向到實際應用程式執行的地方。

用戶端 :

<!-- launch.html -->
<script src="./node_module/fhirclient/build/fhir-client.js"></script> <script>
FHIR.oauth2.authorize({

由EHR提供

"client_id": "my_web_app",
"scope": "patient/*.read" });

</script>

設定所需使用的FHIR Resources

<!-- index.html -->
<script src="./node_module/fhirclient/build/fhir-client.js"></script>
<script>
FHIR.oauth2.ready()

.then(client => client.request("Patient"))
.then(console.log)
.catch(console.error);

</script>

18


建置 JavaScript 應用程式

client.request(requestOptions, fhirOptions) ：

npm i fhirclient

Ref : 請求範例

無須驗證的情況下擷取FHIR Resources :

const client = new FHIR.client("https://r3.smarthealthit.org");

client.request("Patient/2e27c71e-30c8-4ceb-8c1c-5641e066c0a4");

19


建置 JavaScript 應用程式

驗證過的用戶端擷取FHIR Resources :

const client = new FHIR.client({

serverUrl: "https://r3.smarthealthit.org",
tokenResponse: {

patient: "2e27c71e-30c8-4ceb-8c1c-5641e066c0a4"

}

});

client.request(`Patient/${client.patient.id}`);

20


建置 JavaScript 應用程式

正確啟動並經過身份驗證的用戶端將擁有一個 user 屬性；藉由要求 openid 和 fhirUser取得 :

const id_token = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9." +
"eyJwcm9maWxlIjoiUHJhY3RpdGlvbmVyL3NtYXJ0LVByYWN0aXRpb2" +
"5lci03MjA4MDQxNiIsImZoaXJVc2VyIjoiUHJhY3RpdGlvbmVyL3Nt" +
"YXJ0LVByYWN0aXRpb25lci03MjA4MDQxNiIsInN1YiI6IjM2YTEwYm" +
"M0ZDJhNzM1OGI0YWZkYWFhZjlhZjMyYmFjY2FjYmFhYmQxMDkxYmQ0" +
"YTgwMjg0MmFkNWNhZGQxNzgiLCJpc3MiOiJodHRwOi8vbGF1bmNoLn" +
"NtYXJ0aGVhbHRoaXQub3JnIiwiaWF0IjoxNTU5MzkyMjk1LCJleHAi" +
"OjE1NTkzOTU4OTV9.niEs55G4AFJZtU_b9Y1Y6DQmXurUZZkh3WCud" +
"ZgwvYasxVU8x3gJiX3jqONttqPhkh7418EFssCKnnaBlUDwsbhp7xd" +
"WN4o1L1NvH4bp_R_zJ25F1s6jLmNm2Qp9LqU133PEdcRIqQPgBMyZB" +
"WUTyxQ9ihKY1RAjlztAULQ3wKea-rfe0BXJZeUJBsQPzYCnbKY1dON" +
"_NRd8N9pTImqf41MpIbEe7YEOHuirIb6HBpurhAHjTLDv1IuHpEAOx" +
"pmtxVVHiVf-FYXzTFmn4cGe2PsNJfBl8R_zow2n6qaSANdvSxJDE4D" +
"UgIJ6H18wiSJJHp6Plf_bapccAwxbx-zZCw";

const client = new FHIR.client({

serverUrl: "https://r3.smarthealthit.org",
tokenResponse: { id_token }

});

client.request(client.user.fhirUser);

21


建置 JavaScript 應用程式

顯示某位患者的用藥 :

const client = new FHIR.client("https://r3.smarthealthit.org");
const getPath = client.getPath;

client.request(`/MedicationRequest?patient=smart-1642068`, {

resolveReferences: "medicationReference"

}).then(data => data.entry.map(item => getMedicationName(

getPath(item, "resource.medicationCodeableConcept.coding") ||
getPath(item, "resource.medicationReference.code.coding")

)));

22


23


建置 Python 應用程式

無須驗證的情況下擷取FHIR Resources :

pip install fhirclient

Ref : SMART on FHIR Python Client: SMART FHIR Client

from fhirclient import client
from fhirclient.models.patient import Patient

settings = {

'app_id': 'my_web_app',
'api_base': 'https://r4.smarthealthit.org'

}
smart = client.FHIRClient(settings=settings)

patient = Patient.read('2cda5aad-e409-4070-9a15-e1c35c46ed5a', smart.server)
print(patient.birthDate.isostring)
# '1992-07-03'
print(smart.human_name(patient.name[0]))
# 'Mr. Geoffrey Abbott'

24


建置 Python 應用程式

從伺服器中搜尋Resource :

from fhirclient import client
from fhirclient.models.encounter import Encounter
from fhirclient.models.procedure import Procedure

settings = {

'app_id': 'my_web_app',
'api_base': 'https://r4.smarthealthit.org'

}
smart = client.FHIRClient(settings=settings)

search = Encounter.where(struct={'subject': '2cda5aad-e409-4070-9a15-e1c35c46ed5a', 'status':
'finished'})
print({res.type[0].text for res in search.perform_resources_iter(smart.server)})
# {'Encounter for symptom', 'Encounter for check up (procedure)'}

25


# to include the resources referred to by the encounter via `subject` in the results
search = search.include('subject')
print({res.resource_type for res in search.perform_resources_iter(smart.server)})
# {'Encounter', 'Patient'}

# to include the Procedure resources which refer to the encounter via `encounter`
search = search.include('encounter', Procedure, reverse=True)
print({res.resource_type for res in search.perform_resources_iter(smart.server)})
# {'Encounter', 'Patient', 'Procedure'}

# to get the raw Bundles instead of resources only, you can use:
bundles = search.perform_iter(smart.server)
print({entry.resource.resource_type for bundle in bundles for entry in bundle.entry})
# {'Encounter', 'Patient', 'Procedure'}

26


建置 Python 應用程式

Flask App :

https://github.com/smart-on-fhir/client-py/blob/main/demos/flask/flask_app.py

應用程式將啟動一個 Web 伺服器，監聽 localhost:8000，並提示使用者登入沙盒伺服器並選擇患者。
然後，它會檢索所選患者的人口統計和藥物處方，並將其列在一個簡單的 HTML 頁面上。

git clone https://github.com/smart-on-fhir/client-py.git
cd client-py/demos/flask
python3 -m venv env
. env/bin/activate
pip install -r requirements.txt
# Edit flask_app.py and put your own server's URL as api_base.
./flask_app.py

27


開始你的第一個SMART App

28


建立可發布網站的環境(Web-Based App)

GitHub Pages 是 GitHub 提供的靜態網站託管服務，可以直接把你的程式碼或
HTML 網頁部署成網站。

Step 1：建立 Repository

1.登入 GitHub

2.點選 New Repository

3.名稱輸入：

•若是個人網站 → username.github.io

•若是專案網站 → 任意名稱（例如 myproject）

4.建立完成後可選「Add a README」方便初始化。

29


建立可發布網站的環境(Web-Based App)

Step 2：啟用 GitHub Pages

1.到該 repo 頁面，點選上方的 Settings

2.左邊選單 → Pages

3.在「Source」選擇：

•Branch：main

•Folder：/ (root)

4.按 Save

30


建立可發布網站的環境(Web-Based App)

31


建立可發布網站的環境(Web-Based App)

32


Launch.html

<!DOCTYPE html>
<html>

<head>

<meta charset="UTF-8" />
<title>Launch My APP</title>
<script src="https://cdn.jsdelivr.net/npm/fhirclient/build/fhir-client.js"></script>

</head>
<body>

<script>

FHIR.oauth2.authorize({

// The client_id that you should have obtained after registering a client at the EHR.
clientId: "my_web_app",

// The scopes that you request from the EHR. In this case we want to:
scope: "launch openid fhirUser patient/*.read",

// Typically, if your redirectUri points to the root of the current directory
// (where the launchUri is), you can omit this option because the default value is ".".
redirectUri: "index.html"

});

</script>

</body>

</html>

33


index.html

<!DOCTYPE html>
<html lang="en">

<head>

<meta charset="UTF-8" />
<title>Example SMART App</title>
<script src="https://cdn.jsdelivr.net/npm/fhirclient/build/fhir-client.js"></script>
<style>

#patient, #meds {

font-family: Monaco, monospace;
white-space: pre;
font-size: 13px;
height: 30vh;
overflow: scroll;
border: 1 px solid #CCC;

}
</style>

</head>
<body>

<h4>Current Patient</h4>
<div id="patient">Loading...</div>
<br/>
<h4>Medications</h4>
<div id="meds">Loading...</div>

34


index.html

<script type="text/javascript">

FHIR.oauth2.ready().then(function(client) {

// Render the current patient (or any error)
client.patient.read().then(

function(pt) {

 document.getElementById("patient").innerText = JSON.stringify(pt, null, 4);

},
function(error) {

 document.getElementById("patient").innerText = error.stack;

}

);
// Get MedicationRequests for the selected patient
client.request("/MedicationRequest?patient=" + client.patient.id, {

 resolveReferences: [ "medicationReference" ],
 graph: true

})
// Reject if no MedicationRequests are found
.then(function(data) {

 if (!data.entry || !data.entry.length) {

  throw new Error("No medications found for the selected patient");

 }
 return data.entry;

})

35


index.html

// Render the current patient's medications (or any error)

.then(

 function(meds) {

  document.getElementById("meds").innerText = JSON.stringify(meds, null, 4);

 },
 function(error) {

  document.getElementById("meds").innerText = error.stack;

 }

);

}).catch(console.error);

</script>

</body>

</html>

36


SMART on FHIR : 可替代的醫療應用程式和可重複使用的技術
- SMART App測試

37


啟動你的SMART App

38


啟動你的SMART App

39


啟動你的SMART App

40


啟動你的SMART App

41


42


43


SMART App Launch Test Kit

SMART App Launch IG : http://hl7.org/fhir/smart-app-launch/index.html

本實施指南描述了一組基於 OAuth 2.0 的基本模式，供客戶端應用程式授
權、身份驗證以及與基於 FHIR 的資料系統整合。

SMART 定義兩種用戶端授權模式
 應用程式啟動授權 : 與應用程式共享當前選擇的患者資料

• EX. Apple Health

 後端服務授權 :允許後端服務與 EHR 連動，無任何使用者介入。

SMART 定義兩種用戶端身份驗證模式
 非對稱（“私鑰 JWT”）身份驗證 : SMART 的首選身份驗證方法，因為它避免透

過線路發送共用金鑰。

 對稱（「客戶端秘密」）身份驗證 :使用客戶端和伺服器之間預先共用的金鑰對客

戶端進行身份驗證。

44


Epic 中建置和啟動 SMART on FHIR 應用程式

45


SMART on FHIR : 可替代的醫療應用程式和可重複使用的技術
- SMART App提案及上架流程

53


SMART App提案及上架流程

54


SMART App提案及上架流程

55


SMART App提案及上架流程

56


SMART App提案及上架流程

57


SMART App提案及上架流程

58


SMART App提案及上架流程

59


SMART App提案及上架流程

60


SMART App提案及上架流程

61


SMART App提案及上架流程

62


SMART App提案及上架流程

63


SMART App提案及上架流程

64


SMART App提案及上架流程

65


SMART App 實作練習

Exercise 1 : Building a JavaScript App SMART -- Tutorials – JavaScript

Exercise 2 : SMART on FHIR app tutorial

66


THANK YOU

67
