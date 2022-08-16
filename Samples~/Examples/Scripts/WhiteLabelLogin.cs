using UnityEngine;
using System;
using UnityEngine.UI;
using LootLocker.Requests;

public class WhiteLabelLogin : MonoBehaviour
{
    // Input fields
    [Header("New User")]
    public InputField newUserEmailInputField;
    public InputField newUserPasswordInputField;

    [Header("Existing User")]
    public InputField existingUserEmailInputField;
    public InputField existingUserPasswordInputField;

    public Text infoText;

    private void Awake()
    {
        LootLockerSettingsOverrider.OverrideSettings();
    }

    // Called when pressing "Log in"
    public void Login()
    {
        string email = existingUserEmailInputField.text;
        string password = existingUserPasswordInputField.text;
        LootLockerSDKManager.WhiteLabelLogin(email, password, false, loginResponse =>
        {
            if (!loginResponse.success)
            {
                // Error
                infoText.text = "Error logging in:" + loginResponse.Error;
                return;
            }
            else
            {
                infoText.text = "Player was logged in succesfully";
            }

            // Is the account verified?
            if (loginResponse.VerifiedAt == null)
            {
                // Stop here if you want to require your players to verify their email before continuing,
                // verification must also be enabled on the LootLocker dashboard
            }

            // Player is logged in, now start a game session
            LootLockerSDKManager.StartWhiteLabelSession((startSessionResponse) =>
            {
                if (startSessionResponse.success)
                {
                    // Session was succesfully started;
                    // After this you can use LootLocker features
                    infoText.text = "Session started successfully";
                }
                else
                {
                    // Error
                    infoText.text = "Error starting LootLocker session:" + startSessionResponse.Error;
                }
            });
        });
    }

    // Called when pressing "Create account"
    public void CreateAccount()
    {
        string email = newUserEmailInputField.text;
        string password = newUserPasswordInputField.text;

        LootLockerSDKManager.WhiteLabelSignUp(email, password, (response) =>
        {
            if (!response.success)
            {
                infoText.text = "Error signing up:"+response.Error;
                return;
            }
            else
            {
                // Succesful response
                infoText.text = "Account created";
            }
        });
    }

    public void ResendVerificationEmail()
    {
        // Player ID can be retrieved when starting a session or creating an account.
        int playerID = 0;
        // Request a verification email to be sent to the registered user, 
        LootLockerSDKManager.WhiteLabelRequestVerification(playerID, (response) =>
        {
            if(response.success)
            {
                Debug.Log("Verification email sent!");
            }
            else
            {
                Debug.Log("Error sending verification email:" + response.Error);
            }

        });
    }

    public void SendResetPassword()
    {
        // Sends a password reset-link to the email
        LootLockerSDKManager.WhiteLabelRequestPassword("email@email-provider.com", (response) =>
        {
            if(response.success)
            {
                Debug.Log("Password reset link sent!");
            }
            else
            {
                Debug.Log("Error sending password-reset-link:" + response.Error);
            }
        });
    }
}
