using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class InventarioPlayer : MonoBehaviour
{
    public int[] quantidades = new int[3];
    public int capacidadeMaxima = 20;
    public TipoDeComida tipoSelecionado = TipoDeComida.Milho;
    public GameObject comidaPrefab;
    public float distanciaDoArremesso = 4.5f;
    public float distanciaMinimaDoArremesso = 3.5f;
    public float intervaloEntreArremessos = 0.25f;
    public bool paoChamaTodosOsPatos = true;

    [Header("Recolher comida do chão")]
    [Tooltip("Além do trigger, o jogador puxa o milho que estiver neste raio do pé. " +
             "É a rede de segurança para o milho nunca ficar impossível de pegar.")]
    public float raioDeRecolhimento = 2.8f;

    [Tooltip("De quanto em quanto tempo procura milho perto do pé")]
    public float intervaloDeChecagem = 0.12f;

    [Tooltip("Apertar E perto de uma comida que você jogou (e que nenhum pato está indo comer) devolve ela para a mochila")]
    public bool permitirRecolherComidaJogada = true;

    [Header("Comida jogada com F")]
    [Tooltip("Raio do E para pegar de volta. O arremesso cai a 4,5; por isso 5,5.")]
    public float raioDeRecolhimentoJogada = 5.5f;

    [Tooltip("Se nenhum pato estiver indo comer, a comida volta sozinha depois deste tempo")]
    public float tempoParaDevolverComidaJogada = 4f;

    public bool devolverComidaJogadaSozinha = true;

    private class ComidaJogadaInfo
    {
        public GameObject objeto;
        public float jogadaEm;
        public TipoDeComida tipo;
    }

    private readonly List<ComidaJogadaInfo> comidasJogadas = new List<ComidaJogadaInfo>();
    private bool jaAvisouRecolher;

    private Vector2 ultimaDirecao = Vector2.down;
    private float podeJogarEm;
    private Rigidbody2D rb;
    private SpriteRenderer desenho;
    private Collider2D colisor;
    private float proximaChecagem;
    private float proximoAvisoMochilaCheia;

    public int quantidadeDeComida => QuantidadeDe(tipoSelecionado);

    public int QuantidadeDe(TipoDeComida tipo)
    {
        int indice = (int)tipo;

        if (quantidades == null || indice < 0 || indice >= quantidades.Length) return 0;

        return quantidades[indice];
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        desenho = GetComponent<SpriteRenderer>();
        colisor = GetComponent<Collider2D>();

        
        if (raioDeRecolhimento <= 0.1f) raioDeRecolhimento = 2.8f;
        if (intervaloDeChecagem <= 0.01f) intervaloDeChecagem = 0.12f;
        if (raioDeRecolhimentoJogada <= 0.1f) raioDeRecolhimentoJogada = 5.5f;
        if (tempoParaDevolverComidaJogada <= 0.01f) tempoParaDevolverComidaJogada = 4f;

        
        if (quantidades == null || quantidades.Length < 3)
        {
            int[] novas = new int[3];
            if (quantidades != null)
            {
                for (int i = 0; i < quantidades.Length && i < novas.Length; i++) novas[i] = quantidades[i];
            }
            quantidades = novas;
        }

        
        
        if (distanciaDoArremesso < distanciaMinimaDoArremesso)
        {
            Debug.LogWarning($"InventarioPlayer: 'Distancia Do Arremesso' estava em {distanciaDoArremesso}, " +
                             $"que é menor que o corpo do jogador — a comida cairia debaixo dele. " +
                             $"Usei {distanciaMinimaDoArremesso}.", this);

            distanciaDoArremesso = distanciaMinimaDoArremesso;
        }
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;

        
        
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            ultimaDirecao = rb.linearVelocity.normalized;
        }

        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        
        
        if (Time.time >= proximaChecagem)
        {
            proximaChecagem = Time.time + Mathf.Max(0.03f, intervaloDeChecagem);
            TentarRecolherPerto();
            AtualizarComidasJogadas();
        }

        
        if (teclado.digit1Key.wasPressedThisFrame) Selecionar(TipoDeComida.Milho);
        if (teclado.digit2Key.wasPressedThisFrame) Selecionar(TipoDeComida.Alface);
        if (teclado.digit3Key.wasPressedThisFrame) Selecionar(TipoDeComida.Pao);

        if (teclado.fKey.wasPressedThisFrame) JogarComida();

        
        if (permitirRecolherComidaJogada && teclado.eKey.wasPressedThisFrame)
        {
            TentarRecolherComidaJogada();
        }
    }

    void Selecionar(TipoDeComida tipo)
    {
        if (tipoSelecionado == tipo) return;

        tipoSelecionado = tipo;
        GerenciadorDeAudio.Instancia.TocarMoeda();

        Debug.Log($"Comida selecionada: {ComidaComTipo.NomeDe(tipo)} ({QuantidadeDe(tipo)} na mochila).");
    }

    void JogarComida()
    {
        if (Time.time < podeJogarEm) return;

        TipoDeComida tipo = tipoSelecionado;

        if (QuantidadeDe(tipo) <= 0)
        {
            GerenciadorDeAudio.Instancia.TocarErro();
            Debug.Log($"Mochila sem {ComidaComTipo.NomeDe(tipo)}! Pegue comida no mapa " +
                      "(as teclas 1, 2 e 3 trocam o tipo que você joga).");
            return;
        }

        if (comidaPrefab == null)
        {
            Debug.LogError("InventarioPlayer: arraste o MIlho.prefab no campo 'Comida Prefab' do Inspector.", this);
            return;
        }

        quantidades[(int)tipo]--;
        podeJogarEm = Time.time + intervaloEntreArremessos;

        
        
        
        Vector2 chao = PosicaoDoChao();

        float distancia = Mathf.Max(distanciaDoArremesso, distanciaMinimaDoArremesso);
        Vector2 posicaoDrop = chao + ultimaDirecao * distancia;

        GameObject comidaJogada = Instantiate(comidaPrefab, posicaoDrop, Quaternion.identity);

        
        
        comidaJogada.tag = "ComidaPato";

        
        ComidaComTipo.Marcar(comidaJogada, tipo);

        
        bool chamaTodos = paoChamaTodosOsPatos && tipo == TipoDeComida.Pao;
        Pato.ChamarPatos(comidaJogada.transform, chamaTodos);

        comidasJogadas.Add(new ComidaJogadaInfo
        {
            objeto = comidaJogada,
            jogadaEm = Time.time,
            tipo = tipo,
        });

        if (!jaAvisouRecolher)
        {
            jaAvisouRecolher = true;
            NumeroFlutuante.Criar(PosicaoDoChao() + Vector2.up * 3f,
                                  "E pega de volta",
                                  new Color(1f, 0.92f, 0.45f), 1.5f);
        }

        Debug.Log($"{ComidaComTipo.NomeDe(tipo)} jogado no chão! " +
                  $"Restam {QuantidadeDe(tipo)} na mochila.");
    }

    Vector2 PosicaoDoChao()
    {
        if (desenho != null)
        {
            return new Vector2(transform.position.x, desenho.bounds.min.y);
        }

        if (colisor != null)
        {
            return new Vector2(transform.position.x, colisor.bounds.center.y);
        }

        return transform.position;
    }

    
    
    
    void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro == null) return;
        if (!outro.CompareTag("ComidaMapa")) return;

        Recolher(outro);
    }

    void TentarRecolherPerto()
    {
        Vector2 chao = PosicaoDoChao();
        Collider2D[] achados = Physics2D.OverlapCircleAll(chao, raioDeRecolhimento);

        for (int i = 0; i < achados.Length; i++)
        {
            Collider2D achado = achados[i];

            if (achado == null) continue;
            if (!achado.CompareTag("ComidaMapa")) continue;

            Recolher(achado);
        }
    }

    void TentarRecolherComidaJogada()
    {
        Vector2 chao = PosicaoDoChao();
        Collider2D[] achados = Physics2D.OverlapCircleAll(chao, raioDeRecolhimentoJogada);

        bool temComida = false;

        for (int i = 0; i < achados.Length; i++)
        {
            Collider2D achado = achados[i];

            if (achado == null) continue;
            if (!achado.CompareTag("ComidaPato")) continue;

            temComida = true;

            Recolher(achado);
            return;
        }

        if (!temComida)
        {
            NumeroFlutuante.Criar(PosicaoDoChao() + Vector2.up * 2.8f,
                                  "Nenhuma comida jogada perto",
                                  new Color(0.85f, 0.9f, 1f), 1.4f);
        }
    }

    void AtualizarComidasJogadas()
    {
        if (!devolverComidaJogadaSozinha) return;

        for (int i = comidasJogadas.Count - 1; i >= 0; i--)
        {
            ComidaJogadaInfo info = comidasJogadas[i];

            if (info == null || info.objeto == null)
            {
                comidasJogadas.RemoveAt(i);
                continue;
            }

            Collider2D colisorComida = info.objeto.GetComponent<Collider2D>();

            if (colisorComida == null || !colisorComida.enabled)
            {
                comidasJogadas.RemoveAt(i);
                continue;
            }

            if (Time.time - info.jogadaEm < tempoParaDevolverComidaJogada) continue;
            if (Pato.AlguemQuerComer(info.objeto.transform)) continue;
            if (QuantidadeDe(info.tipo) >= capacidadeMaxima) continue;

            DevolverComidaJogada(info);
            comidasJogadas.RemoveAt(i);
        }
    }

    void DevolverComidaJogada(ComidaJogadaInfo info)
    {
        quantidades[(int)info.tipo]++;

        GerenciadorDeAudio.Instancia.TocarMoeda();

        Vector3 onde = PosicaoDoChao() + Vector2.up * 2.6f;
        NumeroFlutuante.Criar(onde, "Comida de volta!",
                              ComidaComTipo.CorDe(info.tipo), 1.7f);

        Collider2D colisorComida = info.objeto.GetComponent<Collider2D>();
        if (colisorComida != null) colisorComida.enabled = false;

        Destroy(info.objeto);
    }

    void Recolher(Collider2D comida)
    {
        if (comida == null) return;
        if (!comida.enabled) return;   

        TipoDeComida tipo = ComidaComTipo.TipoDe(comida.gameObject);
        int indice = (int)tipo;

        if (quantidades == null || indice < 0 || indice >= quantidades.Length) return;

        if (quantidades[indice] >= capacidadeMaxima)
        {
            AvisarMochilaCheia(tipo);
            return;
        }

        quantidades[indice]++;

        GerenciadorDeAudio.Instancia.TocarMoeda();

        Vector3 onde = comida.transform.position + Vector3.up * 1.2f;
        NumeroFlutuante.Criar(onde, "+1", ComidaComTipo.CorDe(tipo), 1.5f);

        
        comida.enabled = false;
        Destroy(comida.gameObject);
    }

    void AvisarMochilaCheia(TipoDeComida tipo)
    {
        if (Time.time < proximoAvisoMochilaCheia) return;

        proximoAvisoMochilaCheia = Time.time + 2.5f;

        GerenciadorDeAudio.Instancia.TocarErro();

        Vector3 onde = PosicaoDoChao() + Vector2.up * 2.6f;
        NumeroFlutuante.Criar(onde, "Mochila cheia!", new Color(1f, 0.85f, 0.35f), 1.8f);

        Debug.Log($"Mochila cheia de {ComidaComTipo.NomeDe(tipo)} ({capacidadeMaxima}). " +
                  "Jogue um pouco com F (ou aperte E para pegar a comida jogada de volta).");
    }
}
