using UnityEngine;

public class RandomEvents : MonoBehaviour
{
    private int strobeLightsRandomizer1;
    private int strobeLightsRandomizer2;

    // Update is called once per frame
    void Update()
    {
        strobeLightsRandomizer1 = UnityEngine.Random.Range(0, 2000); // Random number between 0 and 1999. Increase or decrase range as needed
        strobeLightsRandomizer2 = UnityEngine.Random.Range(0, 10);

        if (strobeLightsRandomizer1 == strobeLightsRandomizer2)
        {
            // Place your strobe light activation code here
        }
    }
}
