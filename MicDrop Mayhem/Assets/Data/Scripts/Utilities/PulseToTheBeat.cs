using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulseToTheBeat : MonoBehaviour
{
    [SerializeField] float _pulseSize = 1.15f;
    [SerializeField] float _returnSpeed = 5f;
    private Vector3 _startSize;
    public bool canPulse = false;

    private void Start()
    {
        _startSize = transform.localScale;
    }
    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, _startSize, Time.deltaTime * _returnSpeed);

    }

    public void Pulse()
    {
        if(canPulse == true)
        {
            transform.localScale = _startSize * _pulseSize;
        }
        //VFX
    }
    public void CanPulse()
    {
        StartCoroutine(Can_StartPulsing());
    }

    IEnumerator Can_StartPulsing()
    {
        yield return new WaitForSeconds(1);
        canPulse = true;
    }
}
