using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class OrderManager : MonoBehaviour
{
    #region 单例模式
    public static OrderManager Instance { get; private set; }
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
        // DontDestroyOnLoad(gameObject);
    }
    #endregion

    // 订单变化事件
    public event EventHandler OnRecipeSpawned;
    // 送菜成功事件
    public event EventHandler OnRecipeFailed;
    // 送菜失败事件
    public event EventHandler OnRecipeSuccessed;


    [SerializeField] private RecipeListSO recipleSOList;
    [SerializeField] private int orderMAXCount = 5;
    [SerializeField] private float orderRate = 2;

    [SerializeField] private List<RecipeSO> orderRecipeSOList = new List<RecipeSO>();

    private int orderCount = 0;
    private int successDeliveryCount = 0;
    private int failDeliveryCount = 0;
    private float orderTimer = 0;
    private bool isStartOrder = false;

    private void Start()
    {
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanger;
    }

    private void GameManager_OnStateChanger(object sender, EventArgs e)
    {
        if (GameManager.Instance.IsGamePlayingState())
        {
            StartSpawnOrder();
        }
    }

    private void Update()
    {
        if (isStartOrder)
        {
            if (orderMAXCount <= orderCount) return;
            OrderUpdate();
        }
    }

    private void OrderUpdate()
    {
        orderTimer += Time.deltaTime;
        if (orderTimer >= orderRate)
        {
            orderTimer = 0;
            if (GuestManager.Instance.GetEmptyDeliveryCounter())
            {
                OrderANewRecipe();
            }
        }
    }

    private void OrderANewRecipe()
    {
        orderCount++;
        int index = UnityEngine.Random.Range(0, recipleSOList.recipeSOList.Count);
        orderRecipeSOList.Add(recipleSOList.recipeSOList[index]);
        GuestManager.Instance.GuestGetOrder();
        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }

    public void DeliverRecipe(PlateKitchenObject _plateKitchenObject)
    {
        RecipeSO correctRecipe = null;
        foreach (RecipeSO _recipe in orderRecipeSOList)
        {
            if (IsCorrect(_recipe, _plateKitchenObject))
            {
                correctRecipe = _recipe;
                break;
            }
        }
        
        if (correctRecipe == null)
        {
            OnRecipeFailed?.Invoke(this, EventArgs.Empty);
            failDeliveryCount++;
            Debug.Log("上菜失败");
        }
        else
        {
            orderRecipeSOList.Remove(correctRecipe);
            OnRecipeSuccessed?.Invoke(this, EventArgs.Empty);
            successDeliveryCount++;
            Debug.Log("上菜成功");
        }
        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }

    public static bool IsCorrect(RecipeSO _recipe, PlateKitchenObject _plateKitchenObject)
    {
        List<KitchenObjectSO> list1 = _recipe.kitchenObjectSOList;
        List<KitchenObjectSO> list2 = _plateKitchenObject.GetKitchenObjectSOLIst();

        if (list1.Count != list2.Count) return false;

        foreach (KitchenObjectSO kitchenObjectSO in list1)
        {
            if (list2.Contains(kitchenObjectSO) == false) return false;
        }

        return true;
    }

    public List<RecipeSO> GetOrderRecipeSOList()
    {
        return orderRecipeSOList;
    }

    public void StartSpawnOrder()
    {
        isStartOrder = true;
    }

    public int GetSuccessDeliveryCount()
    {
        return successDeliveryCount;
    }
    public int GetFailDeliveryCount()
    {
        return failDeliveryCount;
    }
}
