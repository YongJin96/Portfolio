using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InteractionUI : MonoBehaviour
{
    public GameObject InteractionCanvas;
    public Image PressButtonImage;

    private IEnumerator LookAtCamera()
    {
        PressButtonImage.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

        yield return new WaitForSeconds(0.2f);
    }

    public void ViewUI(bool value)
    {
        InteractionCanvas.SetActive(value);
    }
}
