using LootLocker.Requests;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LootLocker.Example
{
    public class PurchaseTest : MonoBehaviour
    {
        public string asset_id = "0", variation_id = "0", rental_option_id = "0",
            receipt_data, purchase_token,
            labelText;

        private void OnGUI()
        {

            GUIStyle centeredTextStyle = new GUIStyle();
            centeredTextStyle.alignment = TextAnchor.MiddleCenter;

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Back", GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000)))
                UnityEngine.SceneManagement.SceneManager.LoadScene("NavigationScene");

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Asset ID");

            asset_id = GUILayout.TextField(asset_id, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));
            asset_id = Regex.Replace(asset_id, @"[^0-9 ]", "");

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Variation ID");

            variation_id = GUILayout.TextField(variation_id, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));
            variation_id = Regex.Replace(variation_id, @"[^0-9 ]", "");

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Rental Option ID");

            rental_option_id = GUILayout.TextField(rental_option_id, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));
            rental_option_id = Regex.Replace(rental_option_id, @"[^0-9 ]", "");

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Receipt Data");

            receipt_data = GUILayout.TextField(receipt_data, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));
            receipt_data = Regex.Replace(receipt_data, @"[^a-zA-Z0-9 ]", "");

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Purchase Token");

            purchase_token = GUILayout.TextField(purchase_token, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1000));
            purchase_token = Regex.Replace(purchase_token, @"[^a-zA-Z0-9 ]", "");

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Normal Purchase", GUILayout.ExpandWidth(true)))
            {
                NormalPurchaseCall();
            }

            if (GUILayout.Button("Rental Purchase", GUILayout.ExpandWidth(true)))
            {
                RentalPurchaseCall();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Verify iOS Purchase", GUILayout.ExpandWidth(true)))
            {
                IosPurchaseVerification();
            }

            if (GUILayout.Button("Verify Android Purchase", GUILayout.ExpandWidth(true)))
            {
                AndroidPurchaseVerification();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Poll Order Status", GUILayout.ExpandWidth(true)))
            {
                PollingOrderStatus();
            }

            if (GUILayout.Button("Activate Rental Asset", GUILayout.ExpandWidth(true)))
            {
                ActivatingARentalAsset();
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(labelText);

            GUILayout.EndVertical();

        }
        public void NormalPurchaseCall()
        {
            LootLockerSDKManager.NormalPurchaseCall(int.Parse(asset_id), int.Parse(variation_id), (response) =>
             {
                 if (response.success)
                 {
                     labelText = "Successful purchase : " + response.text;
                 }
                 else
                 {
                     labelText = "failed purchase : " + response.Error;
                 }
             });
        }

        public void RentalPurchaseCall()
        {
            LootLockerSDKManager.RentalPurchaseCall(int.Parse(asset_id), int.Parse(variation_id), int.Parse(rental_option_id), (response) =>
            {
                if (response.success)
                {
                    labelText = "Successful got assets" + response.text;
                }
                else
                {
                    labelText = "failed to get assets : " + response.Error;
                }
            });
        }

        public void IosPurchaseVerification()
        {
            LootLockerSDKManager.IosPurchaseVerification(receipt_data, (response) =>
            {
                if (response.success)
                {
                    labelText = "Successful verified purchase" + response.text;
                }
                else
                {
                    labelText = "failed to verify purchase : " + response.Error;
                }
            });
        }

        public void AndroidPurchaseVerification()
        {
            LootLockerSDKManager.AndroidPurchaseVerification(purchase_token, int.Parse(asset_id), (response) =>
            {
                if (response.success)
                {
                    labelText = "Successful verified purchase" + response.text;
                }
                else
                {
                    labelText = "failed to verify purchase : " + response.Error;
                }
            });
        }

        public void PollingOrderStatus()
        {
            LootLockerSDKManager.PollingOrderStatus(int.Parse(asset_id), (response) =>
            {
                if (response.success)
                {
                    labelText = "Successful polled order status" + response.text;
                }
                else
                {
                    labelText = "failed to poll order status : " + response.Error;
                }
            });
        }

        public void ActivatingARentalAsset()
        {
            LootLockerSDKManager.ActivatingARentalAsset(int.Parse(asset_id), (response) =>
            {
                if (response.success)
                {
                    labelText = "Successful activated a rental asset" + response.text;
                }
                else
                {
                    labelText = "failed to activate a rental asset : " + response.Error;
                }
            });
        }
    }
}