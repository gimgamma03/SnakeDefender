using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SnakeDefender
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private int totalEnemyCount;
        public int TotalEnemyCount => totalEnemyCount;

        [Header("승리·패배 이벤트")]
        [SerializeField] private UnityEvent onVictory;
        [SerializeField] private UnityEvent onDefeat;

        [Header("종료 시")]
        [Tooltip("승리 또는 패배 시 timeScale을 0으로 설정해 진행을 멈춤.")]
        [SerializeField] private bool pauseTimeOnGameEnd = true;

        [Header("시작")]
        [Tooltip("켜면 씬 로드 직후 timeScale 0. UI 버튼에서 BeginGameplay() 호출로 시작.")]
        [SerializeField] private bool pauseAtStart = true;
        [Tooltip("시작 버튼(또는 패널) 오브젝트. 씬에서는 비활성화해 두고, pauseAtStart일 때 플레이 진입 시 자동으로 켬.")]
        [SerializeField] private GameObject startScreenRoot;
        [SerializeField] private UnityEvent onGameplayStarted;

        [Header("테스트")]
        [Tooltip("에디터·빌드에서 ESC로 플레이 종료(빌드 테스트용).")]
        [SerializeField] private bool quitOnEscape = true;

        private int killedCount;
        private bool finished;

        /// <summary>몸통 파괴 진행(파괴율 UI 등) 갱신 시.</summary>
        public event Action OnBodyDestructionProgressChanged;

        /// <summary>시작 버튼 누른 뒤 true. pauseAtStart가 꺼져 있으면 Awake에서 곧바로 true.</summary>
        public bool HasGameplayBegun { get; private set; }

        /// <summary>이번 스테이지에서 파괴한 몸통 수.</summary>
        public int DestroyedBodyCount => killedCount;

        /// <summary>이번 스테이지 몸통 목표 수(EnemySpawner·프리팹과 동기).</summary>
        public int TotalBodyCount => totalEnemyCount;

        /// <summary>0~100. 몸통 파괴 진행률.</summary>
        public int DestructionRatePercent
        {
            get
            {
                if (totalEnemyCount <= 0)
                {
                    return 0;
                }

                return Mathf.Clamp(Mathf.RoundToInt(killedCount * 100f / totalEnemyCount), 0, 100);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (pauseAtStart)
            {
                Time.timeScale = 0f;
                HasGameplayBegun = false;
                if (startScreenRoot != null)
                {
                    startScreenRoot.SetActive(true);
                }
            }
            else
            {
                Time.timeScale = 1f;
                HasGameplayBegun = true;
            }
        }

        private void Update()
        {
            if (!quitOnEscape || !WasEscapePressedThisFrame())
            {
                return;
            }

            QuitFromEscape();
        }

        private static bool WasEscapePressedThisFrame()
        {
            // 빌드에서 Keyboard.current가 null인 경우가 있어 장치 조회로 보강
            Keyboard kb = Keyboard.current ?? InputSystem.GetDevice<Keyboard>();
            return kb != null && kb.escapeKey.wasPressedThisFrame;
        }

        private void QuitFromEscape()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            // timeScale 0이면 Quit이 기대대로 동작하지 않거나 창이 멈춘 것처럼 보이는 경우가 있음
            Time.timeScale = 1f;
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool IsGameFinished => finished;

        public void SetTotalEnemyCount(int count)
        {
            totalEnemyCount = Mathf.Max(0, count);
        }

        public void NotifyEnemyKilled(int score = 1)
        {
            if (finished)
            {
                return;
            }

            killedCount += Mathf.Max(1, score);
            OnBodyDestructionProgressChanged?.Invoke();

            if (killedCount >= totalEnemyCount)
            {
                finished = true;
                PauseGameplayIfNeeded();
                onVictory?.Invoke();
            }
        }

        public void NotifyEnemyReachedGoal()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            PauseGameplayIfNeeded();
            onDefeat?.Invoke();
        }

        public void NotifyPlayerDefeated()
        {
            NotifyEnemyReachedGoal();
        }

        // 시작 UI 버튼 OnClick에 연결.
        public void BeginGameplay()
        {
            if (HasGameplayBegun)
            {
                return;
            }

            HasGameplayBegun = true;
            Time.timeScale = 1f;
            onGameplayStarted?.Invoke();
        }

        private void PauseGameplayIfNeeded()
        {
            if (pauseTimeOnGameEnd)
            {
                Time.timeScale = 0f;
            }
        }

        // 재시작이나 메뉴 갈 때 부르면 됨. timeScale은 씬 넘어가도 안 풀릴 수 있어서 따로 리셋하는 거임.
        public static void ResetTimeScale()
        {
            Time.timeScale = 1f;
        }
    }
}
