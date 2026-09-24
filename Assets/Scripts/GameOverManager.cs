using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPressRestart()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnPressMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
