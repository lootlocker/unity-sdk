using LootLocker;
using System;
using System.Collections.Generic;
using UnityEngine;
using static LootLockerTestConfigurationUtils.Auth;

namespace LootLockerTestConfigurationUtils
{
    public class LootLockerTestUser
    {
        #region Statics
        private static LootLockerTestUser _activeUser = null;
        public static bool isSignedIn => _activeUser != null;
        private static bool isSignInInProgress = false;

        private static List<Action<bool /*success*/, string /*errorMessage*/, LootLockerTestUser /*User*/>> listeners = new List<Action<bool, string, LootLockerTestUser>>();

        private static void InvokeListenersAndClear(bool success, string errorMessage)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Debug.LogWarning(errorMessage);
            }

            foreach (var listener in listeners)
            {
                listener?.Invoke(success, errorMessage, _activeUser);
            }
            listeners.Clear();
        }

        public static void GetUserOrSignIn(Action<bool /*success*/, string /*errorMessage*/, LootLockerTestUser /*User*/> signInComplete)
        {
            // We already have an active user locally, reuse it
            if (_activeUser != null)
            {
                signInComplete?.Invoke(true, "", _activeUser);
                return;
            }

            listeners.Add(signInComplete);

            // A sign in process is already in progress, simply listen for that result (this is to maybe lazy deal with concurrency)
            if (isSignInInProgress && signInComplete != null)
            {
                return;
            }

            // We are the primary sign in process
            isSignInInProgress = true;

            // Generate the user info
            var userDate = DateTime.Now.ToString("yyyy-MM-dd-HH") + "h";
            var userName = "testrun+" + userDate;
            var password = userName;
            var userEmail = "unity+ci-" + userName + "@lootlocker.com";

            bool isTargetingProduction = LootLockerConfig.IsTargetingProductionEnvironment();
            if (isTargetingProduction)
            {
                // Don't create new users in Production
                GetProductionUser(out userEmail, out password);
            }

            if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(password))
            {
                InvokeListenersAndClear(false, "No account information supplied");
                isSignInInProgress = false;
                return;
            }

            // Try to log in, if the user exists great, if not then signup and log in
            Login(userEmail, password, loginResponse =>
            {
                if (loginResponse.success)
                {
                    FinalizeSignIn(loginResponse.user, loginResponse.auth_token, userName, userEmail, password, userDate);
                    return;
                }

                bool needToSignUp = loginResponse.statusCode == 401;
                if (!needToSignUp)
                {
                    InvokeListenersAndClear(false, loginResponse.errorData?.ToString());
                    isSignInInProgress = false;
                    return;
                }

                Signup(userEmail, password, userName, userName, signupResponse =>
                {
                    if (!signupResponse.success)
                    {
                        InvokeListenersAndClear(false, signupResponse.errorData?.ToString());
                        isSignInInProgress = false;
                        return;
                    }

                    LootLockerConfig.current.adminToken = null;

                    Login(userEmail, password, loginResponseAfterSignup =>
                    {
                        if (!loginResponseAfterSignup.success)
                        {
                            InvokeListenersAndClear(false, loginResponseAfterSignup.errorData?.ToString());
                            isSignInInProgress = false;
                            return;
                        }

                        FinalizeSignIn(loginResponseAfterSignup.user, loginResponseAfterSignup.auth_token, userName, userEmail, password, userDate);
                    });
                });
            });
        }

        private static void GetProductionUser(out string userName, out string password)
        {
            //NOTE: Set these if you want to run towards production locally
            userName = "";
            password = "";
#if LOOTLOCKER_COMMANDLINE_SETTINGS
                string[] args = System.Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-adminemail")
                    {
                        userName = args[i + 1];
                    }
                    else if (args[i] == "-adminpassword")
                    {
                        password = args[i + 1];
                    }
                }
#endif
        }

        private static void FinalizeSignIn(LootLockerTestUser user, string token, string userName, string userEmail, string password, string userDate)
        {

            _activeUser = user;
            _activeUser.auth_token = token;
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                LootLockerConfig.current.adminToken = token;
            }
            _activeUser.userName = userName;
            _activeUser.userEmail = userEmail;
            _activeUser.userPassword = password;
            _activeUser.userDate = userDate;
            Debug.Log("Signed in as " + user + " with password \"" + password + "\"");
            InvokeListenersAndClear(true, "");
            isSignInInProgress = false;
        }

        #endregion

        #region Fields

        public int id { get; set; }
        public string name { get; set; }
        public long signed_up { get; set; }
        public Organisation[] organisations { get; set; }

        public string auth_token { get; set; }
        public string userName { get; set; }
        public string userEmail { get; set; }
        public string userPassword { get; set; }
        public string userDate { get; set; }

        public override string ToString()
        {
            return "User with email "+ userEmail +", id " + id + ", and name " + name;
        }

        public class Organisation
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        #endregion
    }
}
