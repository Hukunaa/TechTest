using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Hero;

namespace Game
{
    public enum GameState
    {
        Idle,
        Running,
        Pause,
        RoundComplete,
        Dead
    }

    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameState _state = GameState.Idle;
        [SerializeField] private AssetReferenceGameObject _startingLevelPrefab;
        [SerializeField] private AssetReferenceGameObject _heroControllerPrefab;

        private HeroController _heroControllerInstance;
        private Level _currentLevel;

        private AsyncOperationHandle<GameObject> _levelInstanceHandle;
        private AsyncOperationHandle<GameObject> _heroInstanceHandle;

        private bool _hasLevelHandle;
        private bool _hasHeroHandle;
        private bool _isLoading;

        public HeroController HeroController => _heroControllerInstance;
        public static GameManager Instance { get; private set; }
        public GameState State => _state;
        public Level CurrentLevel => _currentLevel;
        public event Action<GameState> StateChanged;

        // Singleton setup + keep the manager alive across scene loads.
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Start initial content loading using Addressables.
        void Start()
        {
            StartCoroutine(LoadStartingContentRoutine());
        }

        // Release addressable instances when the manager is destroyed.
        void OnDestroy()
        {
            if (Instance == this)
            {
                ReleaseLevelInstance();
                ReleaseHeroInstance();
                Instance = null;
            }
        }

        // Start the game only once both hero and level have been loaded/instantiated.
        public void StartLevel()
        {
            if (_heroControllerInstance == null || _currentLevel == null)
                return;

            SetRunning();
        }

        public void CompleteLevel()
        {
            SetRoundComplete();
        }

        public void LoseLevel()
        {
            SetDead();
        }

        // Recreate the current level while keeping the hero instance.
        public void RestartLevel()
        {
            if (_isLoading) return;
            StartCoroutine(RestartLevelRoutine());
        }

        // Central state setter that broadcasts changes to listeners.
        public void SetState(GameState state)
        {
            if (_state == state) return;

            _state = state;
            StateChanged?.Invoke(_state);
        }

        public void SetIdle()
        {
            SetState(GameState.Idle);
            DisablePlayer();
        }

        public void SetRunning()
        {
            SetState(GameState.Running);
            EnablePlayer();
        }

        public void SetPause()
        {
            SetState(GameState.Pause);
            DisablePlayer();
        }

        public void SetRoundComplete()
        {
            SetState(GameState.RoundComplete);
            DisablePlayer();
        }

        public void SetDead()
        {
            SetState(GameState.Dead);
            DisablePlayer();
        }

        public void TogglePause()
        {
            if (_state == GameState.Pause) SetRunning();
            else if (_state == GameState.Running) SetPause();
        }

        public void DisablePlayer()
        {
            _heroControllerInstance?.Disable();
        }

        public void EnablePlayer()
        {
            _heroControllerInstance?.Enable();
        }

        // Load the starting level + hero once at boot, then reset hero position and move to Idle.
        private IEnumerator LoadStartingContentRoutine()
        {
            if (_isLoading) yield break;
            _isLoading = true;

            yield return LoadLevelRoutine();
            yield return LoadHeroRoutine();

            if (_heroControllerInstance != null)
                _heroControllerInstance.ResetPositionToSpawn();

            SetIdle();

            _isLoading = false;
        }

        // Restart flow: destroy/release the level instance, instantiate a new one, then reset hero and go Idle.
        private IEnumerator RestartLevelRoutine()
        {
            _isLoading = true;

            ReleaseLevelInstance();
            _currentLevel = null;

            yield return LoadLevelRoutine();

            if (_heroControllerInstance != null)
                _heroControllerInstance.ResetPositionToSpawn();

            SetIdle();

            _isLoading = false;
        }

        // Instantiate the level prefab through Addressables and cache the Level component reference.
        private IEnumerator LoadLevelRoutine()
        {
            if (_startingLevelPrefab == null || !_startingLevelPrefab.RuntimeKeyIsValid())
            {
                Debug.LogError("GameManager: Starting level addressable prefab reference is missing or invalid.", this);
                yield break;
            }

            _levelInstanceHandle = _startingLevelPrefab.InstantiateAsync();
            _hasLevelHandle = true;

            yield return _levelInstanceHandle;

            if (_levelInstanceHandle.Status != AsyncOperationStatus.Succeeded || _levelInstanceHandle.Result == null)
            {
                Debug.LogError("GameManager: Failed to instantiate starting level via Addressables.", this);
                ReleaseLevelInstance();
                yield break;
            }

            _currentLevel = _levelInstanceHandle.Result.GetComponent<Level>();
            if (_currentLevel == null)
                Debug.LogError("GameManager: Instantiated level prefab does not contain a Level component.", this);
        }

        // Instantiate the hero prefab through Addressables and cache the HeroController component reference.
        private IEnumerator LoadHeroRoutine()
        {
            if (_heroControllerInstance != null) yield break;

            if (_heroControllerPrefab == null || !_heroControllerPrefab.RuntimeKeyIsValid())
            {
                Debug.LogError("GameManager: Hero addressable prefab reference is missing or invalid.", this);
                yield break;
            }

            _heroInstanceHandle = _heroControllerPrefab.InstantiateAsync();
            _hasHeroHandle = true;

            yield return _heroInstanceHandle;

            if (_heroInstanceHandle.Status != AsyncOperationStatus.Succeeded || _heroInstanceHandle.Result == null)
            {
                Debug.LogError("GameManager: Failed to instantiate hero via Addressables.", this);
                ReleaseHeroInstance();
                yield break;
            }

            _heroControllerInstance = _heroInstanceHandle.Result.GetComponent<HeroController>();
            if (_heroControllerInstance == null)
                Debug.LogError("GameManager: Instantiated hero prefab does not contain a HeroController component.", this);
        }

        // Release the instantiated level instance handle back to Addressables.
        private void ReleaseLevelInstance()
        {
            if (!_hasLevelHandle) return;

            if (_levelInstanceHandle.IsValid())
                Addressables.ReleaseInstance(_levelInstanceHandle);

            _hasLevelHandle = false;
        }

        // Release the instantiated hero instance handle back to Addressables.
        private void ReleaseHeroInstance()
        {
            if (!_hasHeroHandle) return;

            if (_heroInstanceHandle.IsValid())
                Addressables.ReleaseInstance(_heroInstanceHandle);

            _hasHeroHandle = false;
        }
    }
}
