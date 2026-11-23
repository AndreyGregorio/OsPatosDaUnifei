using System.Collections;
using UnityEngine;










[RequireComponent(typeof(Rigidbody2D))]
public class PatoDeFila : MonoBehaviour
{
    [Header("Fila")]
    public float velocidade = 34f;
    public float distanciaNaFila = 4.8f;
    public float velocidadeDePasseio = 6f;

    [Header("Visual")]
    public Color corDoConvite = new Color(1f, 0.95f, 0.35f, 0.98f);

    public bool Recrutado { get; private set; }
    public bool Entregue { get; private set; }
    public Transform Alvo { get; private set; }

    private Rigidbody2D rb;
    private SpriteRenderer desenho;
    private Animator animator;
    private Vector3 escalaOriginal;

    private Vector2 direcao;
    private float velocidadeAtual;
    private Rect areaDePasseio;
    private Vector2 destinoDoPasseio;
    private float proximaTrocaDeDestino;

    private GameObject marcador;
    private SpriteRenderer desenhoDoMarcador;
    private static Sprite spriteDoConvite;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        desenho = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        escalaOriginal = transform.localScale;
    }

    
    
    
    public void Configurar(float velocidadeDoSeguidor, float espacamento,
                           Color cor, Rect passeioInicial, float velocidadeDoPasseio)
    {
        velocidade = Mathf.Max(1f, velocidadeDoSeguidor);
        distanciaNaFila = Mathf.Max(1f, espacamento);
        velocidadeDePasseio = Mathf.Max(0.5f, velocidadeDoPasseio);

        if (desenho != null) desenho.color = cor;

        areaDePasseio = passeioInicial;
        EscolherDestino();
    }

    public void DefinirAlvo(Transform novoAlvo)
    {
        Alvo = novoAlvo;
    }

    public void Recrutar()
    {
        if (Recrutado || Entregue) return;

        Recrutado = true;
        MostrarMarcador(false);
        StartCoroutine(Pulinho(0.22f));
    }

    public void Entregar(Rect areaDoLago)
    {
        if (Entregue) return;

        Recrutado = false;
        Entregue = true;
        Alvo = null;

        areaDePasseio = areaDoLago;
        direcao = Vector2.zero;
        velocidadeAtual = 0f;

        if (rb != null) rb.linearVelocity = Vector2.zero;

        MostrarMarcador(false);
        EscolherDestino();
        StartCoroutine(Pulinho(0.3f));
    }

    public void Parar()
    {
        direcao = Vector2.zero;
        velocidadeAtual = 0f;

        if (rb != null) rb.linearVelocity = Vector2.zero;

        AtualizarAnimacao();
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;

        AtualizarMarcador();

        if (Entregue)
        {
            Passear();
            return;
        }

        if (!Recrutado)
        {
            Passear();
            return;
        }

        Seguir();
    }

    void Seguir()
    {
        if (Alvo == null)
        {
            direcao = Vector2.zero;
            velocidadeAtual = 0f;
            AtualizarAnimacao();
            return;
        }

        Vector2 paraAlvo = (Vector2)Alvo.position - (Vector2)transform.position;
        float distancia = paraAlvo.magnitude;

        
        if (distancia > distanciaNaFila + 0.4f)
        {
            direcao = paraAlvo.normalized;
            velocidadeAtual = velocidade;
        }
        else
        {
            direcao = Vector2.zero;
            velocidadeAtual = 0f;
        }

        AtualizarAnimacao();
    }

    void Passear()
    {
        if (Time.time >= proximaTrocaDeDestino ||
            Vector2.Distance(transform.position, destinoDoPasseio) < 0.7f)
        {
            EscolherDestino();
        }

        Vector2 paraDestino = destinoDoPasseio - (Vector2)transform.position;

        if (paraDestino.sqrMagnitude > 0.02f)
        {
            direcao = paraDestino.normalized;
            velocidadeAtual = velocidadeDePasseio;
        }
        else
        {
            direcao = Vector2.zero;
            velocidadeAtual = 0f;
        }

        AtualizarAnimacao();
    }

    void EscolherDestino()
    {
        proximaTrocaDeDestino = Time.time + Random.Range(2.5f, 5.5f);

        if (areaDePasseio.width < 1f || areaDePasseio.height < 1f)
        {
            destinoDoPasseio = transform.position;
            return;
        }

        float margem = 0.9f;
        float xMin = areaDePasseio.xMin + margem;
        float xMax = areaDePasseio.xMax - margem;
        float yMin = areaDePasseio.yMin + margem;
        float yMax = areaDePasseio.yMax - margem;

        if (xMax <= xMin) { xMin = areaDePasseio.center.x; xMax = xMin; }
        if (yMax <= yMin) { yMin = areaDePasseio.center.y; yMax = yMin; }

        destinoDoPasseio = new Vector2(Random.Range(xMin, xMax), Random.Range(yMin, yMax));
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        rb.linearVelocity = direcao * velocidadeAtual;
    }

    void AtualizarAnimacao()
    {
        if (animator == null) return;

        animator.SetFloat("Velocidade", direcao.sqrMagnitude);

        if (Mathf.Abs(direcao.x) > 0.01f)
        {
            animator.SetFloat("MovX", direcao.x);
        }
    }

    
    
    
    public void MostrarMarcador(bool mostrar)
    {
        if (Recrutado || Entregue) mostrar = false;

        if (mostrar && marcador == null) CriarMarcador();

        if (marcador != null && marcador.activeSelf != mostrar)
        {
            marcador.SetActive(mostrar);
        }
    }

    void AtualizarMarcador()
    {
        if (marcador == null || !marcador.activeSelf) return;

        float pulso = 1f + 0.1f * Mathf.Sin(Time.time * 6f);
        marcador.transform.localScale = Vector3.one * pulso;
    }

    void CriarMarcador()
    {
        marcador = new GameObject("ConviteS");
        marcador.transform.SetParent(transform, false);
        marcador.transform.localPosition = new Vector3(0f, 0.92f, 0f);
        marcador.transform.localScale = Vector3.one;

        desenhoDoMarcador = marcador.AddComponent<SpriteRenderer>();
        desenhoDoMarcador.sprite = SpriteDoConvite();
        desenhoDoMarcador.color = corDoConvite;
        ArteProcedural.ColocarNaFrente(desenhoDoMarcador);

        marcador.SetActive(false);
    }

    static Sprite SpriteDoConvite()
    {
        if (spriteDoConvite == null)
        {
            spriteDoConvite = ArteProcedural.Criar(new string[]
            {
                "WWWWW",
                "WW...",
                "WW...",
                ".WWWW",
                "....WW",
                "....WW",
                "WWWWW",
            });
        }

        return spriteDoConvite;
    }

    IEnumerator Pulinho(float forca)
    {
        const float duracao = 0.22f;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duracao;

            float pulso = 1f + forca * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            transform.localScale = escalaOriginal * pulso;

            yield return null;
        }

        transform.localScale = escalaOriginal;
    }
}
