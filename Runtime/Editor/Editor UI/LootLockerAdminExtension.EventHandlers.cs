using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER && !LOOTLOCKER_DISABLE_EDITOR_EXTENSION
namespace LootLocker.Extension
{
    public partial class LootLockerAdminExtension : EditorWindow
    {
        // Event handlers for UI interactions
        private void OnPasswordKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return)
            {
                Login();
            }
        }

        private void OnNewApiKeyCancelClick(MouseDownEvent evt)
        {
            if (newApiKeyName != null) newApiKeyName.value = "";
            if (newApiKeyWindow != null) newApiKeyWindow.style.display = DisplayStyle.None;
        }

        private void OnCreateApiKeyClick()
        {
            CreateNewAPIKey();
            if (newApiKeyWindow != null) newApiKeyWindow.style.display = DisplayStyle.None;
        }

        private void OnCreateNewApiKeyClick()
        {
            if (newApiKeyWindow != null) newApiKeyWindow.style.display = DisplayStyle.Flex;
        }

        private void OnSettingsClick()
        {
            previousFlow = currentFlow;
            RequestFlowSwitch(settingsFlow, LoadSettingsUI);
        }

        private void OnSettingsBackClick()
        {
            if (previousFlow != null)
            {
                RequestFlowSwitch(previousFlow);
                previousFlow = null;
            }
            else
            {
                RequestFlowSwitch(apiKeyFlow);
            }
        }

        private void OnGameSelected(EventBase e)
        {
            if (!AtTarget(e)) return;

            var target = e.target as Button;
            if (target == null) return;

            LootLockerEditorData.SetSelectedGame(target.name);
            var selectedGameData = gameData[int.Parse(target.name)];

            LootLockerEditorData.SetSelectedGameName(selectedGameData.name);
            gameName.text = selectedGameData.name;
            UpdateLicenseCountdownUI();

            LootLockerEditorData.SetEnvironmentStage();
            isStage = true;

            ShowLoadingAndExecute(() =>
            {
                LootLockerAdminManager.GetGameDomainKey(LootLockerEditorData.GetSelectedGame(), (onComplete) =>
                {
                    HideLoading();
                    if (!onComplete.success)
                    {
                        ShowPopup("Error", "Could not find Selected game!");
                        return;
                    }
                    LootLockerConfig.current.domainKey = onComplete.game.domain_key;
                });
            });

            SwapFlows(apiKeyFlow);
        }

        private void OnAPIKeySelected(EventBase e)
        {
            if (!AtTarget(e)) return;

            var target = e.target as Button;
            if (target == null) return;

            LootLockerConfig.current.apiKey = target.name;
            SwapNewSelectedKey();
            ShowPopup("API Key Selected", $"API Key {target.name} is now active.");
        }

        private void LicenseCountdownIconClick(MouseDownEvent evt)
        {
            Application.OpenURL("https://console.lootlocker.com");
            evt.StopPropagation();
        }

        private void LicenseCountdownContainerClick(MouseDownEvent evt)
        {
            Application.OpenURL("https://console.lootlocker.com");
            evt.StopPropagation();
        }

        private bool AtTarget(EventBase eventBase)
        {
#if UNITY_2023_1_OR_NEWER
            return eventBase.propagationPhase == PropagationPhase.BubbleUp;
#else
            return eventBase.propagationPhase == PropagationPhase.AtTarget;
#endif
        }

        private void OnEditorUpdate()
        {
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private void ShowPopup(string title, string message)
        {
            if (popupTitle != null) popupTitle.text = title;
            if (popupMessage != null) popupMessage.text = message;
            if (popup != null) popup.style.display = DisplayStyle.Flex;
        }

        private void ClosePopup(EventBase e)
        {
            if (popup != null) popup.style.display = DisplayStyle.None;
        }

        private void ShowLoadingAndExecute(Action action)
        {
            EditorApplication.update += OnEditorUpdate;
            if (loadingPage != null) loadingPage.style.display = DisplayStyle.Flex;
            action?.Invoke();
        }

        private void HideLoading()
        {
            EditorApplication.update -= OnEditorUpdate;
            if (loadingPage != null) loadingPage.style.display = DisplayStyle.None;
        }
    }
}
#endif
