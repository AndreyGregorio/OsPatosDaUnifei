using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventarioUI : MonoBehaviour
{
    [Header("Referências")]
    public InventarioPlayer playerInventario;
    public Image iconeItemSlot1;
    public TextMeshProUGUI textoQuantidadeSlot1;
    public Sprite spriteMilho;

    private TipoDeComida ultimoTipo = (TipoDeComida)(-1);

    void Update()
    {
        if (playerInventario == null) return;

        TipoDeComida tipo = playerInventario.tipoSelecionado;
        int quantidade = playerInventario.QuantidadeDe(tipo);

        if (iconeItemSlot1 != null)
        {
            iconeItemSlot1.gameObject.SetActive(true);
            iconeItemSlot1.sprite = spriteMilho;

            Color cor = ComidaComTipo.CorDe(tipo);
            if (quantidade <= 0) cor.a = 0.25f;
            iconeItemSlot1.color = cor;
        }

        if (textoQuantidadeSlot1 != null)
        {
            textoQuantidadeSlot1.gameObject.SetActive(true);
            textoQuantidadeSlot1.text = quantidade.ToString();
        }

        if (tipo != ultimoTipo)
        {
            ultimoTipo = tipo;
            Debug.Log($"Mochila: {ComidaComTipo.NomeDe(tipo)} selecionado ({quantidade} unidade(s)).");
        }
    }
}
