using System.Collections;
using UnityEngine;

public class GeradorDeComida : MonoBehaviour
{
    [Header("Configurações")]
    public GameObject prefabComida;
    public float tempoEntreSpawns = 4f;

    [Header("Sorteio")]
    public Transform[] pontosDeSpawn;
    public ComidaData[] bancoDeComidas;

    private void Start()
    {
        StartCoroutine(RotinaDeSpawn());
    }

    private IEnumerator RotinaDeSpawn()
    {
        while (true)
        {
            // Sorteia um lugar aleatório do array de pontos
            int indexLugar = Random.Range(0, pontosDeSpawn.Length);
            Transform lugarSorteado = pontosDeSpawn[indexLugar];

            // Cria a comida usando o molde no lugar sorteado
            GameObject novaComida = Instantiate(prefabComida, lugarSorteado.position, Quaternion.identity);

            // aleatoriza um dos ScriptableObjects (Pão, Milho, etc)
            int indexComida = Random.Range(0, bancoDeComidas.Length);
            ComidaData dadosSorteados = bancoDeComidas[indexComida];

            // Passa os dados sorteados para o objeto recém criado na tela
            novaComida.GetComponent<ItemMap>().ConfigurarItem(dadosSorteados);

            // Pausa o loop e espera X segundos antes de repetir
            yield return new WaitForSeconds(tempoEntreSpawns);
        }
    }
}