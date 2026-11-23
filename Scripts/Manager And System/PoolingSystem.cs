using System.Collections;
using System.Collections.Generic;
using Study.Utilities;
using UnityEngine;

namespace HotteokSurvivor.Script.Manager_And_System
{
    public class PoolingSystem : SingletonBase<PoolingSystem>
    {
        public int enemyCount;
        public float xReplaceDistance;
        public float yReplaceDistance;

        private Vector3 screen;
        private Vector3 worldPos;

        public int haveHotteoksKind;
        public int[] hotteokLevel;

        [SerializeField] private float[] hotteokCooldowns;

        [SerializeField] private Transform hotteokMuzzle;
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private Transform moneyParent;

        [SerializeField] private Enemy[] enemise;
        [SerializeField] private Hotteok[] hotteoks;
        [SerializeField] private Money money;

        private ComponentPool<Enemy>[] enemisePool;
        private ComponentPool<Hotteok>[] hotteoksPool;
        public ComponentPool<Money> MoneyPool;

        public List<Enemy> enemiseList = new List<Enemy>();
        private Dictionary<HotteokName, List<Hotteok>> hotteokDic = new Dictionary<HotteokName, List<Hotteok>>();
        
        // 각 호떡 타입별로 실행 중인 코루틴을 추적하는 Dictionary
        // Key: 호떡 이름 (HotteokName enum), Value: 실행 중인 코루틴
        // 목적: 같은 코루틴이 중복 실행되지 않도록 관리
        private Dictionary<HotteokName, Coroutine> hotteokSpawnCoroutines = new Dictionary<HotteokName, Coroutine>();

        private void Start()
        {
            GameManager.Instance.PoolingSystem = this;

            // GridManager는 씬이 바뀌어도 남아 있으므로, 이전 판의 적/돈 기록을 비우고 시작
            GridManager.Instance.Grid.Clear();

            enemisePool = new ComponentPool<Enemy>[enemise.Length];
            hotteoksPool = new ComponentPool<Hotteok>[hotteoks.Length];

            for (int i = 0; i < enemise.Length; i++)
            {
                enemisePool[i] = new ComponentPool<Enemy>(enemise[i]);

                AddEnemyList(i, enemyCount);
            }

            for (int i = 0; i < hotteoks.Length; i++)
            {
                hotteoksPool[i] = new ComponentPool<Hotteok>(hotteoks[i], hotteokMuzzle);
            }

            MoneyPool = new ComponentPool<Money>(money, moneyParent);
        }

        private void Update()
        {
            screen = new Vector3(Screen.width / 2, 0);
            if (Camera.main != null) worldPos = Camera.main.ScreenToWorldPoint(screen);

            for (int i = 0; i < enemiseList.Count; i++)
            {
                float diffX = enemiseList[i].transform.position.x - worldPos.x;
                float diffY = enemiseList[i].transform.position.y - worldPos.y;

                float absDiffX = Mathf.Abs(diffX);
                float absDiffY = Mathf.Abs(diffY);

                Transform randomEnemyPos = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];

                if (enemiseList[i].gameObject.activeSelf == false)
                {
                    enemiseList[i].gameObject.SetActive(true);
                    enemiseList[i].gameObject.transform.position = randomEnemyPos.position;
                }
                if (absDiffX >= xReplaceDistance)
                {
                    enemiseList[i].gameObject.transform.position = randomEnemyPos.position;
                    enemiseList[i].RegistGrid();
                }
                if (absDiffY >= yReplaceDistance)
                {
                    enemiseList[i].gameObject.transform.position = randomEnemyPos.position;
                    enemiseList[i].RegistGrid();
                }
            }

            for (int i = 0; i < haveHotteoksKind; i++)
            {
                HotteokName type = (HotteokName)i;
                
                // 해당 호떡 타입의 코루틴이 없거나 이미 종료된 경우에만 새 코루틴 시작
                // ContainsKey(type): Dictionary에 해당 호떡의 코루틴 기록이 있는지 확인
                // hotteokSpawnCoroutines[type] == null: 코루틴이 완료되었는지 확인
                // → 중복 실행 방지: 같은 호떡의 코루틴이 동시에 여러 개 실행되지 않음
                if (!hotteokSpawnCoroutines.ContainsKey(type) || hotteokSpawnCoroutines[type] == null)
                {
                    hotteokSpawnCoroutines[type] = StartCoroutine(SpawnHotteokByLevelCoroutine(i));
                }
            }
        }

        public void AddEnemyList(int enemyKind, int count)
        {
            for (int j = 0; j < count; j++)
            {
                Transform randomEnemyPos = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
                Enemy spawnedEnemy = enemisePool[enemyKind].Get(true);

                enemiseList.Add(spawnedEnemy);

                spawnedEnemy.transform.position = randomEnemyPos.position;
            }
        }

        public void SpawnHotteokByLevel(int hotteokName)
        {
            HotteokName type = (HotteokName)hotteokName;
            int currentLevel = hotteokLevel[hotteokName];

            for (int i = 0; i < currentLevel; i++)
            {
                if (hotteokDic.TryGetValue(type, out List<Hotteok> hotteoksList))
                {
                    if (i < hotteoksList.Count)
                    {
                        if (hotteoksList[i] != null && !hotteoksList[i].gameObject.activeSelf)
                        {
                            hotteoksList[i].gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        AddHottoek(type, hotteoksPool[hotteokName].Get(true));
                    }
                }
                else
                {
                    AddHottoek(type, hotteoksPool[hotteokName].Get(true));
                }
            }
        }

        public void HotteokReset()
        {
            foreach (KeyValuePair<HotteokName, List<Hotteok>> entry in hotteokDic)
            {
                for (int i = 0; i < entry.Value.Count; i++)
                {
                    entry.Value[i].enemy = null;
                }
            }
        }
        
        /// <summary>
        /// 각 호떡 타입별 고유한 쿨타임을 가지고 호떡을 반복 스폰하는 코루틴
        /// 
        /// 동작 흐름:
        /// 1. SpawnHotteokByLevel(hotteokName) 실행 → 해당 호떡 스폰/활성화
        /// 2. hotteokCooldowns 배열에서 해당 호떡의 쿨타임 조회
        /// 3. WaitForSeconds(cooldown) → 쿨타임만큼 대기
        /// 4. 2-3번 반복 (무한 루프)
        /// 
        /// 예시: hotteokCooldowns[0] = 0.5f이면
        ///   - 호떡A 스폰 → 0.5초 대기 → 호떡A 스폰 → 0.5초 대기 → ...
        /// </summary>
        private IEnumerator SpawnHotteokByLevelCoroutine(int hotteokName)
        {
            while (true)
            {
                // 호떡 스폰/활성화 (기존 풀링 로직 유지)
                SpawnHotteokByLevel(hotteokName);
                
                // 해당 호떡의 쿨타임 가져오기
                // hotteokCooldowns 배열이 유효하고 인덱스가 범위 내면 설정된 값 사용
                // 아니면 기본값 1초 사용
                float cooldown = (hotteokCooldowns != null && hotteokName < hotteokCooldowns.Length) 
                    ? hotteokCooldowns[hotteokName] 
                    : 1f;
                
                // 쿨타임만큼 대기 (지정된 시간 동안 코루틴 일시 중지)
                yield return new WaitForSeconds(cooldown);
            }
        }


        public void AddHottoek(HotteokName hotteokName, Hotteok hotteok)
        {
            if (hotteokDic.TryGetValue(hotteokName, out List<Hotteok> hotteoksList))
            {
                // 2-A. 이미 명단이 존재한다면, 기존 명단에 이 오브젝트를 추가하기만 합니다.
                hotteoksList.Add(hotteok);
            }
            else
            {
                // 2-B. 명단이 없다면(이 방에 처음 들어왔다면), 새 명단을 만들고 등록합니다.
                List<Hotteok> newHotteoks = new List<Hotteok>();
                newHotteoks.Add(hotteok);
                hotteokDic.Add(hotteokName, newHotteoks);
            }
        }
    }
}