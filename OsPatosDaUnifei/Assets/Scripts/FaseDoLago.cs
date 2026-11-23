using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;












public class FaseDoLago : MonoBehaviour
{
    [System.Serializable]
    public class Configuracao
    {
        public GameObject patoPrefab;
        public SpriteRenderer areaDoMapa;
        public LayerMask camadasDeObstaculo = ~0;

        public Transform[] waypoints;

        public int totalDePatos = 3;
        public Vector2 centroDaArea = new Vector2(-27f, 0f);
        public Vector2 tamanhoDaArea = new Vector2(9f, 12f);

        public bool mostrarArea = true;
        public float transparenciaDaArea = 0.12f;
        public float transparenciaDaBorda = 0.35f;
        public float transparenciaDoRotulo = 0.7f;
        public bool mostrarRotulo = true;

        public float raioMinimoDeSpawn = 8f;
        public float raioDeSpawn = 28f;
        public float distanciaDeRecrutamento = 6.5f;
        public float distanciaNaFila = 4.8f;
        public float velocidadeDoSeguidor = 34f;

        public int tentativasDeSpawn = 30;
        public float raioDeObstaculo = 2.5f;
    }

    [Header("Visual da área de entrega")]
    public Color corDaArea = new Color(0.35f, 0.80f, 1f, 0.18f);
    public Color corDaBorda = new Color(0.18f, 0.55f, 0.85f, 0.95f);
    public Color corDoRotulo = new Color(0.04f, 0.18f, 0.30f, 0.98f);
    public float espessuraDaBorda = 0.30f;

    [Header("Passeio dos patos")]
    public float velocidadeDePasseio = 6f;
    public float raioDePasseioInicial = 0f;

    [Header("Feedback")]
    public Color corDoRecrutamento = new Color(1f, 0.82f, 0.92f);
    public Color corDaEntrega = new Color(0.62f, 1f, 0.72f);

    public int PatosSpawnados { get; private set; }
    public int Entregues { get; private set; }

    private Configuracao config;
    private Rect areaDeEntrega;

    private readonly List<PatoDeFila> fila = new List<PatoDeFila>();
    private readonly List<PatoDeFila> todos = new List<PatoDeFila>();

    private Transform jogador;
    private GerenciadorDeJogo gerenciador;
    private PatoDeFila conviteAtual;
    private SpriteRenderer areaVisual;

    private bool iniciado;
    private bool terminou;
    private bool parado;

    private static Sprite spriteBranco;

    
    
    
    public int Iniciar(Configuracao novaConfig)
    {
        config = novaConfig;
        if (config == null) return 0;

        jogador = AcharJogador();
        gerenciador = GerenciadorDeJogo.Instancia;

        areaDeEntrega = new Rect(config.centroDaArea - config.tamanhoDaArea * 0.5f,
                                 config.tamanhoDaArea);

        CriarAreaDeEntrega();
        CriarPatos();

        iniciado = true;

        return PatosSpawnados;
    }

    void CriarPatos()
    {
        PatosSpawnados = 0;
        if (config.patoPrefab == null) return;

        Color[] cores =
        {
            Color.white,
            new Color(1f, 0.94f, 0.68f),
            new Color(0.78f, 0.90f, 1f),
            new Color(1f, 0.82f, 0.87f),
            new Color(0.82f, 1f, 0.86f),
        };

        List<Transform> waypointsValidos = new List<Transform>();

        if (config.waypoints != null)
        {
            for (int w = 0; w < config.waypoints.Length; w++)
            {
                if (config.waypoints[w] != null) waypointsValidos.Add(config.waypoints[w]);
            }
        }

        Debug.Log($"FaseDoLago: {waypointsValidos.Count} waypoint(s) preenchido(s) de " +
                  $"{(config.waypoints != null ? config.waypoints.Length : 0)} slot(s).");

        for (int i = 0; i < config.totalDePatos; i++)
        {
            Vector2 ponto;

            if (waypointsValidos.Count > 0)
            {
                int indiceWaypoint = i % waypointsValidos.Count;
                int volta = i / waypointsValidos.Count;
                ponto = PontoDoWaypoint(waypointsValidos[indiceWaypoint], volta);

                if (!LugarEstaLivre(ponto))
                {
                    Debug.LogWarning($"FaseDoLago: o waypoint {indiceWaypoint + 1} " +
                                     $"('{waypointsValidos[indiceWaypoint].name}') não está livre. " +
                                     "O pato pode nascer preso; mova o waypoint.", waypointsValidos[indiceWaypoint]);
                }

                Debug.Log($"FaseDoLago: pato {i + 1} nasceu no waypoint {indiceWaypoint + 1} " +
                          $"('{waypointsValidos[indiceWaypoint].name}') em {ponto}.");
            }
            else if (!TentarSortearPonto(out ponto))
            {
                Debug.LogWarning($"FaseDoLago: não achei lugar livre para o pato {i + 1}. " +
                                 "A fase continua com os que nasceram.");
                continue;
            }
            else
            {
                Debug.Log($"FaseDoLago: pato {i + 1} nasceu no sorteio normal em {ponto}.");
            }

            GameObject patoNovo = Instantiate(config.patoPrefab, ponto, Quaternion.identity);
            patoNovo.name = $"PatoDoLago {i + 1}";
            patoNovo.transform.SetParent(transform, true);

            
            Pato patoAntigo = patoNovo.GetComponent<Pato>();
            if (patoAntigo != null) patoAntigo.enabled = false;

            PrepararFisica(patoNovo);

            PatoDeFila pato = patoNovo.AddComponent<PatoDeFila>();

            Rect passeio = new Rect(ponto.x - raioDePasseioInicial, ponto.y - raioDePasseioInicial,
                                    raioDePasseioInicial * 2f, raioDePasseioInicial * 2f);

            pato.Configurar(config.velocidadeDoSeguidor, config.distanciaNaFila,
                            cores[i % cores.Length], passeio, velocidadeDePasseio);

            todos.Add(pato);
            PatosSpawnados++;
        }
    }

    static void PrepararFisica(GameObject pato)
    {
        Rigidbody2D rb = pato.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        
        
        BoxCollider2D colisor = pato.GetComponent<BoxCollider2D>();
        if (colisor != null)
        {
            colisor.isTrigger = false;
            colisor.size = new Vector2(0.62f, 0.09f);
            colisor.offset = new Vector2(-0.02f, -0.47f);
        }
    }

    
    
    
    void Update()
    {
        if (!iniciado || Time.timeScale <= 0f) return;

        AtualizarAreaVisual();

        if (gerenciador == null) gerenciador = GerenciadorDeJogo.Instancia;
        if (gerenciador == null) return;

        if (!gerenciador.FaseEstaRodando)
        {
            if (gerenciador.UltimaFaseFalhou && !parado)
            {
                parado = true;
                PararTodos();
            }

            return;
        }

        if (terminou) return;
        if (jogador == null) jogador = AcharJogador();
        if (jogador == null) return;

        AtualizarConvite();

        if (Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame)
        {
            TentarRecrutar();
        }

        VerificarEntregas();
    }

    
    
    
    void AtualizarConvite()
    {
        PatoDeFila maisPerto = null;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < todos.Count; i++)
        {
            PatoDeFila pato = todos[i];
            if (pato == null) continue;

            pato.MostrarMarcador(false);

            if (pato.Recrutado || pato.Entregue) continue;

            float distancia = Vector2.Distance(jogador.position, pato.transform.position);

            if (distancia <= config.distanciaDeRecrutamento && distancia < menorDistancia)
            {
                menorDistancia = distancia;
                maisPerto = pato;
            }
        }

        conviteAtual = maisPerto;

        if (conviteAtual != null) conviteAtual.MostrarMarcador(true);
    }

    void TentarRecrutar()
    {
        if (conviteAtual == null) return;

        PatoDeFila pato = conviteAtual;
        conviteAtual = null;

        pato.Recrutar();
        fila.Add(pato);
        AtualizarAlvos();

        if (GerenciadorDeAudio.Instancia != null) GerenciadorDeAudio.Instancia.TocarQuack();

        Vector3 onde = pato.transform.position + Vector3.up * 2.5f;
        NumeroFlutuante.Criar(onde, "SEGUINDO!", corDoRecrutamento, 1.7f);
        ExplosaoDeParticulas.Soltar(onde, corDoRecrutamento, 10, 0.8f);
    }

    void AtualizarAlvos()
    {
        for (int i = 0; i < fila.Count; i++)
        {
            if (fila[i] == null)
            {
                fila.RemoveAt(i);
                i--;
                continue;
            }

            Transform alvo = (i == 0 || jogador == null) ? jogador : fila[i - 1].transform;
            fila[i].DefinirAlvo(alvo);
        }
    }

    
    
    
    void VerificarEntregas()
    {
        for (int i = fila.Count - 1; i >= 0; i--)
        {
            PatoDeFila pato = fila[i];

            if (pato == null)
            {
                fila.RemoveAt(i);
                continue;
            }

            if (areaDeEntrega.Contains((Vector2)pato.transform.position))
            {
                Entregar(pato);
            }
        }
    }

    void Entregar(PatoDeFila pato)
    {
        if (pato == null || pato.Entregue) return;

        fila.Remove(pato);
        pato.Entregar(areaDeEntrega);
        Entregues++;

        AtualizarAlvos();

        if (GerenciadorDeAudio.Instancia != null) GerenciadorDeAudio.Instancia.TocarQuack();

        Vector3 onde = pato.transform.position + Vector3.up * 2.5f;
        NumeroFlutuante.Criar(onde, "NO LAGO!", corDaEntrega, 2f);
        ExplosaoDeParticulas.Soltar(onde, corDaEntrega, 16, 1f);

        if (gerenciador != null) gerenciador.RegistrarPatoSatisfeito();

        if (Entregues >= PatosSpawnados)
        {
            terminou = true;
            Debug.Log($"FaseDoLago: todos os {PatosSpawnados} patos foram entregues!");
        }
    }

    void PararTodos()
    {
        for (int i = 0; i < todos.Count; i++)
        {
            PatoDeFila pato = todos[i];
            if (pato == null) continue;

            pato.MostrarMarcador(false);
            pato.Parar();
            pato.enabled = false;
        }
    }

    
    
    
    Vector2 PontoDoWaypoint(Transform waypoint, int volta)
    {
        Vector2 basePonto = waypoint.position;

        if (volta <= 0) return basePonto;

        float distancia = 1.6f * volta;
        Vector2[] direcoes = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };

        for (int i = 0; i < direcoes.Length; i++)
        {
            Vector2 ponto = basePonto + direcoes[i] * distancia;

            if (LugarEstaLivre(ponto)) return ponto;
        }

        return basePonto;
    }

    bool TentarSortearPonto(out Vector2 resultado)
    {
        Vector2 centro = jogador != null ? (Vector2)jogador.position : Vector2.zero;
        Rect mapa = AreaDoMapa();

        int tentativas = Mathf.Max(1, config.tentativasDeSpawn);

        for (int i = 0; i < tentativas; i++)
        {
            float angulo = Random.Range(0f, Mathf.PI * 2f);
            float raio = Random.Range(config.raioMinimoDeSpawn,
                                      Mathf.Max(config.raioMinimoDeSpawn, config.raioDeSpawn));

            Vector2 ponto = centro + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * raio;

            if (!mapa.Contains(ponto)) continue;
            if (areaDeEntrega.Contains(ponto)) continue;
            if (!LugarEstaLivre(ponto)) continue;

            resultado = ponto;
            return true;
        }

        
        Vector2[] direcoes = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };

        for (int i = 0; i < direcoes.Length; i++)
        {
            Vector2 ponto = centro + direcoes[i] * 7f;

            if (mapa.Contains(ponto) && !areaDeEntrega.Contains(ponto) && LugarEstaLivre(ponto))
            {
                resultado = ponto;
                return true;
            }
        }

        
        
        
        Vector2 basePertoDoLago = config.centroDaArea +
                                  Vector2.right * (config.tamanhoDaArea.x * 0.5f + 5f);

        for (int i = 0; i < 16; i++)
        {
            Vector2 ponto = basePertoDoLago + new Vector2(Random.Range(-8f, 8f),
                                                          Random.Range(-9f, 9f));

            if (mapa.Contains(ponto) && !areaDeEntrega.Contains(ponto) && LugarEstaLivre(ponto))
            {
                resultado = ponto;
                return true;
            }
        }

        resultado = Vector2.zero;
        return false;
    }

    bool LugarEstaLivre(Vector2 ponto)
    {
        Physics2D.SyncTransforms();

        Collider2D[] achados = Physics2D.OverlapCircleAll(ponto, config.raioDeObstaculo,
                                                         config.camadasDeObstaculo);

        for (int i = 0; i < achados.Length; i++)
        {
            Collider2D achado = achados[i];
            if (achado == null) continue;

            
            
            
            if (achado.isTrigger) continue;

            return false;
        }

        return true;
    }

    Rect AreaDoMapa()
    {
        if (config.areaDoMapa == null) return new Rect(-40f, -25f, 80f, 50f);

        Bounds limites = config.areaDoMapa.bounds;
        float margem = 3f;

        Rect area = new Rect(limites.min.x + margem, limites.min.y + margem,
                             limites.size.x - margem * 2f, limites.size.y - margem * 2f);

        if (area.width < 10f) area = new Rect(limites.min.x, limites.min.y, limites.size.x, limites.size.y);

        return area;
    }

    
    
    
    void CriarAreaDeEntrega()
    {
        
        
        
        if (!config.mostrarArea) return;

        GameObject area = new GameObject("AreaDoLago");
        area.transform.SetParent(transform, false);
        area.transform.position = config.centroDaArea;

        int camada = SortingLayer.NameToID("Objetos");

        Vector2 tamanho = config.tamanhoDaArea;
        float largura = Mathf.Max(1f, tamanho.x);
        float altura = Mathf.Max(1f, tamanho.y);

        Color corFundo = corDaArea;
        corFundo.a = Mathf.Clamp01(config.transparenciaDaArea);

        Color corBorda = corDaBorda;
        corBorda.a = Mathf.Clamp01(config.transparenciaDaBorda);

        GameObject fundo = CriarRetangulo(area.transform, "Fundo", Vector2.zero,
                                          new Vector2(largura, altura), corFundo, camada, -95);

        areaVisual = fundo.GetComponent<SpriteRenderer>();

        float espessura = Mathf.Clamp(espessuraDaBorda, 0.05f, Mathf.Min(largura, altura) * 0.2f);

        CriarRetangulo(area.transform, "BordaCima",
                       new Vector2(0f, altura * 0.5f - espessura * 0.5f),
                       new Vector2(largura, espessura), corBorda, camada, -94);

        CriarRetangulo(area.transform, "BordaBaixo",
                       new Vector2(0f, -altura * 0.5f + espessura * 0.5f),
                       new Vector2(largura, espessura), corBorda, camada, -94);

        CriarRetangulo(area.transform, "BordaEsquerda",
                       new Vector2(-largura * 0.5f + espessura * 0.5f, 0f),
                       new Vector2(espessura, altura), corBorda, camada, -94);

        CriarRetangulo(area.transform, "BordaDireita",
                       new Vector2(largura * 0.5f - espessura * 0.5f, 0f),
                       new Vector2(espessura, altura), corBorda, camada, -94);

        if (config.mostrarRotulo)
        {
            CriarRotulo(area.transform, new Vector3(0f, altura * 0.5f + 2.2f, 0f), camada);
        }
    }

    GameObject CriarRetangulo(Transform pai, string nome, Vector2 centroLocal, Vector2 tamanho,
                              Color cor, int camada, int ordem)
    {
        GameObject objeto = new GameObject(nome);
        objeto.transform.SetParent(pai, false);
        objeto.transform.localPosition = new Vector3(centroLocal.x, centroLocal.y, 0f);

        SpriteRenderer desenho = objeto.AddComponent<SpriteRenderer>();
        desenho.sprite = SpriteBranco();
        desenho.color = cor;
        desenho.sortingLayerID = camada;
        desenho.sortingOrder = ordem;

        float lado = desenho.sprite.bounds.size.x;
        if (lado < 0.0001f) lado = 1f;

        objeto.transform.localScale = new Vector3(tamanho.x / lado, tamanho.y / lado, 1f);

        return objeto;
    }

    void CriarRotulo(Transform pai, Vector3 posicaoLocal, int camada)
    {
        GameObject objeto = new GameObject("RotuloLago");
        objeto.transform.SetParent(pai, false);
        objeto.transform.localPosition = posicaoLocal;

        TextMeshPro texto = objeto.AddComponent<TextMeshPro>();
        texto.text = "LAGO";

        Color corDoTexto = corDoRotulo;
        corDoTexto.a = Mathf.Clamp01(config.transparenciaDoRotulo);
        texto.color = corDoTexto;
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;
        texto.fontSize = 36f;

        TMP_FontAsset fonte = TMP_Settings.defaultFontAsset;
        if (fonte == null)
        {
            TextMeshProUGUI algumTexto = FindAnyObjectByType<TextMeshProUGUI>();
            if (algumTexto != null) fonte = algumTexto.font;
        }

        if (fonte != null) texto.font = fonte;

        texto.sortingLayerID = camada;
        texto.sortingOrder = 500;

        texto.ForceMeshUpdate();

        float alturaDoTexto = texto.textBounds.size.y;
        if (alturaDoTexto > 0.0001f)
        {
            objeto.transform.localScale = Vector3.one * (2.4f / alturaDoTexto);
        }
    }

    void AtualizarAreaVisual()
    {
        if (areaVisual == null) return;

        Color cor = areaVisual.color;
        cor.a = Mathf.Clamp01(config.transparenciaDaArea);
        areaVisual.color = cor;
    }

    static Sprite SpriteBranco()
    {
        if (spriteBranco == null)
        {
            spriteBranco = ArteProcedural.Criar(new string[]
            {
                "WWWW",
                "WWWW",
                "WWWW",
                "WWWW",
            }, 16f);
        }

        return spriteBranco;
    }

    static Transform AcharJogador()
    {
        Player jogador = FindAnyObjectByType<Player>();
        return jogador != null ? jogador.transform : null;
    }
}
