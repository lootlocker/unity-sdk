using LootLockerAdminRequests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLockerAdmin
{
    public class AdminUserTest : MonoBehaviour
    {

        [ContextMenu("Setup Two-Factor Authentication")]
        public void SetupTwoFactorAuthentication()
        {
            LootLockerSDKAdminManager.SetupTwoFactorAuthentication((response) =>
            {
                if (response.success)
                {
                    LootLockerSDKAdminManager.DebugMessage("Successful setup two factor authentication: " + response.text);
                }
                else
                {
                    LootLockerSDKAdminManager.DebugMessage("failed to set two factor authentication: " + response.Error, true);
                }
            });
        }

    }
}

