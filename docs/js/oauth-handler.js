// OAuth 2.0 Authorization Code 處理
window.oauthHandler = {
    // 檢查是否有授權碼
    hasAuthorizationCode: function () {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.has('code');
    },

    // 取得授權碼
    getAuthorizationCode: function () {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get('code');
    },

    // 取得 state 參數
    getState: function () {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get('state');
    },

    // 取得錯誤訊息
    getError: function () {
        const urlParams = new URLSearchParams(window.location.search);
        const error = urlParams.get('error');
        const errorDescription = urlParams.get('error_description');

        if (error) {
            return {
                error: error,
                description: errorDescription || ''
            };
        }
        return null;
    },

    // 清除 URL 參數
    clearUrlParams: function () {
        const url = window.location.origin + window.location.pathname;
        window.history.replaceState({}, document.title, url);
    },

    // 交換授權碼為 Access Token
    exchangeCodeForToken: async function (tokenEndpoint, code, redirectUri, clientId) {
        try {
            const response = await fetch(tokenEndpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: new URLSearchParams({
                    grant_type: 'authorization_code',
                    code: code,
                    redirect_uri: redirectUri,
                    client_id: clientId
                })
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`Token exchange failed: ${errorText}`);
            }

            const tokenResponse = await response.json();
            console.log('Token response:', tokenResponse);

            return tokenResponse;
        } catch (error) {
            console.error('Error exchanging code for token:', error);
            throw error;
        }
    },

    // 儲存 token 到 localStorage
    storeToken: function (tokenResponse) {
        if (tokenResponse.access_token) {
            localStorage.setItem('smart_token', tokenResponse.access_token);
        }
        if (tokenResponse.patient) {
            localStorage.setItem('smart_patient', tokenResponse.patient);
        }
        if (tokenResponse.id_token) {
            localStorage.setItem('smart_id_token', tokenResponse.id_token);
        }
        if (tokenResponse.refresh_token) {
            localStorage.setItem('smart_refresh_token', tokenResponse.refresh_token);
        }
        if (tokenResponse.expires_in) {
            const expiresAt = Date.now() + (tokenResponse.expires_in * 1000);
            localStorage.setItem('smart_token_expires_at', expiresAt.toString());
        }
    },

    // 取得儲存的 token
    getStoredToken: function () {
        return localStorage.getItem('smart_token');
    },

    // 取得儲存的 patient ID
    getStoredPatientId: function () {
        return localStorage.getItem('smart_patient');
    },

    // 取得儲存的 FHIR base URL
    getStoredFhirBaseUrl: function () {
        return localStorage.getItem('smart_iss');
    },

    // 取得儲存的 token endpoint
    getStoredTokenEndpoint: function () {
        return localStorage.getItem('smart_token_endpoint');
    },

    // 檢查 token 是否過期
    isTokenExpired: function () {
        const expiresAt = localStorage.getItem('smart_token_expires_at');
        if (!expiresAt) return false;
        return Date.now() >= parseInt(expiresAt);
    },

    // 清除所有儲存的資料
    clearStoredData: function () {
        localStorage.removeItem('smart_token');
        localStorage.removeItem('smart_patient');
        localStorage.removeItem('smart_iss');
        localStorage.removeItem('smart_token_endpoint');
        localStorage.removeItem('smart_id_token');
        localStorage.removeItem('smart_refresh_token');
        localStorage.removeItem('smart_token_expires_at');
    }
};

// 當頁面載入時，自動處理 OAuth 回調
// window.addEventListener('DOMContentLoaded', async function () {
//     const code = window.oauthHandler.getAuthorizationCode();
//     const error = window.oauthHandler.getError();
//
//     if (error) {
//         console.error('OAuth error:', error);
//         alert(`授權失敗: ${error.error}\n${error.description}`);
//         window.oauthHandler.clearUrlParams();
//         return;
//     }
//
//     if (code) {
//         console.log('Authorization code received:', code);
//
//         const tokenEndpoint = window.oauthHandler.getStoredTokenEndpoint();
//         const fhirBaseUrl = window.oauthHandler.getStoredFhirBaseUrl();
//         const clientId = 'my_web_app';
//         const redirectUri = window.location.origin + window.location.pathname;
//
//         if (!tokenEndpoint) {
//             console.error('Token endpoint not found in localStorage');
//             alert('錯誤：找不到 Token Endpoint');
//             return;
//         }
//
//         try {
//             // 交換授權碼為 access token
//             const tokenResponse = await window.oauthHandler.exchangeCodeForToken(
//                 tokenEndpoint,
//                 code,
//                 redirectUri,
//                 clientId
//             );
//
//             // 儲存 token
//             window.oauthHandler.storeToken(tokenResponse);
//
//             // 重新儲存 FHIR Base URL（確保不被丟失）
//             if (fhirBaseUrl) {
//                 localStorage.setItem('smart_iss', fhirBaseUrl);
//             } else if (tokenEndpoint) {
//                 // 後備方案：從 token endpoint 提取 FHIR base URL
//                 // 例如: https://server/fhir/oauth/token -> https://server/fhir
//                 try {
//                     const url = new URL(tokenEndpoint);
//                     const pathParts = url.pathname.split('/');
//                     // 移除 /oauth/token 部分
//                     const fhirPath = pathParts.slice(0, -2).join('/');
//                     const inferredIss = `${url.origin}${fhirPath}`;
//                     localStorage.setItem('smart_iss', inferredIss);
//                     console.log('Inferred FHIR base URL from token endpoint:', inferredIss);
//                 } catch (e) {
//                     console.error('Failed to infer FHIR base URL:', e);
//                 }
//             }
//
//             // 清除 URL 參數
//             window.oauthHandler.clearUrlParams();
//
//             console.log('Token exchange successful');
//
//             // Blazor 會自動載入並檢測到 token
//         } catch (error) {
//             console.error('Error during token exchange:', error);
//             alert(`授權失敗: ${error.message}`);
//         }
//     }
// });
