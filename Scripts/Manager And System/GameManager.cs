using Study.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HotteokSurvivor.Script.Manager_And_System
{
    public class GameManager : SingletonBase<GameManager>
    {
        public Player Player;
        public GameObject Canvas;
        public PoolingSystem PoolingSystem;

        public float CurrentTime;

        private void Start()
        {
            DontDestroyOnLoad(Canvas);
        }

        private void Update()
        {
            if(SceneManager.GetActiveScene().name=="Game Scene")
            {
                CurrentTime += Time.deltaTime;
            }

            // PoolingSystem이 이미 파괴됐다면 클리어 처리를 끝낸 것이므로 다시 실행하지 않음
            if (CurrentTime >= 100.0f && PoolingSystem != null)
            {
                Canvas.GetComponent<UIManager>().SetPanelActive(UIType.ClearMenu, true);
                Destroy(PoolingSystem.gameObject);
                Time.timeScale = 0;
            }
        }

        public void LevelUpPopUp()
        {
            Canvas.GetComponent<UIManager>().SetPanelActive(UIType.LevelUpPopUp, true);
            Time.timeScale = 0;
        }
    }
}