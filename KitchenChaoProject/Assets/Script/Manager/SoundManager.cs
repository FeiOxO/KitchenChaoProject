using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    #region 单例模式
    public static SoundManager Instance { get; private set; }
    private Camera mainCam;
    private AudioSource audioSource;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        DontDestroyOnLoad(gameObject);
        mainCam = Camera.main;
        audioSource = GetComponent<AudioSource>();
        GameData.OnBackGroundVolumeValueChanged += SetBackgroundVolume;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }
    #endregion
    [SerializeField] private AudioClipRefsSO audioClipRefsSO;

    private void SetBackgroundVolume(float value)
    {
        audioSource.volume = value;
    }

    private void OnSceneLoaded(Scene previousScene,LoadSceneMode mode)
    {
        mainCam = Camera.main;
        if(previousScene.name != "0-GameMenu" && previousScene.name != "1-LoadScene")
        {
            if(OrderManager.Instance){
                OrderManager.Instance.OnRecipeSuccessed += OrderManager_OnRecipeSuccessed;
                OrderManager.Instance.OnRecipeFailed += OrderManager_OnRecipeFailed;
            }
            CuttingCounter.OnRecipeCut += CuttingCounter_OnRecipeCut;
            KitchenObjectHolder.OnDrop += KitchenObjectHolder_OnDrop;
            KitchenObjectHolder.OnPickup += KitchenObjectHolder_OnPickup;
            TrashCounter.OnObjectTrashed += TrashCounter_OnObjectTrashed;
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if(scene.name != "0-GameMenu" && scene.name != "1-LoadScene")
        {
            if(OrderManager.Instance){
                OrderManager.Instance.OnRecipeSuccessed -= OrderManager_OnRecipeSuccessed;
                OrderManager.Instance.OnRecipeFailed -= OrderManager_OnRecipeFailed;
            }
            CuttingCounter.OnRecipeCut -= CuttingCounter_OnRecipeCut;
            KitchenObjectHolder.OnDrop -= KitchenObjectHolder_OnDrop;
            KitchenObjectHolder.OnPickup -= KitchenObjectHolder_OnPickup;
            TrashCounter.OnObjectTrashed -= TrashCounter_OnObjectTrashed;
        }
    }

    private void TrashCounter_OnObjectTrashed(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.trash,GameData.soundVolumeValue);
    }

    private void KitchenObjectHolder_OnPickup(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.objectPickup,GameData.soundVolumeValue);
    }

    private void KitchenObjectHolder_OnDrop(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.objectDrop,GameData.soundVolumeValue);
    }

    private void CuttingCounter_OnRecipeCut(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.chop,GameData.soundVolumeValue);
    }

    private void OrderManager_OnRecipeFailed(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.dekiveryFail,GameData.soundVolumeValue);
    }

    private void OrderManager_OnRecipeSuccessed(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.deliverySuccess,GameData.soundVolumeValue);
    }

    public void PlayStepSound()
    {
        PlaySound(audioClipRefsSO.footStep,GameData.soundVolumeValue);
    }

    private void PlaySound(AudioClip[] _clips, float _volume)
    {
        PlaySound(_clips, mainCam.transform.position, _volume);
    }

    private void PlaySound(AudioClip[] _clips, Vector3 _position, float _volume)
    {
        int index = Random.Range(0, _clips.Length);

        AudioSource.PlayClipAtPoint(_clips[index], _position, _volume);
    }
    private void OnDestroy()
    {
        GameData.OnBackGroundVolumeValueChanged -= SetBackgroundVolume;
    }
}
