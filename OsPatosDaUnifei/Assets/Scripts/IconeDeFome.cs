using UnityEngine;

public class IconeDeFome : MonoBehaviour
{
    public float alturaNoMundo = 1.5f;
    public float alturaAcimaDoPato = 3.4f;
    public float tempoDoCoracao = 2.5f;
    private SpriteRenderer desenho;
    private Pato pato;
    private Vector3 posicaoBase;
    private float escalaLocal;
    private float escalaDoPai = 1f;
    private float tempoSatisfeito = -1f;

    private static Sprite spriteFome;
    private static Sprite spriteCoracao;

    public static IconeDeFome CriarEm(Pato dono)
    {
        GameObject objeto = new GameObject("IconeDeFome");
        objeto.transform.SetParent(dono.transform, false);

        IconeDeFome icone = objeto.AddComponent<IconeDeFome>();
        icone.pato = dono;
        return icone;
    }

    void Start()
    {
        desenho = gameObject.AddComponent<SpriteRenderer>();
        desenho.sprite = DesenhoFome();
        ArteProcedural.ColocarNaFrente(desenho);

        escalaDoPai = Mathf.Max(0.0001f, transform.parent.lossyScale.y);
        escalaLocal = alturaNoMundo / (escalaDoPai * desenho.sprite.bounds.size.y);

        transform.localScale = Vector3.one * escalaLocal;
        posicaoBase = Vector3.up * (alturaAcimaDoPato / escalaDoPai);
        transform.localPosition = posicaoBase;
    }

    void LateUpdate()
    {
        if (pato == null || desenho == null) return;

        if (pato.satisfeito)
        {
            if (tempoSatisfeito < 0f) tempoSatisfeito = Time.time;

            bool aindaMostrando = Time.time - tempoSatisfeito < tempoDoCoracao;

            desenho.enabled = aindaMostrando;
            if (!aindaMostrando) return;

            desenho.sprite = DesenhoCoracao();

            float pulso = 1f + 0.15f * Mathf.Sin((Time.time - tempoSatisfeito) * 9f);
            transform.localScale = Vector3.one * (escalaLocal * pulso);
        }
        else
        {
            desenho.enabled = true;
            desenho.sprite = DesenhoFome();
            transform.localScale = Vector3.one * escalaLocal;
        }
        float balanco = Mathf.Sin(Time.time * 3.2f) * (0.15f / escalaDoPai);
        transform.localPosition = posicaoBase + Vector3.up * balanco;
    }

    static Sprite DesenhoFome()
    {
        if (spriteFome == null)
        {
            spriteFome = ArteProcedural.Criar(new string[]
            {
                ".KKKKK.",
                ".KRRRK.",
                ".KRRRK.",
                ".KRRRK.",
                ".KRRRK.",
                ".KRRRK.",
                ".KKKKK.",
                ".......",
                ".KKKKK.",
                ".KRRRK.",
                ".KKKKK.",
            });
        }

        return spriteFome;
    }

    static Sprite DesenhoCoracao()
    {
        if (spriteCoracao == null)
        {
            spriteCoracao = ArteProcedural.Criar(new string[]
            {
                ".KK...KK.",
                "KRRK.KRRK",
                "KRWRKRRRK",
                "KRRRRRRRK",
                "KRRRRRRRK",
                ".KRRRRRK.",
                "..KRRRK..",
                "...KKK...",
            });
        }

        return spriteCoracao;
    }
}
