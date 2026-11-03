using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Duck : MonoBehaviour
{
    [Header("Rotas (Waypoints)")]
    [SerializeField] private GameObject[] waypoint1;

    [Header("Configurações de Movimento")]
    public float tempoEntrePontos = 3f;
    public float tempoParadoNoPonto = 1.5f;

    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();

        if (waypoint1.Length > 0)
        {
            StartCoroutine(MoverPelosWaypoints(waypoint1));
        }
    }

    private IEnumerator MoverPelosWaypoints(GameObject[] caminho)
    {
        while (true)
        {
            for (int i = 0; i < caminho.Length; i++)
            {
                Vector3 posicaoInicial = transform.position;
                Vector3 posicaoFinal = caminho[i].transform.position;

                // 1. Calcula a direção exata para onde ele está indo
                Vector3 direcao = (posicaoFinal - posicaoInicial).normalized;

                // 2. Manda para o Animator o valor de X (-1 é esquerda, 1 é direita)
                animator.SetFloat("MovX", direcao.x);
                animator.SetBool("Andando", true);

                float tempoDecorrido = 0f;

                while (tempoDecorrido < tempoEntrePontos)
                {
                    transform.position = Vector3.Lerp(posicaoInicial, posicaoFinal, tempoDecorrido / tempoEntrePontos);
                    tempoDecorrido += Time.deltaTime;
                    yield return null;
                }

                transform.position = posicaoFinal;
                animator.SetBool("Andando", false);

                yield return new WaitForSeconds(tempoParadoNoPonto);
            }
        }
    }
}