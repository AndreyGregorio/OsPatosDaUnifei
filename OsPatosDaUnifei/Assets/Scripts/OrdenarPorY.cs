using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class OrdenarPorY : MonoBehaviour
{
    [Tooltip("Ajuste fino em unidades de mundo, caso a base do sprite não case com a linha do chão")]
    [SerializeField] private float ajusteY = 0f;

    [Tooltip("Precisão da ordenação entre dois sprites quase na mesma altura")]
    [SerializeField] private int precisao = 100;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    
    void LateUpdate()
    {
        
        
        float baseY = sr.bounds.min.y + ajusteY;
        sr.sortingOrder = Mathf.RoundToInt(-baseY * precisao);
    }
}