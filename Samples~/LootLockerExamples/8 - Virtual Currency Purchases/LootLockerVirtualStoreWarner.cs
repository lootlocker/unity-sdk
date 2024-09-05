using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LootLockerVirtualStoreWarner : MonoBehaviour
{

    public static LootLockerVirtualStoreWarner Instance;

    [SerializeField] private Text warningText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else 
            Destroy(this.gameObject);
    }


    public void ShowText(string text)
    {
        warningText.text = text;
        StartCoroutine(ShowText());
    }

    public IEnumerator ShowText()
    {
        warningText.gameObject.SetActive(true);

        yield return new WaitForSeconds(6);

        warningText.gameObject.SetActive(false);
    }


}
