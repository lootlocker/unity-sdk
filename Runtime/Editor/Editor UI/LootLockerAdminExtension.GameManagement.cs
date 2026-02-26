using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER && !LOOTLOCKER_DISABLE_EDITOR_EXTENSION
using LootLocker.Extension.Responses;

namespace LootLocker.Extension
{
    public partial class LootLockerAdminExtension : EditorWindow
    {
        public void CreateGameButtons()
        {
            if (gameSelectorList != null) gameSelectorList.Clear();

            var action = new Action(() =>
            {
                foreach (var game in gameData.Values)
                {
                    GameButtonTemplate(game, game.organisation_name);
                }
            });

            if (gameDataRefreshTime == null || 
                DateTime.Now >= gameDataRefreshTime.AddMinutes(gameDataCacheExpirationTimeMinutes) || 
                LootLockerEditorData.IsNewSession())
            {
                RefreshUserInformation(action);
            }
            else
            {
                action();
            }
        }

        public void GameButtonTemplate(LootLocker.Extension.DataTypes.Game game, string orgName)
        {
            Button button = new Button();
            button.style.flexDirection = FlexDirection.Column;
            button.name = game.id.ToString();
            button.AddToClassList("gameButton");

            Label gameTitle = new Label();
            gameTitle.text = game.name;
            gameTitle.name = "GameTitle";
            gameTitle.AddToClassList("gameButtonTitle");
            button.Add(gameTitle);

            Label gameOrg = new Label();
            gameOrg.text = orgName;
            gameOrg.name = "OrgTitle";
            gameOrg.AddToClassList("gameButtonOrgTitle");
            button.Add(gameOrg);

            button.clickable.clickedWithEventInfo += OnGameSelected;
            
            if (gameSelectorList != null) gameSelectorList.Add(button);
        }

        void APIKeyTemplate(KeyResponse key)
        {
            // Only show game api keys
            if (key.api_type != "game") return;
            Button button = new Button();
            bool isLegacyKey = !(key.api_key.StartsWith("prod_") || key.api_key.StartsWith("dev_"));
            button.name = key.api_key;
            
            Label keyName = new Label();
            keyName.text = string.IsNullOrEmpty(key.name) ? "Unnamed API Key" : key.name;
            
            if (button.name == LootLockerConfig.current.apiKey)
            {
                button.style.borderRightColor = button.style.borderLeftColor = 
                    button.style.borderTopColor = button.style.borderBottomColor = new Color(0.094f, 0.749f, 0.352f, 1);
                button.style.backgroundColor = new StyleColor(new Color(0.2f, 0.5f, 0.2f, 1f));
            }
            
            if (isLegacyKey)
            {
                keyName.text = "Legacy key: " + keyName.text;
                button.AddToClassList("legacyApikey");
            }
            else
            {
                button.AddToClassList("apikey");
                button.clickable.clickedWithEventInfo += OnAPIKeySelected;
                button.tooltip = "Click to select this API key.";
            }
            
            keyName.AddToClassList("apikeyName");
            button.Add(keyName);
            
            if (apiKeyList != null) apiKeyList.Add(button);
        }

        void SwapNewSelectedKey()
        {
            if (apiKeyList == null) return;
            
            foreach (var element in apiKeyList.Children())
            {
                var key = element as Button;
                if (key == null) continue;
                
                if (key.name == LootLockerConfig.current.apiKey)
                {
                    key.style.borderRightColor = key.style.borderLeftColor = 
                        key.style.borderTopColor = key.style.borderBottomColor = new Color(0.094f, 0.749f, 0.352f, 1);
                    key.style.backgroundColor = new StyleColor(new Color(0.2f, 0.5f, 0.2f, 1f));
                }
                else
                {
                    key.style.backgroundColor = defaultButton;
                    key.style.borderRightColor = key.style.borderLeftColor = 
                        key.style.borderTopColor = key.style.borderBottomColor = defaultButton;
                }
            }
        }

        public void UpdateLicenseCountdownUI()
        {
            if (licenseCountdownContainer == null || licenseCountdownLabel == null || licenseCountdownIcon == null)
                return;

            int selectedGameId = LootLockerEditorData.GetSelectedGame();
            if (!gameData.ContainsKey(selectedGameId))
            {
                licenseCountdownContainer.style.display = DisplayStyle.None;
                return;
            }

            var game = gameData[selectedGameId];
            string trialEndDate = game.trial_end_date;
            if (string.IsNullOrEmpty(trialEndDate))
            {
                licenseCountdownContainer.style.display = DisplayStyle.None;
                return;
            }

            if (!System.DateTime.TryParse(trialEndDate, out var endDate))
            {
                licenseCountdownContainer.style.display = DisplayStyle.None;
                return;
            }

            var now = System.DateTime.UtcNow;
            int daysLeft = (endDate.Date - now.Date).Days;

            UpdateLicenseCountdownDisplay(daysLeft, endDate < now);
        }

        private void UpdateLicenseCountdownDisplay(int daysLeft, bool isExpired)
        {
            if (isExpired || daysLeft == 0)
            {
                string labelText = isExpired ? "Trial Expired" : "Trial ends today";
                SetLicenseCountdownExpired(labelText);
            }
            else if (daysLeft > 0 && daysLeft <= 10)
            {
                SetLicenseCountdownWarning(daysLeft);
            }
            else
            {
                licenseCountdownContainer.style.display = DisplayStyle.None;
            }
        }

        private void SetLicenseCountdownExpired(string labelText)
        {
            licenseCountdownLabel.text = labelText;
            licenseCountdownLabel.RemoveFromClassList("licenseCountdownLabel");
            licenseCountdownLabel.RemoveFromClassList("expired");
            licenseCountdownLabel.AddToClassList("licenseCountdownLabel");
            licenseCountdownLabel.AddToClassList("expired");
            licenseCountdownLabel.style.color = new Color(1f, 0.31f, 0.31f);
            licenseCountdownLabel.style.display = DisplayStyle.Flex;
            licenseCountdownIcon.image = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
            licenseCountdownIcon.style.display = DisplayStyle.Flex;
            licenseCountdownContainer.style.display = DisplayStyle.Flex;
            licenseCountdownContainer.tooltip = "Click to manage your license";
            licenseCountdownContainer.RegisterCallback<MouseDownEvent>(LicenseCountdownContainerClick);
        }

        private void SetLicenseCountdownWarning(int daysLeft)
        {
            licenseCountdownLabel.text = $"Trial ends in {daysLeft} days";
            licenseCountdownLabel.RemoveFromClassList("expired");
            licenseCountdownLabel.AddToClassList("licenseCountdownLabel");
            licenseCountdownLabel.style.display = DisplayStyle.Flex;
            licenseCountdownIcon.image = EditorGUIUtility.IconContent("_Help").image as Texture2D;
            licenseCountdownIcon.style.display = DisplayStyle.Flex;
            licenseCountdownContainer.style.display = DisplayStyle.Flex;
            licenseCountdownContainer.tooltip = "Learn more about your license";
            licenseCountdownContainer.UnregisterCallback<MouseDownEvent>(LicenseCountdownContainerClick);
            licenseCountdownIcon.RegisterCallback<MouseDownEvent>(LicenseCountdownIconClick);
        }
    }
}
#endif
