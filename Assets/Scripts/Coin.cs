using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public int coins = 0;
    


public TextMeshProUGUI TextInput;




    void Start()
    {
        UpdateCoinText();
    }


    public void AddCoin(int coin)
    {
        coins += coin;
        UpdateCoinText();
    }


    void UpdateCoinText()
    {
        if (TextInput != null) TextInput.text = coins.ToString();

    }

    public void SubCoin(int coin)
    {
        coins -= coin;
        UpdateCoinText();
    }

}
