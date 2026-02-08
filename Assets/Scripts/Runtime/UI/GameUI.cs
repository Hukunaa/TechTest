using UnityEngine;
using Game;

namespace UI
{
    public class GameUI : MonoBehaviour
    {
        [SerializeField] private GameObject _gameStartPanel;
        [SerializeField] private GameObject _gameCompletePanel;
        [SerializeField] private GameObject _gameOverPanel;

        void Start()
        {
            GameManager.Instance.StateChanged += OnGameStateChanged;
            ToggleGameStartPanel(true);
        }

        void OnDestroy()
        {
            if(GameManager.Instance != null)
                GameManager.Instance.StateChanged -= OnGameStateChanged;
        }

        void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Idle:
                    ToggleGameStartPanel(true);
                    ToggleGameCompletePanel(false);
                    ToggleGameDeadPanel(false);
                    break;
                case GameState.Running:
                    ToggleGameStartPanel(false);
                    break;
                case GameState.RoundComplete:
                    ToggleGameCompletePanel(true);
                    break;
                case GameState.Dead:
                    ToggleGameDeadPanel(true);
                    break;
            }
        }

        public void ToggleGameStartPanel(bool show)
        {
            if (_gameStartPanel != null)
            {
                _gameStartPanel.SetActive(show);
            }
        }

        public void ToggleGameDeadPanel(bool show)
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(show);
            }
        }

        public void ToggleGameCompletePanel(bool show)
        {
            if (_gameCompletePanel != null)
            {
                _gameCompletePanel.SetActive(show);
            }
        }

        public void OnStartPressed()
        {
            GameManager.Instance.StartLevel();
        }

        public void OnRestartPressed()
        {
            GameManager.Instance.RestartLevel();
        }
    }
}
