using System.Collections.Generic;
using UnityEngine;

// 떨어진 코인 목록: 코인 자석이 매 프레임 씬 전체를 태그로 뒤지지 않도록 코인이 스스로 등록 · 해제
public class CoinTag : MonoBehaviour
{
    public static readonly List<Transform> All = new List<Transform>();

    public static void Register(GameObject coin)
    {
        if (coin != null && coin.GetComponent<CoinTag>() == null) coin.AddComponent<CoinTag>();
    }

    void OnEnable() => All.Add(transform);
    void OnDisable() => All.Remove(transform);
}
