using UnityEngine;

namespace LootLocker
{
    /// <summary>
    /// Implement this interface to create a custom persistent state writer for saving and loading player state data.
    /// 
    /// The default implementation uses Unity's PlayerPrefs. If that is something you do not want then you can implement this interface and set your implementation using LootLockerSDKManager.SetStateWriter
    /// The requirements are:
    ///   - The implementation can save and load strings to persistent storage that is retrievable across game sessions.
    ///   - The implementation must be thread safe.
    ///   - The implementation must be performant enough to not cause noticeable hitches in the game when saving or loading data.
    ///   - The implementation must be able to consistently retrieve the data that was saved from the provided key
    /// </summary>
    public interface ILootLockerStateWriter
    {
        /// <summary>
        /// Get a string from persistent storage. If the key does not exist then return the provided default value.
        /// </summary>
        /// <param name="key">The key to retrieve the value for.</param>
        /// <param name="defaultValue">The value to return if the key does not exist.</param>
        /// <returns>The value associated with the key, or the default value if the key does not exist.</returns>
        string GetString(string key, string defaultValue = "");
        /// <summary>
        /// Set a string in persistent storage.
        /// </summary>
        /// <param name="key">The key to set the value for.</param>
        /// <param name="value">The value to set.</param>
        void SetString(string key, string value);

        /// <summary>
        /// Delete a key from persistent storage.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        void DeleteKey(string key);
        /// <summary>
        /// Check if a key exists in persistent storage.
        /// </summary>
        /// <param name="key">The key to check for existence.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        bool HasKey(string key);
    }

    public class LootLockerPlayerPrefsStateWriter : ILootLockerStateWriter
    {
        /// <summary>
        /// Deletes a key from PlayerPrefs and saves the changes.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        public void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Gets a string from PlayerPrefs.
        /// </summary>
        /// <param name="key">The key to retrieve the value for.</param>
        /// <param name="defaultValue">The value to return if the key does not exist.</param>
        /// <returns>The value associated with the key, or the default value if the key does not exist.</returns>
        public string GetString(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        /// <summary>
        /// Checks if a key exists in PlayerPrefs.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        /// <summary>
        /// Sets a string in PlayerPrefs and saves the changes.
        /// </summary>
        /// <param name="key">The key to set the value for.</param>
        /// <param name="value">The value to set.</param>
        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }
    }

    public class LootLockerNullStateWriter : ILootLockerStateWriter
    {
        public void DeleteKey(string key)
        {
            // Do nothing
        }

        public string GetString(string key, string defaultValue = "")
        {
            return defaultValue;
        }

        public bool HasKey(string key)
        {
            return false;
        }

        public void SetString(string key, string value)
        {
            // Do nothing
        }
    }
}
