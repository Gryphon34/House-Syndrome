using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    public GameObject loadingUIRoot;
    // 게임 시작 시 불러올 씬 이름 (Build Settings에 등록된 이름과 같아야 함)
    public string gameSceneName = "HouseSyndromeScene";

    public void StartGame()
    {
        StartCoroutine(LoadAsynchronously());
    }

    IEnumerator LoadAsynchronously()
    {
        // 로딩 화면 활성화 (이때 LoadingText의 OnEnable이 실행되며 점 애니메이션 시작)
        if (loadingUIRoot != null)
        {
            loadingUIRoot.SetActive(true);
        }

        // 비동기 씬 로드
        AsyncOperation operation = SceneManager.LoadSceneAsync(gameSceneName);

        while (!operation.isDone)
        {
            yield return null;
        }
    }

    public void LoadGame()
    {
        // 세이브 시스템 구현 시 여기에 로딩 로직을 넣습니다.
        Debug.Log("데이터 로드 기능을 실행합니다.");
    }

    public void OpenSettings()
    {
        // 설정 패널 UI를 활성화하는 코드를 넣습니다.
        Debug.Log("설정 메뉴를 엽니다.");
    }

    public void ExitGame()
    {
        Debug.Log("게임을 종료합니다.");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}