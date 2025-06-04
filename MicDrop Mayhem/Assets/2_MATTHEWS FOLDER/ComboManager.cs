using UnityEngine;

public class ComboManager : MonoBehaviour
{
    public float beatWindow = 0.5f; // How long a combo stays alive after a hit (e.g., half a second)
    private float comboTimer = 0f;
    private bool isInCombo = false;

    public delegate void ComboEnd();
    public event ComboEnd OnComboEnd;

    void Update()
    {
        if (isInCombo)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                isInCombo = false;
                Debug.Log("Combo ended.");
                OnComboEnd?.Invoke();
            }
        }
    }

    public void RegisterHit()
    {
        isInCombo = true;
        comboTimer = beatWindow;
        Debug.Log("Combo continued.");
    }

    public bool IsInCombo() => isInCombo;
}
