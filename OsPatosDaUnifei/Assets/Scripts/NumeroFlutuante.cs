using TMPro;
using UnityEngine;

public class NumeroFlutuante : MonoBehaviour
{
    public float duracao = 0.9f;
    public float altura = 4f;
    private TextMeshPro texto;
    private Color corOriginal;
    private Vector3 posicaoInicial;
    private Vector3 escalaDoTexto;
    private float nascimento;
    private static TMP_FontAsset fonteCache;

    public static void Criar(Vector3 posicao, string conteudo, Color cor, float alturaMundo = 2.4f)
    {
        GameObject objeto = new GameObject("Numero " + conteudo);
        objeto.transform.position = posicao;

        TextMeshPro tmp = objeto.AddComponent<TextMeshPro>();
        tmp.text = conteudo;
        tmp.color = cor;
        tmp.fontSize = 36f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        TMP_FontAsset fonte = Fonte();
        if (fonte != null) tmp.font = fonte;

        
        SortingLayer[] camadas = SortingLayer.layers;
        if (camadas.Length > 0) tmp.sortingLayerID = camadas[camadas.Length - 1].id;
        tmp.sortingOrder = 32000;

        NumeroFlutuante numero = objeto.AddComponent<NumeroFlutuante>();
        numero.texto = tmp;
        numero.corOriginal = cor;
        numero.posicaoInicial = posicao;
        numero.nascimento = Time.time;

        
        
        tmp.ForceMeshUpdate();
        float alturaAtual = tmp.textBounds.size.y;
        float fator = alturaAtual > 0.0001f ? alturaMundo / alturaAtual : 1f;
        numero.escalaDoTexto = Vector3.one * fator;
        objeto.transform.localScale = Vector3.zero;   
    }

    void Update()
    {
        float t = (Time.time - nascimento) / Mathf.Max(0.01f, duracao);

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        
        float subida = Mathf.Sin(t * Mathf.PI * 0.5f);
        transform.position = posicaoInicial + Vector3.up * (altura * subida);

        
        float pop = t < 0.18f ? Mathf.Lerp(0.2f, 1f, t / 0.18f) : 1f;
        transform.localScale = escalaDoTexto * pop;

        
        Color c = corOriginal;
        c.a = 1f - Mathf.Pow(t, 2.2f);
        if (texto != null) texto.color = c;
    }

    static TMP_FontAsset Fonte()
    {
        if (fonteCache != null) return fonteCache;

        fonteCache = TMP_Settings.defaultFontAsset;

        if (fonteCache == null)
        {
            TextMeshProUGUI qualquerTextoDoHud = Object.FindAnyObjectByType<TextMeshProUGUI>();
            if (qualquerTextoDoHud != null) fonteCache = qualquerTextoDoHud.font;
        }

        return fonteCache;
    }
}
