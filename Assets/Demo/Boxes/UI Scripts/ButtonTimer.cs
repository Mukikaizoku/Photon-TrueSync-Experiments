using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonTimer : MonoBehaviour {

    private float countdown = 0f;

    // Button countdown
    void Update() {
        if (countdown > 0)
        {
            countdown -= Time.deltaTime;
            if (countdown <= 0)
            {
                this.GetComponent<Button>().interactable = true;
            }
        }
	}

    public void DisableButtonAndStartTimer (float seconds)
    {
        countdown = seconds;
        this.GetComponent<Button>().interactable = false;
    }
}
