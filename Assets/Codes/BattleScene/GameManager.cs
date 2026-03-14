using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{

    public GameObject gameOverUI;
    public GameObject gameClearUI;
    private bool isGameOver = false;
    
    void Start()
    {
        gameClearUI.SetActive(false);
        gameOverUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartGame();
        }
    }

    public void ShowGameOver()
    {
        if(isGameOver)
            return;
        
        isGameOver = true;
        gameOverUI.SetActive(true);
    }

    public void ShowGameClear()
    {
        if(isGameOver)
            return;
        
        isGameOver = true;
        gameClearUI.SetActive(true);
    }

    void RestartGame()
    {
        SceneManager.LoadScene("TitleScene");
    }
}
