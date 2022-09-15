using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests
{
    public class GuestLoginTest
    {
        [UnityTest]
        public IEnumerator GuestUserCanLogIn()
        {
            var guestLoginGO = new GameObject();
            var loginInformationGO = new GameObject();
            var playerIdGO = new GameObject();
            var loginInformationText = loginInformationGO.AddComponent<Text>();
            var playerIdText = playerIdGO.AddComponent<Text>();
            var guestLogin = guestLoginGO.AddComponent<GuestLogin>();
            guestLogin.loginInformationText = loginInformationText;
            guestLogin.playerIdText = playerIdText;
            yield return new WaitUntil(() => guestLogin.isDone());
            Assert.IsTrue(loginInformationText.text.Contains("Guest session started"));
            Assert.IsTrue(playerIdText.text.Contains("Player ID: "));
        }
    }
}