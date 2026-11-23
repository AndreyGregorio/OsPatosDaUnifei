using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Pato : MonoBehaviour
{
    public float velocidade = 6f;
    public string tagDaComida = "ComidaPato";
    public float raioDeVisao = 25f;
    public float raioDeChamado = 40f;
    public float distanciaParaComer = 0.6f;
    public float intervaloBusca = 0.25f;
    public float tempoMaximoEmpacado = 4f;
    public int porcoesNecessarias = 2;
    public int porcoesComidas = 0;
    public bool satisfeito = false;
    public string nomeDaEspecie = "Comum";
    public int pontosPorPorcao = 0;
    public Color corDaEspecie = Color.white;
    public bool fogeDoJogador = false;
    public float distanciaDeFuga = 22f;
    public float tempoDeVida = 0f;
    public bool temComidaFavorita = false;
    public TipoDeComida comidaFavorita = TipoDeComida.Alface;
    public int bonusDaComidaFavorita = 15;
    public Color corDaComidaFavorita = new Color(0.65f, 1f, 0.75f);
    public float tempoMinParado = 3f;
    public float tempoMaxParado = 6f;
    public float tempoCaminhando = 2f;
    public bool pintarQuandoSatisfeito = true;
    public Color corSatisfeito = new Color(1f, 0.93f, 0.55f);
    public bool mostrarFeedback = true;
    public float alturaDoFeedback = 3.2f;
    public float tamanhoDoNumero = 2.6f;
    public bool mostrarIconeDeFome = true;
    public Color corDoNumero = new Color(1f, 0.95f, 0.6f);
    public Color corDoBonus = new Color(0.6f, 1f, 0.6f);
    [SerializeField] private Transform alvoAtual;
    [SerializeField] private bool tagValida = true;

    
    private static readonly List<Pato> todos = new List<Pato>();
    public static IReadOnlyList<Pato> Todos => todos;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 direcaoMovimento;
    private Transform alvoComida;
    private bool estaComendo;
    private Vector3 escalaOriginal;
    private float tempoEmpacado;
    private float menorDistancia;
    private Transform comidaIgnorada;
    private float nascimento;
    private bool foiEmbora;

    
    
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        escalaOriginal = transform.localScale;
    }

    void OnEnable()
    {
        if (!todos.Contains(this)) todos.Add(this);
    }

    void OnDisable()
    {
        todos.Remove(this);
    }

    void Start()
    {
        nascimento = Time.time;

        AplicarCorDaEspecie();

        
        if (mostrarIconeDeFome) IconeDeFome.CriarEm(this);

        StartCoroutine(RotinaDoPato());
        StartCoroutine(BuscarComidaPeriodicamente());
    }

    public void AplicarCorDaEspecie()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        spriteRenderer.color = corDaEspecie;
    }

    public static void ChamarPatos(Transform comida)
    {
        ChamarPatos(comida, false);
    }

    public static void ChamarPatos(Transform comida, bool chamarTodos)
    {
        if (comida == null) return;

        for (int i = todos.Count - 1; i >= 0; i--)
        {
            Pato pato = todos[i];

            if (pato == null)      
            {
                todos.RemoveAt(i);
                continue;
            }

            pato.ReceberChamado(comida, chamarTodos);
        }
    }

    
    
    
    
    public static bool AlguemQuerComer(Transform comida)
    {
        if (comida == null) return false;

        for (int i = todos.Count - 1; i >= 0; i--)
        {
            Pato pato = todos[i];

            if (pato == null)
            {
                todos.RemoveAt(i);
                continue;
            }

            if (pato.alvoComida == comida) return true;
        }

        return false;
    }

    void ReceberChamado(Transform comida, bool ignorarDistancia)
    {
        if (satisfeito || comida == null || comida == comidaIgnorada) return;

        float distancia = Vector2.Distance(PosicaoDoPe(), comida.position);
        if (ignorarDistancia || distancia <= raioDeChamado)
        {
            DefinirAlvo(comida);
        }
    }

    void DefinirAlvo(Transform comida)
    {
        alvoComida = comida;
        alvoAtual = comida;
        tempoEmpacado = 0f;
        menorDistancia = float.MaxValue;
    }

    
    
    
    IEnumerator BuscarComidaPeriodicamente()
    {
        var espera = new WaitForSeconds(intervaloBusca);

        while (true)
        {
            if (!satisfeito && alvoComida == null) ProcurarComida();
            yield return espera;
        }
    }

    void ProcurarComida()
    {
        if (!tagValida) return;

        GameObject[] comidas;

        try
        {
            comidas = GameObject.FindGameObjectsWithTag(tagDaComida);
        }
        catch (UnityException)
        {
            
            
            tagValida = false;
            Debug.LogError($"Pato: a tag '{tagDaComida}' não existe no projeto. " +
                           "Crie em Edit > Project Settings > Tags and Layers e marque a comida com ela.", this);
            return;
        }

        float menorDistanciaEncontrada = raioDeVisao;
        Transform maisPerto = null;

        foreach (GameObject comida in comidas)
        {
            if (comida == null) continue;

            
            if (comida.transform == comidaIgnorada) continue;

            float distancia = Vector2.Distance(PosicaoDoPe(), comida.transform.position);
            if (distancia < menorDistanciaEncontrada)
            {
                menorDistanciaEncontrada = distancia;
                maisPerto = comida.transform;
            }
        }

        DefinirAlvo(maisPerto);   
    }

    
    
    
    IEnumerator RotinaDoPato()
    {
        while (true)
        {
            
            if (alvoComida != null)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            int acao = Random.Range(0, 2); 

            if (acao == 0)
            {
                direcaoMovimento = Vector2.zero;
                yield return new WaitForSeconds(Random.Range(tempoMinParado, tempoMaxParado));
            }
            else
            {
                float angulo = Random.Range(0f, 360f);
                direcaoMovimento = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)).normalized;
                yield return new WaitForSeconds(tempoCaminhando);
            }
        }
    }

    
    
    
    void Update()
    {
        if (alvoComida != null)
        {
            
            
            
            
            Vector2 meuPe = PosicaoDoPe();
            float distancia = Vector2.Distance(meuPe, alvoComida.position);

            
            
            
            if (distancia < menorDistancia - 0.02f)
            {
                menorDistancia = distancia;
                tempoEmpacado = 0f;
            }
            else
            {
                tempoEmpacado += Time.deltaTime;
            }

            if (distancia <= distanciaParaComer)
            {
                ComerAlvo();
            }
            else if (tempoEmpacado >= tempoMaximoEmpacado)
            {
                DesistirDoAlvo();
            }
            else
            {
                
                
                float alturaDoCorpo = transform.position.y - meuPe.y;

                Vector2 destino = new Vector2(alvoComida.position.x,
                                              alvoComida.position.y + alturaDoCorpo);

                direcaoMovimento = (destino - (Vector2)transform.position).normalized;
            }
        }
        else
        {
            tempoEmpacado = 0f;
        }

        AtualizarFuga();
        AtualizarTempoDeVida();

        if (animator != null)
        {
            
            animator.SetFloat("Velocidade", direcaoMovimento.sqrMagnitude);

            if (Mathf.Abs(direcaoMovimento.x) > 0.01f)
            {
                animator.SetFloat("MovX", direcaoMovimento.x);
            }
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = direcaoMovimento * velocidade;
    }

    
    
    
    void AtualizarFuga()
    {
        if (!fogeDoJogador || satisfeito || alvoComida != null) return;

        Player jogador = AcharJogador();
        if (jogador == null) return;

        Vector2 diferenca = (Vector2)transform.position - (Vector2)jogador.transform.position;

        if (diferenca.magnitude > distanciaDeFuga) return;

        direcaoMovimento = diferenca.sqrMagnitude > 0.01f ? diferenca.normalized : Vector2.right;
    }

    void AtualizarTempoDeVida()
    {
        if (tempoDeVida <= 0f || satisfeito || foiEmbora) return;
        if (Time.time - nascimento < tempoDeVida) return;

        IrEmbora();
    }

    void IrEmbora()
    {
        foiEmbora = true;
        satisfeito = true;   

        alvoComida = null;
        alvoAtual = null;

        
        GerenciadorDeJogo.Instancia.RegistrarPatoPerdido();

        ExplosaoDeParticulas.Soltar(transform.position + Vector3.up * alturaDoFeedback,
                                    new Color(1f, 0.85f, 0.35f), 10, 0.9f);
        GerenciadorDeAudio.Instancia.TocarQuack();

        Destroy(gameObject);
    }

    private static Player jogadorCache;

    static Player AcharJogador()
    {
        if (jogadorCache == null) jogadorCache = FindAnyObjectByType<Player>();
        return jogadorCache;
    }

    Vector2 PosicaoDoPe()
    {
        if (spriteRenderer != null)
        {
            return new Vector2(transform.position.x, spriteRenderer.bounds.min.y);
        }

        return transform.position;
    }

    void DesistirDoAlvo()
    {
        comidaIgnorada = alvoComida;

        alvoComida = null;
        alvoAtual = null;
        tempoEmpacado = 0f;
        menorDistancia = float.MaxValue;
        direcaoMovimento = Vector2.zero;

        Debug.Log("Pato desistiu: não conseguiu chegar no milho (caiu em lugar inalcançável?).");
    }

    
    
    
    void ComerAlvo()
    {
        if (estaComendo || satisfeito || alvoComida == null) return;

        estaComendo = true;

        Transform comida = alvoComida;
        alvoComida = null;      
        alvoAtual = null;

        
        
        Collider2D colisorDaComida = comida.GetComponent<Collider2D>();
        if (colisorDaComida != null) colisorDaComida.enabled = false;

        
        TipoDeComida tipoDaComida = ComidaComTipo.TipoDe(comida.gameObject);
        bool ehAFavorita = temComidaFavorita && tipoDaComida == comidaFavorita;

        Destroy(comida.gameObject); 

        
        direcaoMovimento = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        porcoesComidas++;

        
        GerenciadorDeJogo gerenciador = GerenciadorDeJogo.Instancia;

        
        
        int valorDaPorcao = pontosPorPorcao > 0 ? pontosPorPorcao : gerenciador.pontosPorPorcao;

        if (ehAFavorita) valorDaPorcao += bonusDaComidaFavorita;

        int ganho = gerenciador.GanharPontos(valorDaPorcao);

        
        
        MostrarFeedback("+" + ganho, ehAFavorita ? corDaComidaFavorita : corDoNumero, 7, false);

        Debug.Log($"{nomeDaEspecie} comeu {ComidaComTipo.NomeDe(tipoDaComida)}: " +
                  $"{porcoesComidas}/{porcoesNecessarias} porções (+{ganho}).");

        if (porcoesComidas >= porcoesNecessarias)
        {
            FicarSatisfeito();
        }

        estaComendo = false;
    }

    void FicarSatisfeito()
    {
        satisfeito = true;
        alvoComida = null;
        alvoAtual = null;
        direcaoMovimento = Vector2.zero;

        if (pintarQuandoSatisfeito && spriteRenderer != null)
        {
            spriteRenderer.color = corSatisfeito;
        }

        
        int ganhoDoBonus = GerenciadorDeJogo.Instancia.RegistrarPatoSatisfeito();

        
        MostrarFeedback("+" + ganhoDoBonus, corDoBonus, 14, true);
    }

    
    
    
    void MostrarFeedback(string texto, Color cor, int quantidadeDeParticulas, bool ehBonus)
    {
        if (!mostrarFeedback) return;

        Vector3 onde = transform.position + Vector3.up * alturaDoFeedback;

        NumeroFlutuante.Criar(onde, texto, cor, ehBonus ? tamanhoDoNumero * 1.5f : tamanhoDoNumero);

        ExplosaoDeParticulas.Soltar(onde, cor,
                                    ehBonus ? quantidadeDeParticulas * 2 : quantidadeDeParticulas,
                                    ehBonus ? 1.3f : 0.8f);

        GerenciadorDeAudio audio = GerenciadorDeAudio.Instancia;
        audio.TocarComer();
        audio.TocarMoeda();
        if (ehBonus) audio.TocarQuack();

        StartCoroutine(Pulinho(ehBonus ? 0.28f : 0.16f));
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

    
    
    void OnTriggerEnter2D(Collider2D outro)
    {
        if (satisfeito || estaComendo) return;

        if (outro.CompareTag(tagDaComida))
        {
            alvoComida = outro.transform;
            ComerAlvo();
        }
    }

    void OnDrawGizmosSelected()
    {
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, raioDeVisao);

        
        Gizmos.color = new Color(0.2f, 0.6f, 1f);
        Gizmos.DrawWireSphere(transform.position, raioDeChamado);

        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaParaComer);
    }
}
