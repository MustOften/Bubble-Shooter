using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePlayController : MonoBehaviour
{
    public void ToStartMenu()
    {
        SceneManager.LoadScene("Main menu");
    }

    public void Link()
    {
        Application.OpenURL("http://vk.com/mustoften/");
    }
}
