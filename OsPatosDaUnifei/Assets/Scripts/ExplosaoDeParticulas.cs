using UnityEngine;

public class ExplosaoDeParticulas : MonoBehaviour
{
    [Header("Física das partículas")]
    public float gravidade = 9f;
    public float duracao = 0.75f;

    private SpriteRenderer[] desenhos;
    private Vector2[] velocidades;
    private float[] giros;
    private Color corBase;
    private float nascimento;
    private static Sprite spriteCache;

    static Sprite Particula()
    {
        if (spriteCache == null)
        {
            spriteCache = ArteProcedural.Criar(new string[]
            {
                ".WW.",
                "WWWW",
                "WWWW",
                ".WW.",
            }, 16f);
        }

        return spriteCache;
    }

    public static void Soltar(Vector3 posicao, Color cor, int quantidade = 8, float tamanho = 0.85f)
    {
        GameObject raiz = new GameObject("Particulas");
        raiz.transform.position = posicao;

        ExplosaoDeParticulas explosao = raiz.AddComponent<ExplosaoDeParticulas>();
        explosao.Montar(cor, quantidade, tamanho);
    }

    void Montar(Color cor, int quantidade, float tamanho)
    {
        nascimento = Time.time;
        corBase = cor;

        desenhos = new SpriteRenderer[quantidade];
        velocidades = new Vector2[quantidade];
        giros = new float[quantidade];

        for (int i = 0; i < quantidade; i++)
        {
            GameObject pedaco = new GameObject("peninha");
            pedaco.transform.SetParent(transform, false);
            pedaco.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = pedaco.AddComponent<SpriteRenderer>();
            sr.sprite = Particula();
            sr.color = cor;
            ArteProcedural.ColocarNaFrente(sr);

            float tamanhoDoSprite = sr.sprite.bounds.size.x;
            float escala = (tamanho * Random.Range(0.7f, 1.3f)) / Mathf.Max(0.001f, tamanhoDoSprite);
            pedaco.transform.localScale = Vector3.one * escala;
            pedaco.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            
            float angulo = Random.Range(0f, Mathf.PI * 2f);
            float forca = Random.Range(2.5f, 7f);
            velocidades[i] = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * forca
                           + Vector2.up * Random.Range(3f, 7f);

            giros[i] = Random.Range(-320f, 320f);

            desenhos[i] = sr;
        }
    }

    void Update()
    {
        float t = (Time.time - nascimento) / Mathf.Max(0.01f, duracao);

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        for (int i = 0; i < desenhos.Length; i++)
        {
            velocidades[i] += Vector2.down * (gravidade * Time.deltaTime);

            desenhos[i].transform.localPosition += (Vector3)(velocidades[i] * Time.deltaTime);
            desenhos[i].transform.Rotate(0f, 0f, giros[i] * Time.deltaTime);

            Color c = corBase;
            c.a = (1f - t) * (1f - t);
            desenhos[i].color = c;
        }
    }
}
