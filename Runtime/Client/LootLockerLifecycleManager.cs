using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LootLocker
{
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
                Instantiate();
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
                    return null;
                }
                
                if (_instance == null)
                {
                    Instantiate();
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

            _state = LifecycleManagerState.Initializing;
            
            lock (_instanceLock)
            {                
                var gameObject = new GameObject("LootLockerLifecycleManager");
                _instance = gameObject.AddComponent<LootLockerLifecycleManager>();
                _instanceId = _instance.GetInstanceID();
                _hostingGameObject = gameObject;

                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }

                _instance.StartCoroutine(CleanUpOldInstances());
                _instance._RegisterAndInitializeAllServices();
            }
            _state = LifecycleManagerState.Ready;
        }

        private static void TeardownInstance() 
        {
            if(_instance == null) return;
            if(_state == LifecycleManagerState.Quitting) return;
            lock (_instanceLock)
            {
                _state = LifecycleManagerState.Quitting;
                
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
            }
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
            TeardownInstance();

            Instantiate();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(UnityEditor.EnterPlayModeOptions options)
        {
            TeardownInstance();
        }
#endif

        #endregion

        #region Service Management

        private readonly Dictionary<Type, ILootLockerService> _services = new Dictionary<Type, ILootLockerService>();
        private readonly List<ILootLockerService> _initializationOrder = new List<ILootLockerService>();
        private bool _isInitialized = false;
        private bool _serviceHealthMonitoringEnabled = true;
        private Coroutine _healthMonitorCoroutine = null;
        private static LifecycleManagerState _state = LifecycleManagerState.Ready;
        private readonly object _serviceLock = new object();

        /// <summary>
        /// Create and register a MonoBehaviour service component to be managed by the lifecycle manager.
        /// Service is immediately initialized upon registration.
        /// </summary>
        public static T RegisterService<T>() where T : MonoBehaviour, ILootLockerService
        {
            var instance = Instance;
            if (instance == null)
            {
                return null;
            }
            return instance._RegisterAndInitializeService<T>();
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
                LootLockerLogger.Log($"Service {typeof(T).Name} requested during LifecycleManager initialization - this could cause deadlock. Returning null.", LootLockerLogger.LogLevel.Info);
                return null;
            }
            
            var instance = Instance;
            if (instance == null)
            {
                LootLockerLogger.Log($"Cannot access service {typeof(T).Name} - LifecycleManager is not available", LootLockerLogger.LogLevel.Warning);
                return null;
            }
            
            var service = instance._GetService<T>();
            if (service == null)
            {
                LootLockerLogger.Log($"Service {typeof(T).Name} is not registered. This indicates a bug in service registration.", LootLockerLogger.LogLevel.Warning);
                return null;
            }
            return service;
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public static bool HasService<T>() where T : class, ILootLockerService
        {
            if (_state != LifecycleManagerState.Ready || _instance == null)
            {
                return false;
            }
            
            return _instance._HasService<T>();
        }

        /// <summary>
        /// Unregister and cleanup a service from the lifecycle manager
        /// </summary>
        public static void UnregisterService<T>() where T : class, ILootLockerService
        {
            if (_state != LifecycleManagerState.Ready || _instance == null)
            {
                LootLockerLogger.Log($"Ignoring unregister request for {typeof(T).Name} during {_state.ToString().ToLower()}", LootLockerLogger.LogLevel.Debug);
                return;
            }
            
            _instance._UnregisterService<T>();
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
                    return;
                }

                _state = LifecycleManagerState.Initializing; // Set state to prevent circular GetService calls
                
                try
                {

                    // Register and initialize core services in defined order with dependency injection
                    
                    // 1. Initialize RateLimiter first (no dependencies)
                    var rateLimiter = _RegisterAndInitializeService<RateLimiter>();
                    
                    // 2. Initialize EventSystem (no dependencies)  
                    var eventSystem = _RegisterAndInitializeService<LootLockerEventSystem>();
                    
                    // 3. Initialize StateData (no dependencies)
                    var stateData = _RegisterAndInitializeService<LootLockerStateData>();
                    if (eventSystem != null) 
                    {
                        stateData.SetEventSystem(eventSystem);
                    }
                    
                    // 4. Initialize HTTPClient and set RateLimiter dependency
                    var httpClient = _RegisterAndInitializeService<LootLockerHTTPClient>();
                    httpClient.SetRateLimiter(rateLimiter);
                    
                    // 5. Initialize PresenceManager (no special dependencies)
                    var presenceManager = _RegisterAndInitializeService<LootLockerPresenceManager>();
                    if (eventSystem != null)
                    {
                        presenceManager.SetEventSystem(eventSystem);
                    }

                    _isInitialized = true;
                    
                    // Change state to Ready before finishing initialization
                    _state = LifecycleManagerState.Ready;
                    
                    // Start service health monitoring
                    if (_serviceHealthMonitoringEnabled && Application.isPlaying)
                    {
                        _healthMonitorCoroutine = StartCoroutine(ServiceHealthMonitor());
                    }
                    
                    LootLockerLogger.Log($"LifecycleManager initialization complete. Services registered: {string.Join(", ", _initializationOrder.Select(s => s.ServiceName))}", LootLockerLogger.LogLevel.Debug);
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
                return _GetService<T>();
            }

            T service = null;
            
            lock (_serviceLock)
            {
                service = gameObject.AddComponent<T>();
                
                if (service == null)
                {
                    return null;
                }

                _services[typeof(T)] = service;

                try
                {
                    service.Initialize();
                    _initializationOrder.Add(service);
                }
                catch (Exception ex)
                {
                    LootLockerLogger.Log($"Failed to initialize service {service.ServiceName}: {ex.Message}", LootLockerLogger.LogLevel.Warning);
                }
            }
            return service;
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
                return;
            }
            T service = null;
            lock (_serviceLock)
            {
                _services.TryGetValue(typeof(T), out var svc);
                if(svc == null)
                {
                    return;
                }
                service = svc as T;

                // Remove from initialization order if present
                _initializationOrder.Remove(service);

                // Remove from services dictionary
                _services.Remove(typeof(T));
            }
            
            _ResetService(service);
        }
        
        private void _ResetService(ILootLockerService service)
        {
            if (service == null) return;
            
            try
            {
                service.Reset();

                // Destroy the component if it's a MonoBehaviour
                if (service is MonoBehaviour component)
                {
#if UNITY_EDITOR
                    DestroyImmediate(component);
#else
                    Destroy(component);
#endif
                }
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
                    if (service == null) continue;
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
                    if (service == null) continue;
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

            TeardownInstance();
            
            ILootLockerService[] serviceSnapshot;
            lock (_serviceLock)
            {
                serviceSnapshot = new ILootLockerService[_services.Values.Count];
                _services.Values.CopyTo(serviceSnapshot, 0);
            }
            
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
            TeardownInstance();
        }

        private void ResetAllServices()
        {
            // Stop health monitoring during reset
            if (_healthMonitorCoroutine != null)
            {
                StopCoroutine(_healthMonitorCoroutine);
                _healthMonitorCoroutine = null;
            }

            // Reset services in reverse order of initialization
            // This ensures dependencies are torn down in the correct order
            ILootLockerService[] servicesSnapshot;
            // Create a snapshot of services to avoid collection modification during iteration
            lock (_serviceLock)
            {
                servicesSnapshot = new ILootLockerService[_initializationOrder.Count];
                _initializationOrder.CopyTo(servicesSnapshot, 0);
                Array.Reverse(servicesSnapshot);
            }

            foreach (var service in servicesSnapshot)
            {
                if (service == null) continue;
                
                _ResetService(service);
            }

            // Clear the service collections after all resets are complete
            _services.Clear();
            _initializationOrder.Clear();
            _isInitialized = false;
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
                            servicesToRestart.Add(serviceType);
                            continue;
                        }
                        
                        try
                        {
                            // Check if service is still initialized
                            if (!service.IsInitialized)
                            {
                                servicesToRestart.Add(serviceType);
                            }
                        }
                        catch (Exception)
                        {
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

            if (!_services.ContainsKey(serviceType))
            {
                return; // Service not registered
            }

            _ResetService(_services[serviceType]);

            try
            {                
                // Recreate and reinitialize the service based on its type
                if (serviceType == typeof(RateLimiter))
                {
                    _RegisterAndInitializeService<RateLimiter>();
                }
                else if (serviceType == typeof(LootLockerHTTPClient))
                {
                    var httpClient = _RegisterAndInitializeService<LootLockerHTTPClient>();
                    httpClient.SetRateLimiter(_GetService<RateLimiter>());
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
                    var presenceManager = _GetService<LootLockerPresenceManager>();
                    if (presenceManager != null)
                    {
                        presenceManager.SetEventSystem(eventSystem);
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
                else if (serviceType == typeof(LootLockerPresenceManager))
                {
                    var presenceManager = _RegisterAndInitializeService<LootLockerPresenceManager>();
                    var eventSystem = _GetService<LootLockerEventSystem>();
                    if (eventSystem != null)
                    {
                        presenceManager.SetEventSystem(eventSystem);
                    }
                }
                
                LootLockerLogger.Log($"Successfully restarted service: {serviceType.Name}", LootLockerLogger.LogLevel.Info);
            }
            catch (Exception ex)
            {
                LootLockerLogger.Log($"Failed to restart service {serviceType.Name}: {ex.Message}", LootLockerLogger.LogLevel.Warning);
            }
        }

        #endregion
    }
}
