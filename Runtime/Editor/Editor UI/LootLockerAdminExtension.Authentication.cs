using System;
using LootLocker.Extension.Responses;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER && !LOOTLOCKER_DISABLE_EDITOR_EXTENSION
namespace LootLocker.Extension
{
    public partial class LootLockerAdminExtension : EditorWindow
    {
        public void Login()
        {
            if (string.IsNullOrEmpty(emailField?.value) || string.IsNullOrEmpty(passwordField?.value))
            {
                ShowPopup("Error", "Email or Password is empty.");
                HideLoading();
                return;
            }

            ShowLoadingAndExecute(() =>
            {
                try
                {
                    LootLockerAdminManager.AdminLogin(emailField.value, passwordField.value, OnLoginComplete);
                }
                catch (Exception ex)
                {
                    ShowPopup("Error", $"Unexpected error: {ex.Message}");
                    HideLoading();
                }
            });
        }

        private void OnLoginComplete(LoginResponse response)
        {
            if (!response.success)
            {
                string errorMsg = !string.IsNullOrEmpty(response.errorData?.message) 
                    ? response.errorData.message 
                    : "We couldn't recognize your information or there is no user with this email, please check and try again!";
                ShowPopup("Error", errorMsg);
                HideLoading();
                return;
            }

            if (response.mfa_key != null)
            {
                mfaKey = response.mfa_key;
                if (menu != null) menu.style.display = DisplayStyle.Flex;
                HideLoading();
                SwapFlows(mfaFlow);
            }
            else
            {
                CompleteLogin(response.auth_token);
            }

            if (menuLogoutBtn != null) menuLogoutBtn.style.display = DisplayStyle.Flex;
        }

        private void CompleteLogin(string authToken)
        {
            if (menu != null) menu.style.display = DisplayStyle.Flex;
            LootLockerConfig.current.adminToken = authToken;
            LootLockerEditorData.SetAdminToken(authToken);
            HideLoading();
            SwapFlows(gameSelectorFlow);
        }

        public void SignIn(EventBase e)
        {
            ShowLoadingAndExecute(() =>
            {
                LootLockerAdminManager.MFAAuthenticate(mfaKey, codeField.value, (onComplete) =>
                {
                    if (!onComplete.success)
                    {
                        ShowPopup("Error", "Could not authenticate MFA!");
                        HideLoading();
                        return;
                    }

                    CompleteLogin(onComplete.auth_token);
                    SwapFlows(gameSelectorFlow);
                });
            });
        }

        public void RefreshUserInformation(Action onComplete)
        {
            ShowLoadingAndExecute(() =>
            {
                LootLockerAdminManager.GetUserInformation((response) =>
                {
                    if (!response.success)
                    {
                        ShowPopup("Error", "Your token has expired, will redirect you to Login page now!");
                        Debug.Log(response.errorData.message);
                        HideLoading();
                        Logout();
                        return;
                    }

                    ProcessUserInformation(response);
                    UpdateLicenseCountdownUI();
                    gameDataRefreshTime = DateTime.Now;
                    onComplete?.Invoke();
                    HideLoading();
                });
            });
        }

        private void ProcessUserInformation(LoginResponse response)
        {
            gameData.Clear();

            foreach (var org in response.user.organisations)
            {
                foreach (var game in org.games)
                {
                    if (game.id == LootLockerEditorData.GetSelectedGame() || game.development.id == LootLockerEditorData.GetSelectedGame())
                    {
                        LootLockerEditorData.SetSelectedGameName(game.name);
                    }
                    game.organisation_name = org.name;
                    gameData.Add(game.id, game);
                }
            }
        }

        void CreateNewAPIKey()
        {
            int gameId = LootLockerEditorData.GetSelectedGame();
            if (gameId == 0)
            {
                ShowPopup("Error", "No active Game found!");
                return;
            }

            if (isStage && gameData.ContainsKey(gameId))
            {
                gameId = gameData[gameId].development.id;
            }

            ShowLoadingAndExecute(() =>
            {
                LootLockerAdminManager.GenerateKey(gameId, newApiKeyName.value, "game", (onComplete) =>
                {
                    if (!onComplete.success)
                    {
                        ShowPopup("Error", "Could not create a new API Key!");
                        HideLoading();
                        return;
                    }

                    APIKeyTemplate(onComplete);
                    HideLoading();
                });
            });

            if (newApiKeyName != null) newApiKeyName.value = "";
        }

        public void RefreshAPIKeys()
        {
            if (apiKeyList != null) apiKeyList.Clear();
            
            int gameID = LootLockerEditorData.GetSelectedGame();
            if (isStage && gameData.ContainsKey(gameID))
            {
                gameID = gameData[gameID].development.id;
            }
            
            isLoadingKeys = true;

            ShowLoadingAndExecute(() =>
            {
                LootLockerAdminManager.GetAllKeys(gameID, (onComplete) =>
                {
                    if (!onComplete.success)
                    {
                        ShowPopup("Error", "Could not find API Keys!");
                        HideLoading();
                        return;
                    }

                    foreach (var key in onComplete.api_keys)
                    {
                        APIKeyTemplate(key);
                    }

                    isLoadingKeys = false;
                    HideLoading();
                });
            });
        }

        void ConfirmLogout()
        {
            ShowPopup("Confirm Logout", "Are you sure you want to log out? This will clear your admin token and settings.");
            if (popupBtn != null)
            {
                popupBtn.clickable.clickedWithEventInfo -= ClosePopup;
                popupBtn.clickable.clickedWithEventInfo += (evt) =>
                {
                    Logout();
                    ClosePopup(evt);
                };
            }
        }

        void Logout()
        {
            if (loadingPage != null) loadingPage.style.display = DisplayStyle.Flex;
            LootLockerEditorData.ClearLootLockerPrefs();

            gameData.Clear();
            if (apiKeyList != null) apiKeyList.Clear();
            if (gameSelectorList != null) gameSelectorList.Clear();
            
            if (loadingPage != null) loadingPage.style.display = DisplayStyle.None;
            SwapFlows(loginFlow);
        }
    }
}
#endif
