using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    /// <summary>
    /// Lifecycle state of the LifecycleManager
    /// </summary>
    public enum LifecycleManagerState
    {
        /// <summary>
        /// Normal operation - services can be accessed and managed
        /// </summary>
        Ready,
        
        /// <summary>
        /// Currently initializing services - prevent circular GetService calls
        /// </summary>
        Initializing,
        
        /// <summary>
        /// Currently resetting services - prevent circular reset calls
        /// </summary>
        Resetting,
        
        /// <summary>
        /// Application is shutting down - prevent new service access
        /// </summary>
        Quitting
    }

    /// <summary>
    /// Centralized lifecycle manager for all LootLocker services that need Unity GameObject management.
    /// Handles the creation of a single GameObject and coordinates Unity lifecycle events across all services.
    /// </summary>
    public class LootLockerLifecycleManager : MonoBehaviour
    {
        #region Instance Handling

        private static LootLockerLifecycleManager _instance;
        private static int _instanceId = 0;
        private static GameObject _hostingGameObject = null;
        private static readonly object _instanceLock = new object();

        /// <summary>
        /// Automatically initialize the lifecycle manager when the application starts.
        /// This ensures all services are ready before any game code runs.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (_instance == null && Application.isPlaying)
            {
                LootLockerLogger.Log("Auto-initializing LootLocker LifecycleManager on application start", LootLockerLogger.LogLevel.Debug);
                // Access the Instance property to trigger lazy initialization
                _ = Instance;
            }
        }

        /// <summary>
        /// Get or create the lifecycle manager instance
        /// </summary>
        public static LootLockerLifecycleManager Instance
        {
            get
            {
                if (_state == LifecycleManagerState.Quitting)
                {
                    LootLockerLogger.Log("Cannot access LifecycleManager during application shutdown", LootLockerLogger.LogLevel.Warning);
                    return null;
                }
                
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null && _state != LifecycleManagerState.Quitting)
                        {
                            Instantiate();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Check if the lifecycle manager is ready and initialized
        /// </summary>
        public static bool IsReady => _instance != null && _instance._isInitialized;

        private static void Instantiate()
        {
            if (_instance != null) return;

            LootLockerLogger.Log("Creating LootLocker LifecycleManager GameObject and initializing services", LootLockerLogger.LogLevel.Debug);
            
            var gameObject = new GameObject("LootLockerLifecycleManager");
            _instance = gameObject.AddComponent<LootLockerLifecycleManager>();
            _instanceId = _instance.GetInstanceID();
            _hostingGameObject = gameObject;

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            // Clean up any old instances
            _instance.StartCoroutine(CleanUpOldInstances());
            
            // Register and initialize all services immediately
            _instance._RegisterAndInitializeAllServices();
            
            LootLockerLogger.Log("LootLocker LifecycleManager initialization complete", LootLockerLogger.LogLevel.Debug);
        }

        public static IEnumerator CleanUpOldInstances()
        {
#if UNITY_2020_1_OR_NEWER
            LootLockerLifecycleManager[] managers = GameObject.FindObjectsByType<LootLockerLifecycleManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            LootLockerLifecycleManager[] managers = GameObject.FindObjectsOfType<LootLockerLifecycleManager>();
#endif
            foreach (LootLockerLifecycleManager manager in managers)
            {
                if (manager != null && _instanceId != manager.GetInstanceID() && manager.gameObject != null)
                {
#if UNITY_EDITOR
                    DestroyImmediate(manager.gameObject);
#else
                    Destroy(manager.gameObject);
#endif
                }
            }
            yield return null;
        }

        public static void ResetInstance()
        {
            lock (_instanceLock)
            {
                _state = LifecycleManagerState.Quitting; // Mark as quitting to prevent new access
                
                if (_instance != null)
                {
                    _instance.ResetAllServices();
                    
#if UNITY_EDITOR
                    if (_instance.gameObject != null)
                        DestroyImmediate(_instance.gameObject);
#else
                    if (_instance.gameObject != null)
                        Destroy(_instance.gameObject);
#endif
                    
                    _instance = null;
                    _instanceId = 0;
                    _hostingGameObject = null;
                }
                
                // Reset state for clean restart
                _state = LifecycleManagerState.Ready;
            }
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(UnityEditor.EnterPlayModeOptions options)
        {
            _state = LifecycleManagerState.Ready; // Reset state when entering play mode
            ResetInstance();
        }
#endif

        #endregion

        #region Service Management

        private readonly Dictionary<Type, ILootLockerService> _services = new Dictionary<Type, ILootLockerService>();
        private readonly List<ILootLockerService> _initializationOrder = new List<ILootLockerService>();
        private readonly List<Type> _serviceInitializationOrder = new List<Type>
        {
            // Define the initialization order here
            typeof(RateLimiter), // Rate limiter first (used by HTTP client)
            typeof(LootLockerHTTPClient),         // HTTP client second
            typeof(LootLockerEventSystem),       // Events system third
#if LOOTLOCKER_ENABLE_PRESENCE
            typeof(LootLockerPresenceManager)     // Presence manager last (depends on HTTP)
#endif
        };
        private bool _isInitialized = false;
        private bool _serviceHealthMonitoringEnabled = true;
        private Coroutine _healthMonitorCoroutine = null;
        private static LifecycleManagerState _state = LifecycleManagerState.Ready;
        private readonly object _serviceLock = new object();

        /// <summary>
        /// Register a service to be managed by the lifecycle manager.
        /// Service is immediately initialized upon registration.
        /// </summary>
        public static void RegisterService<T>(T service) where T : class, ILootLockerService
        {
            var instance = Instance;
            instance._RegisterServiceAndInitialize(service);
        }

        /// <summary>
        /// Create and register a MonoBehaviour service component to be managed by the lifecycle manager.
        /// Service is immediately initialized upon registration.
        /// </summary>
        public static T RegisterService<T>() where T : MonoBehaviour, ILootLockerService
        {
            var instance = Instance;
            var service = instance.gameObject.AddComponent<T>();
            instance._RegisterServiceAndInitialize(service);
            return service;
        }

        /// <summary>
        /// Get a service. The LifecycleManager auto-initializes on first access if needed.
        /// </summary>
        public static T GetService<T>() where T : class, ILootLockerService
        {
            if (_state == LifecycleManagerState.Quitting || _state == LifecycleManagerState.Resetting)
            {
                LootLockerLogger.Log($"Access of service {typeof(T).Name} during {_state.ToString().ToLower()} was requested but denied", LootLockerLogger.LogLevel.Debug);
                return null;
            }
            
            // CRITICAL: Prevent circular dependency during initialization
            if (_state == LifecycleManagerState.Initializing)
            {
                LootLockerLogger.Log($"Service {typeof(T).Name} requested during LifecycleManager initialization - this could cause deadlock. Returning null.", LootLockerLogger.LogLevel.Warning);
                return null;
            }
            
            var instance = Instance; // This will trigger auto-initialization if needed
            if (instance == null)
            {
                LootLockerLogger.Log($"Cannot access service {typeof(T).Name} - LifecycleManager is not available", LootLockerLogger.LogLevel.Warning);
                return null;
            }
            
            var service = instance._GetService<T>();
            if (service == null)
            {
                throw new InvalidOperationException($"Service {typeof(T).Name} is not registered. This indicates a bug in service registration.");
            }
            return service;
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public static bool HasService<T>() where T : class, ILootLockerService
        {
            if (_state == LifecycleManagerState.Quitting || _state == LifecycleManagerState.Resetting || _instance == null)
            {
                return false;
            }
            
            // Allow HasService checks during initialization (safe, read-only)
            var instance = _instance ?? Instance;
            if (instance == null)
            {
                return false;
            }
            
            return instance._HasService<T>();
        }

        /// <summary>
        /// Unregister and cleanup a service from the lifecycle manager
        /// </summary>
        public static void UnregisterService<T>() where T : class, ILootLockerService
        {
            if (_state != LifecycleManagerState.Ready || _instance == null)
            {
                // Don't allow unregistration during shutdown/reset/initialization to prevent circular dependencies
                LootLockerLogger.Log($"Ignoring unregister request for {typeof(T).Name} during {_state.ToString().ToLower()}", LootLockerLogger.LogLevel.Debug);
                return;
            }
            
            var instance = Instance;
            if (instance == null)
            {
                return;
            }
            
            instance._UnregisterService<T>();
        }

        /// <summary>
        /// Reset a specific service without unregistering it
        /// </summary>
        public static void ResetService<T>() where T : class, ILootLockerService
        {
            if (_state != LifecycleManagerState.Ready || _instance == null)
            {
                LootLockerLogger.Log($"Ignoring reset request for {typeof(T).Name} during {_state.ToString().ToLower()}", LootLockerLogger.LogLevel.Debug);
                return;
            }
            
            var instance = Instance;
            if (instance == null)
            {
                return;
            }
            
            instance._ResetService<T>();
        }

        /// <summary>
        /// Get all registered services
        /// </summary>
        public static IEnumerable<ILootLockerService> GetAllServices()
        {
            if (_state == LifecycleManagerState.Quitting || _instance == null)
            {
                return new List<ILootLockerService>();
            }
            
            var instance = Instance;
            if (instance == null)
            {
                return new List<ILootLockerService>();
            }
            
            lock (instance._serviceLock)
            {
                // Return a copy to avoid modification during iteration
                return new List<ILootLockerService>(instance._services.Values);
            }
        }

        /// <summary>
        /// Register all services and initialize them immediately in the defined order.
        /// This replaces the previous split approach of separate register and initialize phases.
        /// </summary>
        private void _RegisterAndInitializeAllServices()
        {
            lock (_serviceLock)
            {
                if (_isInitialized)
                {
                    LootLockerLogger.Log("Services already registered and initialized", LootLockerLogger.LogLevel.Debug);
                    return;
                }

                _state = LifecycleManagerState.Initializing; // Set state to prevent circular GetService calls
                
                try
                {
                    LootLockerLogger.Log("Registering and initializing all services...", LootLockerLogger.LogLevel.Debug);

                    // Register and initialize core services in defined order with dependency injection
                    
                    // 1. Initialize RateLimiter first (no dependencies)
                    var rateLimiter = _RegisterAndInitializeService<RateLimiter>();
                    
                    // 2. Initialize EventSystem (no dependencies)  
                    var eventSystem = _RegisterAndInitializeService<LootLockerEventSystem>();
                    
                    // 3. Initialize StateData (no dependencies)
                    var stateData = _RegisterAndInitializeService<LootLockerStateData>();
                    
                    // 4. Initialize HTTPClient and set RateLimiter dependency
                    var httpClient = _RegisterAndInitializeService<LootLockerHTTPClient>();
                    httpClient.SetRateLimiter(rateLimiter);
                    
                    // 5. Set up StateData event subscriptions after both services are ready
                    stateData.SetEventSystem(eventSystem);
                    
#if LOOTLOCKER_ENABLE_PRESENCE
                    // 6. Initialize PresenceManager (no special dependencies)
                    _RegisterAndInitializeService<LootLockerPresenceManager>();
#endif

                    // Note: RemoteSessionPoller is registered on-demand only when needed

                    _isInitialized = true;
                    
                    // Change state to Ready before finishing initialization
                    _state = LifecycleManagerState.Ready;
                    
                    // Start service health monitoring
                    if (_serviceHealthMonitoringEnabled && Application.isPlaying)
                    {
                        _healthMonitorCoroutine = StartCoroutine(ServiceHealthMonitor());
                    }
                    
                    LootLockerLogger.Log("LifecycleManager initialization complete", LootLockerLogger.LogLevel.Debug);
                }
                finally
                {
                    // State is already set to Ready above, only set to Error if we had an exception
                    if (_state == LifecycleManagerState.Initializing)
                    {
                        _state = LifecycleManagerState.Ready; // Fallback in case of unexpected path
                    }
                }
            }
        }

        /// <summary>
        /// Register and immediately initialize a specific MonoBehaviour service
        /// </summary>
        private T _RegisterAndInitializeService<T>() where T : MonoBehaviour, ILootLockerService
        {
            if (_HasService<T>())
            {
                LootLockerLogger.Log($"Service {typeof(T).Name} already registered", LootLockerLogger.LogLevel.Debug);
                return _GetService<T>();
            }

            var service = gameObject.AddComponent<T>();
            _RegisterServiceAndInitialize(service);
            return service;
        }

        /// <summary>
        /// Register and immediately initialize a service (for external registration)
        /// </summary>
        private void _RegisterServiceAndInitialize<T>(T service) where T : class, ILootLockerService
        {
            if (service == null)
            {
                LootLockerLogger.Log($"Cannot register null service of type {typeof(T).Name}", LootLockerLogger.LogLevel.Warning);
                return;
            }

            var serviceType = typeof(T);
            
            lock (_serviceLock)
            {
                if (_services.ContainsKey(serviceType))
                {
                    LootLockerLogger.Log($"Service {service.ServiceName} of type {serviceType.Name} is already registered", LootLockerLogger.LogLevel.Warning);
                    return;
                }

                _services[serviceType] = service;
                
                LootLockerLogger.Log($"Registered service: {service.ServiceName}", LootLockerLogger.LogLevel.Debug);

                // Always initialize immediately upon registration
                try
                {
                    LootLockerLogger.Log($"Initializing service: {service.ServiceName}", LootLockerLogger.LogLevel.Debug);
                    service.Initialize();
                    _initializationOrder.Add(service);
                    LootLockerLogger.Log($"Successfully initialized service: {service.ServiceName}", LootLockerLogger.LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    LootLockerLogger.Log($"Failed to initialize service {service.ServiceName}: {ex.Message}", LootLockerLogger.LogLevel.Warning);
                }
            }
        }

        private T _GetService<T>() where T : class, ILootLockerService
        {
            lock (_serviceLock)
            {
                _services.TryGetValue(typeof(T), out var service);
                return service as T;
            }
        }

        private bool _HasService<T>() where T : class, ILootLockerService
        {
            lock (_serviceLock)
            {
                return _services.ContainsKey(typeof(T));
            }
        }

        private void _UnregisterService<T>() where T : class, ILootLockerService
        {
            if(!_HasService<T>())
            {
                LootLockerLogger.Log($"Service of type {typeof(T).Name} is not registered, cannot unregister", LootLockerLogger.LogLevel.Warning);
                return;
            }
            lock (_serviceLock)
            {
                var serviceType = typeof(T);
                if (_services.TryGetValue(serviceType, out var service))
                {
                    LootLockerLogger.Log($"Unregistering service: {service.ServiceName}", LootLockerLogger.LogLevel.Debug);

                    try
                    {
                        // Reset the service
                        service.Reset();

                        // Remove from initialization order if present
                        _initializationOrder.Remove(service);

                        // Remove from services dictionary
                        _services.Remove(serviceType);

                        // Destroy the component if it's a MonoBehaviour
                        if (service is MonoBehaviour component)
                        {
#if UNITY_EDITOR
                            DestroyImmediate(component);
#else
                            Destroy(component);
#endif
                        }

                        LootLockerLogger.Log($"Successfully unregistered service: {service.ServiceName}", LootLockerLogger.LogLevel.Debug);
                    }
                    catch (Exception ex)
                    {
                        LootLockerLogger.Log($"Error unregistering service {service.ServiceName}: {ex.Message}", LootLockerLogger.LogLevel.Warning);
                    }
                }
            }
        }

        private void _ResetService<T>() where T : class, ILootLockerService
        {
            if (!_HasService<T>())
            {
                LootLockerLogger.Log($"Service of type {typeof(T).Name} is not registered, cannot reset", LootLockerLogger.LogLevel.Warning);
                return;
            }

            lock (_serviceLock)
            {
                var serviceType = typeof(T);
                if (_services.TryGetValue(serviceType, out var service))
                {
                    if (service == null)
                    {
                        LootLockerLogger.Log($"Service {typeof(T).Name} reference is null, cannot reset", LootLockerLogger.LogLevel.Warning);
                        return;
                    }

                    _ResetSingleService(service);
                }
            }
        }

        /// <summary>
        /// Reset a single service with proper logging and error handling
        /// </summary>
        private void _ResetSingleService(ILootLockerService service)
        {
            if (service == null) return;
            
            try
            {
                LootLockerLogger.Log($"Resetting service: {service.ServiceName}", LootLockerLogger.LogLevel.Debug);
                
                service.Reset();
                
                LootLockerLogger.Log($"Successfully reset service: {service.ServiceName}", LootLockerLogger.LogLevel.Debug);
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Error resetting service {service.ServiceName}: {ex.Message}", LootLockerLogger.LogLevel.Warning);
            }
        }

        #endregion

        #region Unity Lifecycle Events

        private void OnApplicationPause(bool pauseStatus)
        {
            lock (_serviceLock)
            {
                foreach (var service in _services.Values)
                {
                    if (service == null) continue; // Defensive null check
                    try
                    {
                        service.HandleApplicationPause(pauseStatus);
                    }
                    catch (Exception ex)
                    {
                        LootLockerLogger.Log($"Error in OnApplicationPause for service {service.ServiceName}: {ex.Message}", LootLockerLogger.LogLevel.Warning);
                    }
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            lock (_serviceLock)
            {
                foreach (var service in _services.Values)
                {
                    if (service == null) continue; // Defensive null check
                    try
                    {
                        service.HandleApplicationFocus(hasFocus);
                    }
                    catch (Exception ex)
                    {
                        LootLockerLogger.Log($"Error in OnApplicationFocus for service {service.ServiceName}: {ex.Message}", LootLockerLogger.LogLevel.Warning);
                    }
                }
            }
        }

        private void OnApplicationQuit()
        {
            if (_state == LifecycleManagerState.Quitting) return; // Prevent multiple calls
            
            _state = LifecycleManagerState.Quitting;
            LootLockerLogger.Log("Application is quitting, notifying services and marking lifecycle manager for shutdown", LootLockerLogger.LogLevel.Debug);
            
            // Create a snapshot of services to avoid collection modification during iteration
            ILootLockerService[] serviceSnapshot;
            lock (_serviceLock)
            {
                serviceSnapshot = new ILootLockerService[_services.Values.Count];
                _services.Values.CopyTo(serviceSnapshot, 0);
            }
            
            // Notify all services that the application is quitting (without holding the lock)
            foreach (var service in serviceSnapshot)
            {
                if (service == null) continue; // Defensive null check
                try
                {
                    service.HandleApplicationQuit();
                }
                catch (Exception ex)
                {
                    LootLockerLogger.Log($"Error notifying service {service.ServiceName} of application quit: {ex.Message}", LootLockerLogger.LogLevel.Warning);
                }
            }
        }

        private void OnDestroy()
        {
            ResetAllServices();
        }

        private void ResetAllServices()
        {
            if (_state == LifecycleManagerState.Resetting) return; // Prevent circular reset calls
            
            lock (_serviceLock)
            {
                _state = LifecycleManagerState.Resetting; // Set state to prevent circular dependencies
                
                try
                {
                    // Stop health monitoring during reset
                    if (_healthMonitorCoroutine != null)
                    {
                        StopCoroutine(_healthMonitorCoroutine);
                        _healthMonitorCoroutine = null;
                    }
                    
                    LootLockerLogger.Log("Resetting all services...", LootLockerLogger.LogLevel.Debug);

                    // Reset services in reverse order of initialization
                    // This ensures dependencies are torn down in the correct order
                    for (int i = _initializationOrder.Count - 1; i >= 0; i--)
                    {
                        var service = _initializationOrder[i];
                        if (service == null) continue; // Defensive null check
                        
                        // Reuse the common reset logic
                        _ResetSingleService(service);
                    }

                    // Clear the service collections after all resets are complete
                    _services.Clear();
                    _initializationOrder.Clear();
                    _isInitialized = false;
                    
                    LootLockerLogger.Log("All services reset and collections cleared", LootLockerLogger.LogLevel.Debug);
                }
                finally
                {
                    _state = LifecycleManagerState.Ready; // Always reset the state
                }
            }
        }

        /// <summary>
        /// Service health monitoring coroutine - checks service health and restarts failed services
        /// </summary>
        private IEnumerator ServiceHealthMonitor()
        {
            const float healthCheckInterval = 30.0f; // Check every 30 seconds
            
            while (_serviceHealthMonitoringEnabled && Application.isPlaying)
            {
                yield return new WaitForSeconds(healthCheckInterval);
                
                if (_state != LifecycleManagerState.Ready)
                {
                    continue; // Skip health checks during initialization/reset
                }

                lock (_serviceLock)
                {
                    // Check each service health
                    var servicesToRestart = new List<Type>();
                    
                    foreach (var serviceEntry in _services)
                    {
                        var serviceType = serviceEntry.Key;
                        var service = serviceEntry.Value;
                        
                        if (service == null)
                        {
                            LootLockerLogger.Log($"Service {serviceType.Name} is null - marking for restart", LootLockerLogger.LogLevel.Warning);
                            servicesToRestart.Add(serviceType);
                            continue;
                        }
                        
                        try
                        {
                            // Check if service is still initialized
                            if (!service.IsInitialized)
                            {
                                LootLockerLogger.Log($"Service {service.ServiceName} is no longer initialized - attempting restart", LootLockerLogger.LogLevel.Warning);
                                servicesToRestart.Add(serviceType);
                            }
                        }
                        catch (Exception ex)
                        {
                            LootLockerLogger.Log($"Error checking health of service {serviceType.Name}: {ex.Message} - marking for restart", LootLockerLogger.LogLevel.Warning);
                            servicesToRestart.Add(serviceType);
                        }
                    }
                    
                    // Restart failed services
                    foreach (var serviceType in servicesToRestart)
                    {
                        _RestartService(serviceType);
                    }
                }
            }
        }

        /// <summary>
        /// Restart a specific service that has failed
        /// </summary>
        private void _RestartService(Type serviceType)
        {
            if (_state != LifecycleManagerState.Ready)
            {
                return;
            }

            try
            {
                LootLockerLogger.Log($"Attempting to restart failed service: {serviceType.Name}", LootLockerLogger.LogLevel.Warning);
                
                // Remove the failed service
                if (_services.ContainsKey(serviceType))
                {
                    var failedService = _services[serviceType];
                    if (failedService != null)
                    {
                        _initializationOrder.Remove(failedService);
                        
                        // Clean up the failed service if it's a MonoBehaviour
                        if (failedService is MonoBehaviour component)
                        {
#if UNITY_EDITOR
                            DestroyImmediate(component);
#else
                            Destroy(component);
#endif
                        }
                    }
                    _services.Remove(serviceType);
                }
                
                // Recreate and reinitialize the service based on its type
                if (serviceType == typeof(RateLimiter))
                {
                    _RegisterAndInitializeService<RateLimiter>();
                }
                else if (serviceType == typeof(LootLockerHTTPClient))
                {
                    var rateLimiter = _GetService<RateLimiter>();
                    var httpClient = _RegisterAndInitializeService<LootLockerHTTPClient>();
                    httpClient.SetRateLimiter(rateLimiter);
                }
                else if (serviceType == typeof(LootLockerEventSystem))
                {
                    var eventSystem = _RegisterAndInitializeService<LootLockerEventSystem>();
                    // Re-establish StateData event subscriptions if both services exist
                    var stateData = _GetService<LootLockerStateData>();
                    if (stateData != null)
                    {
                        stateData.SetEventSystem(eventSystem);
                    }
                }
                else if (serviceType == typeof(LootLockerStateData))
                {
                    var stateData = _RegisterAndInitializeService<LootLockerStateData>();
                    // Set up event subscriptions if EventSystem exists
                    var eventSystem = _GetService<LootLockerEventSystem>();
                    if (eventSystem != null)
                    {
                        stateData.SetEventSystem(eventSystem);
                    }
                }
#if LOOTLOCKER_ENABLE_PRESENCE
                else if (serviceType == typeof(LootLockerPresenceManager))
                {
                    _RegisterAndInitializeService<LootLockerPresenceManager>();
                }
#endif
                
                LootLockerLogger.Log($"Successfully restarted service: {serviceType.Name}", LootLockerLogger.LogLevel.Info);
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Failed to restart service {serviceType.Name}: {ex.Message}", LootLockerLogger.LogLevel.Warning);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether the lifecycle manager is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Number of registered services
        /// </summary>
        public int ServiceCount 
        { 
            get 
            { 
                lock (_serviceLock) 
                { 
                    return _services.Count; 
                } 
            } 
        }

        /// <summary>
        /// Get the hosting GameObject
        /// </summary>
        public GameObject GameObject => _hostingGameObject;

        /// <summary>
        /// Current lifecycle state of the manager
        /// </summary>
        public static LifecycleManagerState CurrentState => _state;

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get service initialization status for debugging
        /// </summary>
        public static Dictionary<string, bool> GetServiceStatuses()
        {
            var statuses = new Dictionary<string, bool>();
            
            if (_instance != null)
            {
                lock (_instance._serviceLock)
                {
                    foreach (var service in _instance._services.Values)
                    {
                        statuses[service.ServiceName] = service.IsInitialized;
                    }
                }
            }

            return statuses;
        }

        /// <summary>
        /// Reset a specific service by its type. This is useful for clearing state without unregistering the service.
        /// Example: LootLockerLifecycleManager.ResetService&lt;LootLockerHTTPClient&gt;();
        /// </summary>
        /// <typeparam name="T">The service type to reset</typeparam>
        public static void ResetServiceByType<T>() where T : class, ILootLockerService
        {
            ResetService<T>();
        }

        /// <summary>
        /// Enable or disable service health monitoring
        /// </summary>
        /// <param name="enabled">Whether to enable health monitoring</param>
        public static void SetServiceHealthMonitoring(bool enabled)
        {
            if (_instance != null)
            {
                _instance._serviceHealthMonitoringEnabled = enabled;
                
                if (enabled && _instance._healthMonitorCoroutine == null && Application.isPlaying)
                {
                    _instance._healthMonitorCoroutine = _instance.StartCoroutine(_instance.ServiceHealthMonitor());
                }
                else if (!enabled && _instance._healthMonitorCoroutine != null)
                {
                    _instance.StopCoroutine(_instance._healthMonitorCoroutine);
                    _instance._healthMonitorCoroutine = null;
                }
            }
        }

        /// <summary>
        /// Check if service health monitoring is enabled
        /// </summary>
        public static bool IsServiceHealthMonitoringEnabled => _instance?._serviceHealthMonitoringEnabled ?? false;

        #endregion
    }
}