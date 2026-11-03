using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ItemMap : MonoBehaviour
{
    public ComidaData dadosDesteItem;

    public void ConfigurarItem(ComidaData novosDados)
    {
        dadosDesteItem = novosDados;
        GetComponent<SpriteRenderer>().sprite = dadosDesteItem.artePixel;
    }
}