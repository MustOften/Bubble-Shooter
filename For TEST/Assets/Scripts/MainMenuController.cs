using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public GameObject QuitMenu;

    private void Awake()
    {
        QuitMenu.SetActive(false);
    }

    public void Play()
    {
        SceneManager.LoadScene("GamePlay");
    }

    public void AboutGame()
    {
        SceneManager.LoadScene("AboutGame");
    }

    public void QuitAgree()
    {
        QuitMenu.SetActive(true);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void BackToMenu()
    {
        QuitMenu.SetActive(false);
    }
}