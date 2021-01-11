using LootLocker.Admin.Requests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker.Admin
{
    public class AdminAuthTest : MonoBehaviour
    {
        public string email, password, mfa_key, secret;

        [ContextMenu("Initial Authentication Request")]
        public void InitialAuthenticationRequest()
        {
            LootLockerSDKAdminManager.InitialAuthRequest(email, password, (response) =>
            {
                if (response.success)
                {
                    Debug.LogError("Successful got admin auth response: " + response.text);
                    mfa_key = response.mfa_key;
                }
                else
                {
                    Debug.LogError("failed to get admin auth response: " + response.Error);
                }
            });
        }

        [ContextMenu("Two-Factor Authentication Code Verification")]
        public void TwoFactorAuthenticationCodeVerification()
        {
            LootLockerSDKAdminManager.TwoFactorAuthVerification(mfa_key, secret, (response) =>
            {
                if (response.success)
                {
                    Debug.Log("Successful got two-factor authentication code: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to get two-factor authentication code: " + response.Error);
                }
            });
        }

        [ContextMenu("Subsequent requests")]
        public void SubsequentRequests()
        {
            LootLockerSDKAdminManager.SubsequentRequestsRequest((response) =>
            {
                if (response.success)
                {
                    Debug.LogError("Successful got Subsequent requests: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to get Subsequent requests: " + response.Error);
                }
            });
        }

    }

}