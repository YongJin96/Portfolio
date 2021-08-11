using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private bool IsPause;

    void Start()
    {
        
    }

    void Update()
    {
        Pause();
        ReStart();
    }

    private void Pause()
    {
        if (Input.GetKeyDown(KeyCode.P) && IsPause == false)
        {
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            IsPause = true;
        }
        else if (Input.GetKeyDown(KeyCode.P) && IsPause == true)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            IsPause = false;
        }
    }

    static public void ReStart()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F1))
        {
            SceneManager.LoadScene(1);
        }
        else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F2))
        {
            SceneManager.LoadScene(2);
        }
    }
}
