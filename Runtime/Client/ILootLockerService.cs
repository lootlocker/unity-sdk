namespace LootLocker
{
    /// <summary>
    /// Interface that all LootLocker services must implement to be managed by the LifecycleManager
    /// </summary>
    public interface ILootLockerService
    {
        /// <summary>
        /// Initialize the service
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Reset/cleanup the service state
        /// </summary>
        void Reset();
        
        /// <summary>
        /// Handle application pause events (optional - default implementation does nothing)
        /// </summary>
        void HandleApplicationPause(bool pauseStatus);
        
        /// <summary>
        /// Handle application focus events (optional - default implementation does nothing)
        /// </summary>
        void HandleApplicationFocus(bool hasFocus);
        
        /// <summary>
        /// Handle application quit events
        /// </summary>
        void HandleApplicationQuit();
        
        /// <summary>
        /// Whether the service has been initialized
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Service name for logging and identification
        /// </summary>
        string ServiceName { get; }
    }
}