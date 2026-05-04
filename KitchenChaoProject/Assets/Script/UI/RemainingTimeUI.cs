using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RemainingTimeUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    void Start()
    {
        timerText = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        timerText.text = "Remaining time(s):"+((int)GameManager.Instance.GetGamePlayingTimer());
    }
}
