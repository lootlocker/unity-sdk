using LootLocker.Requests;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace LootLocker
{
    public class PlayerFiles : MonoBehaviour
    {
        public Text informationText;
        public Text availablePlayerFilesText;
        public InputField playerFilesTextContent;
        public Text playerFilesTextPlaceholder;

        // Enable this to save the file on the disk as well as uploading it to LootLocker
        public bool saveOnDisk;

        public InputField fileIdUploadInput;
        public InputField fileIdDownloadInput;

        // Start is called before the first frame update
        void Start()
        {
            /* 
            * Override settings to use the Example game setup
            */
            LootLockerSettingsOverrider.OverrideSettings();

            /* Start guest session without an identifier.
             * LootLocker will create an identifier for the user and store it in PlayerPrefs.
             * If you want to create a new player when testing, you can use PlayerPrefs.DeleteKey("LootLockerGuestPlayerID");
             */
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                if (response.success)
                {
                    informationText.text = "Guest session started" + "\n";
                    GetPlayerFiles();
                }
                else
                {
                    informationText.text = "Error" + response.errorData.message + "\n";
                }
            });
        }

        public void GetPlayerFiles()
        {
            // Get a list of the files that the current player has
            LootLockerSDKManager.GetAllPlayerFiles((response) =>
            {
                if (response.success)
                {
                    informationText.text += "Got player files" + "\n";
                    availablePlayerFilesText.text = "Available files: \n";
                    for (int i = 0; i < response.items.Length; i++)
                    {
                        // Write name and ID of the files to the UI
                        availablePlayerFilesText.text += "Name:"+response.items[i].name+"\n";
                        availablePlayerFilesText.text += "ID:" + response.items[i].id + "\n";
                        availablePlayerFilesText.text += "Purpose:" + response.items[i].purpose + "\n";
                        availablePlayerFilesText.text += "Size:" + response.items[i].size + "\n";
                        availablePlayerFilesText.text += "------------------" + "\n";
                    }
                }
                else
                {
                    informationText.text += "Error" + response.errorData.message + "\n";
                }
            });
        }

        public void GetPlayerFileContent()
        {
            // You get the URL of the file from LootLocker
            // You need to download the file yourself from LootLocker.
            // See the IEnumerator Download() method below for an example on how to do it
            LootLockerSDKManager.GetPlayerFile(int.Parse(fileIdDownloadInput.text), (response) =>
            {
                if (response.success)
                {
                    informationText.text += "Got player file" + "\n";
                    fileIdUploadInput.text = response.id.ToString();
                    StartCoroutine(Download(response.url, (fileContent) =>
                    {
                        informationText.text += "Got player file content" + "\n";
                        playerFilesTextContent.text = "";
                        playerFilesTextPlaceholder.text = fileContent;
                    }));
                }
                else
                {
                    informationText.text += "Error" + response.errorData.message + "\n";
                }
            });
        }

        // Used for downloading files from a URL
        IEnumerator Download(string url, System.Action<string> fileContent)
        {
            UnityWebRequest www = new UnityWebRequest(url);
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();
            // Unity 2020.1 and newer does not use isNetworkError and isHttpError anymore
#if UNITY_2020_1_OR_NEWER
                if (www.result != UnityWebRequest.Result.Success)
#else
                if (www.isNetworkError || www.isHttpError)
#endif
                {
                    Debug.Log(www.error);
                }
                else
                {
                    // Show results as text
                Debug.Log(www.downloadHandler.text);
                fileContent(www.downloadHandler.text);
            }
        }

        public void UploadOrUpdateFile()
        {
            if (fileIdUploadInput.text == "" || fileIdUploadInput.text == "0")
            {
                NewPlayerFile();
            }
            else
            {
                UpdateFile(int.Parse(fileIdUploadInput.text));
            }
        }

        public void NewPlayerFile()
        {
            if (saveOnDisk)
            {
                // Save the file to disk and upload that to LootLocker as a file
                WriteToFile("lootlocker.txt", playerFilesTextContent.text);
                LootLockerSDKManager.UploadPlayerFile("lootlocker.txt", "testFile", (response) =>
                {
                    if (response.success)
                    {
                        informationText.text += "Uploaded player file as a file" + "\n";

                    }
                    else
                    {
                        informationText.text += "Error" + response.errorData.message + "\n";
                    }
                    GetPlayerFiles();
                });
            }
            else
            {
                // Do not save anything on disk and upload the file to LootLocker as a byte-array
                byte[] fileByteArray = Encoding.UTF8.GetBytes(playerFilesTextContent.text);
                LootLockerSDKManager.UploadPlayerFile(fileByteArray, "lootlocker.txt", "testFile", (response) =>
                {
                    if (response.success)
                    {
                        informationText.text += "Uploaded player file as a stream" + "\n";
                    }
                    else
                    {
                        informationText.text += "Error" + response.errorData.message + "\n";
                    }
                    GetPlayerFiles();
                });
            }
        }

        void UpdateFile(int fileID)
        {
            // Updating a file requires the id of the file,
            // you get the id of the file when you retrieve all files
            // and when you upload a new file.
            // This will overwrite the current file.
            // 5 revisions are stored for each file on LootLocker that you can rollback in the web console
            // https://console.lootlocker.com
            if (saveOnDisk == true)
            {
                // Save as a file and update it on LootLocker as well
                WriteToFile("lootlocker.txt", playerFilesTextContent.text);
                LootLockerSDKManager.UpdatePlayerFile(fileID, "lootlocker.txt", (response) =>
                {
                    if (response.success)
                    {
                        informationText.text += "Updated file!" + "\n";
                    }
                    else
                    {
                        informationText.text += "Error" + response.errorData.message + "\n";
                    }
                    GetPlayerFiles();
                });
            }
            else
            {
                // Save as a byte array to not store anything on disk, only in memory
                // Can be good in some cases in WebGL where you are sometimes not allowed to store data for example
                byte[] fileByteArray = Encoding.UTF8.GetBytes(playerFilesTextContent.text);
                LootLockerSDKManager.UpdatePlayerFile(fileID, fileByteArray, (response) =>
                {
                    if (response.success)
                    {
                        informationText.text += "Updated file!" + "\n";
                    }
                    else
                    {
                        informationText.text += "Error" + response.errorData.message + "\n";
                    }
                    GetPlayerFiles();
                });
            }
        }

        public void DeleteFile()
        {
            // Deleting a file is permanent, and it can not be reverted.
            // Make sure to implement the necessary steps so that your players don't do this by accident

            LootLockerSDKManager.DeletePlayerFile(int.Parse(fileIdUploadInput.text), (response) =>
            {
                if (response.success)
                {
                    informationText.text += "Deleted player file"+"\n";
                }
                else
                {
                    informationText.text += "Error" + response.errorData.message + "\n";
                }
            });
        }

        public static string WriteToFile(string fileName, string content)
        {
            string path = Application.persistentDataPath + "/" + fileName;
            StreamWriter writer = new StreamWriter(path, false);
            writer.WriteLine(content);
            writer.Close();
            Debug.Log(content);
            return path;
        }
        public static string ReadFromFile(string fileName)
        {
            string path = Application.persistentDataPath + "/" + fileName;
            StreamReader reader = new StreamReader(path);
            string content = reader.ReadToEnd();
            reader.Close();
            Debug.Log(content);
            return content;
        }
    }
}
