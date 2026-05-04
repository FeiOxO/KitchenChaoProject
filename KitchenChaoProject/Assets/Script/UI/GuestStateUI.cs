using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuestStateUI : MonoBehaviour
{
    public Image stateImage;
    public Sprite happy;
    public Sprite angrey;

    void Start()
    {
        Hide();
    }

    public void ShowHappy()
    {
        stateImage.sprite = happy;
        Show();
    }

    public void ShowAngrey()
    {
        stateImage.sprite = angrey;
        Show();
    }

    private void Show()
    {
        gameObject.SetActive(true);    
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
