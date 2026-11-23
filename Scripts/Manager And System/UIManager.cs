using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HotteokSurvivor.Script.Manager_And_System
{
    public enum UIType
    {
        MainMenu,
        OverMenu,
        ClearMenu,
        LevelUpPopUp,
        GameUI
    }
    
    public class UIManager : MonoBehaviour
    {
        [System.Serializable]
        public struct UIPanelData
        {
            public UIType type;
            public GameObject panelObject;
        }
        
        [SerializeField] private List<UIPanelData> uiPanels;
        
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] clickClips;

        [SerializeField] private TMP_Text currentTimeText;
        [SerializeField] private TMP_Text endTimeText;
        [SerializeField] private TMP_Text endLevelText;
        public Slider expSlider;

        private void Update()
        {
            if (Time.timeScale == 0)
            {
                return;
            }

            currentTimeText.SetText($"{GameManager.Instance.CurrentTime:F1}");
            expSlider.value = (float)GameManager.Instance.Player.Stat.Exp / GameManager.Instance.Player.Stat.MaxExp;
        }

        public void SetPanelActive(UIType type, bool isActive)
        {
            // 리스트를 돌면서 일치하는 타입의 UI를 찾아서 켜고 끕니다.
            foreach (var panel in uiPanels)
            {
                if (panel.type == type)
                {
                    panel.panelObject.SetActive(isActive);
                    
                    if (panel.type == UIType.ClearMenu || panel.type == UIType.OverMenu)
                    {
                        endTimeText = panel.panelObject.transform.Find("Time Text").GetComponent<TMP_Text>();
                        endLevelText = panel.panelObject.transform.Find("Level Text").GetComponent<TMP_Text>();
                        
                        endTimeText.SetText($"Time: {GameManager.Instance.CurrentTime:F1}");
                        endLevelText.SetText($"Level: {GameManager.Instance.Player.Stat.Level}");
                    }
                    return;
                }
            }
        }
        
        public void OnClickButton(int soundNum)
        {
            audioSource.PlayOneShot(clickClips[soundNum]);
        }

        public void SelectSkills(int hotteokName)
        {
            GameManager gameManager = GameManager.Instance;

            HotteokRotate rotateHotteok = gameManager.Player.hotteokRotate;
            
            switch (hotteokName)
            {
                case (int)HotteokName.Normal:

                    gameManager.PoolingSystem.hotteokLevel[hotteokName]++;
                    break;

                case (int)HotteokName.Pat:

                    gameManager.PoolingSystem.hotteokLevel[hotteokName]++;

                    if (gameManager.PoolingSystem.haveHotteoksKind < hotteokName + 1)
                    {
                        gameManager.PoolingSystem.haveHotteoksKind++;
                    }
                    break;

                case (int)HotteokName.Ddalgi:

                    if (rotateHotteok.ActiveHotteoks < rotateHotteok.DdalgiHottoeks.Count)
                    {
                        rotateHotteok.ActiveHotteoks++;
                    }

                    rotateHotteok.gameObject.SetActive(true);
                    break;
            }

            Time.timeScale = 1;
        }

        public void SceneChange(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
            if (sceneName.Equals("Game Scene"))
            {
                GameManager.Instance.CurrentTime = 0.0f;
            }
            Time.timeScale = 1;
        }

        public void ExitGame()
        {
            Application.Quit();
        }
    }
}
