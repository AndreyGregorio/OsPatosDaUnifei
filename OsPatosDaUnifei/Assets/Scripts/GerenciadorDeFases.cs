using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[System.Serializable]
public class EspecieDePato
{
    public string nome = "Comum";
    public float peso = 1f;
    public Color cor = Color.white;
    public float multiplicadorDeVelocidade = 1f;
    public int porcoesExtras = 0;
    public int pontosPorPorcao = 0;
    public bool fogeDoJogador = false;
    public float distanciaDeFuga = 22f;
    public float tempoDeVida = 0f;
    public bool temComidaFavorita = false;
    public TipoDeComida comidaFavorita = TipoDeComida.Alface;
    public int bonusDaComidaFavorita = 15;
}

[System.Serializable]
public class ComidaDoMapa
{
    public TipoDeComida tipo = TipoDeComida.Milho;
    public float peso = 1f;
}




public enum TipoDeFase
{
    AlimentarPatos = 0,
    LevarPatosAoLago = 1,
}




[System.Serializable]
public class ConfiguracaoDeFase
{
    [Tooltip("Só para você se achar no Inspector")]
    public string nome = "Fase";

    public TipoDeFase tipo = TipoDeFase.AlimentarPatos;

    [Tooltip("Quantos patos participar da fase")]
    public int patos = 1;

    [Tooltip("Só para Alimentar: porções que CADA pato precisa comer")]
    public int porcoesPorPato = 2;

    [Tooltip("Só para Alimentar: quantos milhos nascem no mapa")]
    public int comidaNoMapa = 8;

    [Tooltip("Só para Levar ao Lago: tempo em segundos. 0 = sem limite.")]
    public float tempoLimite = 0f;

    [Tooltip("Tempo para ganhar as 3 estrelas. 0 = usa a curva antiga do GerenciadorDeJogo.")]
    public float tempoTresEstrelas = 0f;
}

public class GerenciadorDeFases : MonoBehaviour
{
    public enum ModoDeFases
    {
        Configuradas = 0,   
        FormulaAntiga = 1,  
    }

    [Header("Prefabs")]
    public GameObject comidaPrefab;
    public GameObject patoPrefab;

    [Header("Onde os objetos aparecem")]
    public BoxCollider2D areaDeSpawn;
    public SpriteRenderer areaDoMapa;
    public Vector2 margemInterna = new Vector2(3f, 3f);

    [Tooltip("Nasce perto do jogador em vez de espalhar pelo mapa inteiro. É o que faz o milho ser encontrável.")]
    public bool nascerPertoDoJogador = true;

    [Tooltip("Distância máxima do jogador em que o milho/pato pode nascer")]
    public float raioDeSpawnDoJogador = 28f;

    [Tooltip("Distância mínima do jogador (para não nascer dentro dele)")]
    public float raioMinimoDeSpawnDoJogador = 8f;

    [Header("Não nascer dentro de obstáculo (lago, cerca...)")]
    public bool evitarObstaculos = true;

    [Tooltip("Raio do teste para o milho. O milho tem ~1,8 de largura")]
    public float raioDaChecagem = 1.2f;

    [Tooltip("Raio do teste para o pato. O pato tem ~4 de largura")]
    public float raioDaChecagemDoPato = 2.5f;

    public int tentativasDeSpawn = 30;
    public LayerMask camadasDeObstaculo = ~0;

    [Header("Waypoints: onde os patos nascem")]
    [Tooltip("Arraste objetos vazios para cá, na ordem. O 1º pato nasce no 1º waypoint preenchido, o 2º no 2º, etc. Slots vazios são ignorados. Usado na fase do lago.")]
    public Transform[] waypointsDeNascimentoDosPatos = new Transform[0];

    [Header("Fases e dificuldades")]
    [Tooltip("Configuradas = usa a lista abaixo. Fórmula Antiga = a progressão automática de antes.")]
    public ModoDeFases modoDeFases = ModoDeFases.Configuradas;

    [Tooltip("As fases do jogo, na ordem. Depois da última, elas se repetem mais difíceis.")]
    public ConfiguracaoDeFase[] fases = FasesPadrao();

    [Header("Progressão antiga (só se Modo = Fórmula Antiga)")]
    public int patosNaPrimeiraFase = 1;
    public EspecieDePato[] especies = new EspecieDePato[]
    {
        new EspecieDePato
        {
            nome = "Comum", peso = 65f, cor = Color.white, multiplicadorDeVelocidade = 1f,
        },
        new EspecieDePato
        {
            nome = "Guloso", peso = 20f, cor = new Color(1f, 0.78f, 0.45f),
            multiplicadorDeVelocidade = 0.7f, porcoesExtras = 1, pontosPorPorcao = 20,
            temComidaFavorita = true, comidaFavorita = TipoDeComida.Alface, bonusDaComidaFavorita = 20,
        },
        new EspecieDePato
        {
            nome = "Fujão", peso = 15f, cor = new Color(0.65f, 0.85f, 1f),
            multiplicadorDeVelocidade = 1.5f, pontosPorPorcao = 20,
            fogeDoJogador = true, distanciaDeFuga = 22f,
        },
        new EspecieDePato
        {
            nome = "Dourado", peso = 3f, cor = new Color(1f, 0.90f, 0.35f),
            multiplicadorDeVelocidade = 1.3f, pontosPorPorcao = 60, tempoDeVida = 25f,
        },
    };
    public ComidaDoMapa[] comidas = new ComidaDoMapa[]
    {
        new ComidaDoMapa { tipo = TipoDeComida.Milho,  peso = 70f },
        new ComidaDoMapa { tipo = TipoDeComida.Alface, peso = 22f },
        new ComidaDoMapa { tipo = TipoDeComida.Pao,    peso = 8f  },
    };
    public int patosGanhosPorFase = 1;
    public int maximoDePatos = 8;
    public int porcoesPorPatoNaPrimeiraFase = 2;
    public int porcoesGanhasPorFase = 1;
    public int comidaNoMapaNaPrimeiraFase = 8;
    public int comidaGanhaPorFase = 4;
    public int maximoDeComidaNoMapa = 30;
    public float esperaEntreFases = 2.5f;
    public bool mostrarAreaDeSpawn = true;

    [Header("Fase do lago")]
    [Tooltip("Centro da área de entrega, na parte de TERRA do lago. Ajuste no Scene view pelo Gizmo azul.")]
    public Vector2 centroDaAreaDeEntrega = new Vector2(-27f, 0f);

    [Tooltip("Tamanho da área de entrega (largura x altura). Usado só quando o objeto da área não está definido.")]
    public Vector2 tamanhoDaAreaDeEntrega = new Vector2(9f, 12f);

    [Tooltip("OPCIONAL e recomendado: um objeto com BoxCollider2D (Is Trigger) que define a área. " +
             "Se estiver preenchido, ele manda no centro e no tamanho. " +
             "Atalho: no componente, ⋮ > Criar/Selecionar Área do Lago.")]
    public BoxCollider2D areaDeEntregaDoLago;

    [Header("Visual da área de entrega (o que aparece no jogo)")]
    [Tooltip("Desligue para a área ficar invisível no jogo. O Gizmo azul continua no Scene view.")]
    public bool mostrarAreaDeEntrega = true;

    [Range(0f, 1f)]
    [Tooltip("Transparência do preenchimento. 0 = invisível, 1 = opaco")]
    public float transparenciaDaArea = 0.12f;

    [Range(0f, 1f)]
    [Tooltip("Transparência da borda. 0 = invisível, 1 = opaco")]
    public float transparenciaDaBorda = 0.35f;

    [Range(0f, 1f)]
    [Tooltip("Transparência do texto LAGO")]
    public float transparenciaDoRotulo = 0.7f;

    [Tooltip("Mostra o texto LAGO em cima da área")]
    public bool mostrarRotuloLago = true;

    [Tooltip("Distância do jogador para poder apertar S e chamar o pato")]
    public float distanciaDeRecrutamento = 6.5f;

    [Tooltip("Distância entre um pato e o da frente na fila")]
    public float distanciaNaFila = 4.8f;

    [Tooltip("Velocidade do pato seguindo. Tem que ser maior que a do jogador (25) para a fila não esticar.")]
    public float velocidadeDoSeguidor = 34f;

    [Header("Atalhos de teste")]
    [Tooltip("F9: pula para a próxima fase")]
    public bool atalhoDeTestePulaFase = true;

    [Tooltip("F10: vai direto para a primeira fase do lago")]
    public bool atalhoDeTesteFaseDoLago = true;

    [SerializeField] private int faseAtual = 0;   
    [SerializeField] private int patosDaFase = 0;
    [SerializeField] private int porcoesDaFase = 0;
    [SerializeField] private int comidaDaFase = 0;

    private bool trocandoDeFase;
    private bool jaAvisouAreaCheia;
    private GerenciadorDeJogo gerenciador;
    private FaseDoLago faseDoLago;
    private static Player jogadorCache;

    
    int PatosDaFase(int indice) => Mathf.Min(patosNaPrimeiraFase + patosGanhosPorFase * indice, maximoDePatos);
    int PorcoesPorPato(int indice) => porcoesPorPatoNaPrimeiraFase + porcoesGanhasPorFase * indice;
    int ComidaDoMapa(int indice) => Mathf.Min(comidaNoMapaNaPrimeiraFase + comidaGanhaPorFase * indice, maximoDeComidaNoMapa);

    
    
    
    static ConfiguracaoDeFase[] FasesPadrao()
    {
        return new ConfiguracaoDeFase[]
        {
            new ConfiguracaoDeFase
            {
                nome = "Primeiros patos", tipo = TipoDeFase.AlimentarPatos,
                patos = 1, porcoesPorPato = 2, comidaNoMapa = 8, tempoTresEstrelas = 45f,
            },
            new ConfiguracaoDeFase
            {
                nome = "A caminho do lago", tipo = TipoDeFase.LevarPatosAoLago,
                patos = 3, tempoLimite = 90f, tempoTresEstrelas = 55f,
            },
            new ConfiguracaoDeFase
            {
                nome = "Fome de verdade", tipo = TipoDeFase.AlimentarPatos,
                patos = 3, porcoesPorPato = 4, comidaNoMapa = 16, tempoTresEstrelas = 75f,
            },
            new ConfiguracaoDeFase
            {
                nome = "Fila indiana", tipo = TipoDeFase.LevarPatosAoLago,
                patos = 4, tempoLimite = 105f, tempoTresEstrelas = 65f,
            },
            new ConfiguracaoDeFase
            {
                nome = "Bando grande", tipo = TipoDeFase.AlimentarPatos,
                patos = 5, porcoesPorPato = 5, comidaNoMapa = 22, tempoTresEstrelas = 100f,
            },
            new ConfiguracaoDeFase
            {
                nome = "Todo mundo no lago", tipo = TipoDeFase.LevarPatosAoLago,
                patos = 6, tempoLimite = 130f, tempoTresEstrelas = 85f,
            },
        };
    }

    void Awake()
    {
        GarantirFases();
        GarantirValoresPadrao();
    }

    
    
    
    
    void GarantirValoresPadrao()
    {
        if (raioDeSpawnDoJogador <= 0.1f) raioDeSpawnDoJogador = 28f;
        if (raioMinimoDeSpawnDoJogador <= 0.1f) raioMinimoDeSpawnDoJogador = 8f;
        if (raioMinimoDeSpawnDoJogador >= raioDeSpawnDoJogador)
        {
            raioMinimoDeSpawnDoJogador = Mathf.Min(8f, raioDeSpawnDoJogador * 0.3f);
        }

        if (distanciaDeRecrutamento <= 0.1f) distanciaDeRecrutamento = 6.5f;
        if (distanciaNaFila <= 0.1f) distanciaNaFila = 4.8f;
        if (velocidadeDoSeguidor <= 0.1f) velocidadeDoSeguidor = 34f;

        if (tamanhoDaAreaDeEntrega.x <= 0.1f || tamanhoDaAreaDeEntrega.y <= 0.1f)
        {
            centroDaAreaDeEntrega = new Vector2(-27f, 0f);
            tamanhoDaAreaDeEntrega = new Vector2(9f, 12f);
        }

        if (tentativasDeSpawn <= 0) tentativasDeSpawn = 30;
    }

    void Start()
    {
        gerenciador = GerenciadorDeJogo.Instancia;
        gerenciador.AoCompletarFase += QuandoCompletarFase;

        ComecarFase(0);
    }

    void OnDestroy()
    {
        if (gerenciador != null) gerenciador.AoCompletarFase -= QuandoCompletarFase;

        if (faseDoLago != null && faseDoLago.gameObject != null)
        {
            Destroy(faseDoLago.gameObject);
        }
    }

    void GarantirFases()
    {
        if (fases == null || fases.Length == 0)
        {
            fases = FasesPadrao();
        }
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (Keyboard.current == null) return;

        
        
        if (atalhoDeTestePulaFase && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            Debug.Log("[Teste] Pulando para a próxima fase com F9.");
            ComecarFase(faseAtual + 1);
            return;
        }

        if (atalhoDeTesteFaseDoLago && Keyboard.current.f10Key.wasPressedThisFrame)
        {
            int indice = IndiceDaPrimeiraFaseDoLago();
            Debug.Log($"[Teste] Pulando para a fase do lago (fase {indice + 1}) com F10.");
            ComecarFase(indice);
            return;
        }

        
        if (gerenciador != null && gerenciador.UltimaFaseFalhou && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("[Fase] Tentando de novo com R.");
            ComecarFase(faseAtual);
        }
    }

    void QuandoCompletarFase()
    {
        if (trocandoDeFase) return;

        trocandoDeFase = true;

        GerenciadorDeAudio.Instancia.TocarFaseCompleta();

        StartCoroutine(EsperarEProximaFase(faseAtual + 1));
    }

    IEnumerator EsperarEProximaFase(int proximaFase)
    {
        yield return new WaitForSeconds(esperaEntreFases);

        
        if (trocandoDeFase) ComecarFase(proximaFase);
    }

    public void ComecarFase(int indice)
    {
        if (indice < 0) indice = 0;

        trocandoDeFase = false;
        faseAtual = indice;

        LimparFase();

        gerenciador = GerenciadorDeJogo.Instancia;
        GarantirFases();

        ConfiguracaoDeFase config = ConfiguracaoDaFase(indice);

        if (config.tipo == TipoDeFase.LevarPatosAoLago)
        {
            IniciarFaseDoLago(indice, config);
        }
        else
        {
            IniciarFaseDeComida(indice, config);
        }
    }

    
    
    
    List<Transform> WaypointsValidos()
    {
        List<Transform> lista = new List<Transform>();

        if (waypointsDeNascimentoDosPatos == null) return lista;

        for (int i = 0; i < waypointsDeNascimentoDosPatos.Length; i++)
        {
            if (waypointsDeNascimentoDosPatos[i] != null) lista.Add(waypointsDeNascimentoDosPatos[i]);
        }

        return lista;
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

            if (!evitarObstaculos || LugarEstaVazio(ponto, raioDaChecagemDoPato))
            {
                return ponto;
            }
        }

        return basePonto;
    }

    void IniciarFaseDeComida(int indice, ConfiguracaoDeFase config)
    {
        patosDaFase = Mathf.Clamp(config.patos, 0, Mathf.Max(0, maximoDePatos));
        porcoesDaFase = Mathf.Max(1, config.porcoesPorPato);
        comidaDaFase = Mathf.Clamp(config.comidaNoMapa, 0, Mathf.Max(0, maximoDeComidaNoMapa));

        List<Transform> waypoints = WaypointsValidos();

        Debug.Log($"GerenciadorDeFases: {waypoints.Count} waypoint(s) preenchido(s) para os patos da fase {indice + 1}.");

        int patosCriados = 0;

        for (int i = 0; i < patosDaFase; i++)
        {
            GameObject patoNovo;

            if (waypoints.Count > 0)
            {
                int indiceWaypoint = i % waypoints.Count;
                int volta = i / waypoints.Count;
                Vector2 ponto = PontoDoWaypoint(waypoints[indiceWaypoint], volta);

                if (evitarObstaculos && !LugarEstaVazio(ponto, raioDaChecagemDoPato))
                {
                    Debug.LogWarning($"GerenciadorDeFases: o waypoint {indiceWaypoint + 1} " +
                                     $"('{waypoints[indiceWaypoint].name}') não está livre. " +
                                     "O pato pode nascer preso; mova o waypoint.", waypoints[indiceWaypoint]);
                }

                patoNovo = patoPrefab != null
                    ? Instantiate(patoPrefab, ponto, Quaternion.identity)
                    : null;

                if (patoNovo != null)
                {
                    Debug.Log($"GerenciadorDeFases: pato {i + 1} nasceu no waypoint " +
                              $"{indiceWaypoint + 1} em {ponto}.");
                }
            }
            else
            {
                patoNovo = GerarObjetoAleatorio(patoPrefab, raioDaChecagemDoPato);

                if (patoNovo != null)
                {
                    Debug.Log($"GerenciadorDeFases: pato {i + 1} nasceu no sorteio normal " +
                              $"em {patoNovo.transform.position}.");
                }
            }

            if (patoNovo == null) continue;

            patosCriados++;

            Pato pato = patoNovo.GetComponent<Pato>();
            if (pato != null)
            {
                pato.porcoesNecessarias = porcoesDaFase;
                pato.porcoesComidas = 0;
                pato.satisfeito = false;

                AplicarEspecie(pato, SortearEspecie());
            }
        }

        
        patosDaFase = patosCriados;

        for (int i = 0; i < comidaDaFase; i++)
        {
            GameObject comidaNova = GerarObjetoAleatorio(comidaPrefab, raioDaChecagem);
            if (comidaNova == null) continue;

            ComidaDoMapa escolhida = SortearComida();
            if (escolhida != null) ComidaComTipo.Marcar(comidaNova, escolhida.tipo);
        }

        gerenciador.IniciarFase(indice + 1, patosDaFase,
                                config.tempoLimite, ObjetivoDa(config), config.tempoTresEstrelas);

        Debug.Log($"===== FASE {indice + 1} ({config.nome}) =====");
        Debug.Log($"{patosDaFase} pato(s) | {porcoesDaFase} porção(ões) por pato | " +
                  $"{comidaDaFase} milho(s) no mapa" +
                  (config.tempoLimite > 0f ? $" | {config.tempoLimite:0}s de limite" : ""));
    }

    
    
    
    
    
    
    
    
    void ObterAreaDeEntrega(out Vector2 centro, out Vector2 tamanho)
    {
        if (areaDeEntregaDoLago != null)
        {
            Bounds limites = areaDeEntregaDoLago.bounds;

            centro = new Vector2(limites.center.x, limites.center.y);
            tamanho = new Vector2(Mathf.Max(4f, limites.size.x),
                                  Mathf.Max(4f, limites.size.y));

            return;
        }

        centro = centroDaAreaDeEntrega;
        tamanho = new Vector2(Mathf.Max(4f, tamanhoDaAreaDeEntrega.x),
                              Mathf.Max(4f, tamanhoDaAreaDeEntrega.y));
    }

    void IniciarFaseDoLago(int indice, ConfiguracaoDeFase config)
    {
        GameObject objeto = new GameObject("FaseDoLago");

        faseDoLago = objeto.AddComponent<FaseDoLago>();

        ObterAreaDeEntrega(out Vector2 centroEntrega, out Vector2 tamanhoEntrega);

        if (areaDeEntregaDoLago != null && !areaDeEntregaDoLago.isTrigger)
        {
            Debug.LogWarning("GerenciadorDeFases: o BoxCollider2D da área do lago não é Trigger. " +
                             "O jogador e os patos podem esbarrar nele; marque 'Is Trigger'.", this);
        }

        FaseDoLago.Configuracao cfg = new FaseDoLago.Configuracao
        {
            patoPrefab = patoPrefab,
            areaDoMapa = areaDoMapa,
            camadasDeObstaculo = camadasDeObstaculo,
            waypoints = waypointsDeNascimentoDosPatos,
            totalDePatos = Mathf.Clamp(config.patos, 1, Mathf.Max(1, maximoDePatos)),
            centroDaArea = centroEntrega,
            tamanhoDaArea = tamanhoEntrega,
            mostrarArea = mostrarAreaDeEntrega,
            transparenciaDaArea = Mathf.Clamp01(transparenciaDaArea),
            transparenciaDaBorda = Mathf.Clamp01(transparenciaDaBorda),
            transparenciaDoRotulo = Mathf.Clamp01(transparenciaDoRotulo),
            mostrarRotulo = mostrarRotuloLago,
            raioMinimoDeSpawn = raioMinimoDeSpawnDoJogador,
            raioDeSpawn = raioDeSpawnDoJogador,
            distanciaDeRecrutamento = distanciaDeRecrutamento,
            distanciaNaFila = distanciaNaFila,
            velocidadeDoSeguidor = velocidadeDoSeguidor,
            tentativasDeSpawn = tentativasDeSpawn,
            
            
            raioDeObstaculo = Mathf.Max(raioDaChecagemDoPato, 4f),
        };

        int patosCriados = faseDoLago.Iniciar(cfg);

        patosDaFase = patosCriados;
        porcoesDaFase = 0;
        comidaDaFase = 0;

        gerenciador.IniciarFase(indice + 1, patosDaFase,
                                config.tempoLimite, ObjetivoDa(config), config.tempoTresEstrelas);

        Debug.Log($"===== FASE {indice + 1} ({config.nome}) =====");
        Debug.Log($"{patosDaFase} pato(s) para levar ao lago | " +
                  $"área de entrega em {centroEntrega} | " +
                  $"tempo: {config.tempoLimite:0}s");

        if (patosCriados <= 0)
        {
            Debug.LogError("GerenciadorDeFases: a fase do lago não conseguiu criar nenhum pato. " +
                           "Confira o campo 'Pato Prefab' no objeto Comida e as mensagens de " +
                           "'não achei lugar livre' acima no Console.", this);
        }
    }

    void AplicarEspecie(Pato pato, EspecieDePato especie)
    {
        if (pato == null || especie == null) return;

        pato.nomeDaEspecie = especie.nome;
        pato.porcoesNecessarias += Mathf.Max(0, especie.porcoesExtras);
        pato.pontosPorPorcao = especie.pontosPorPorcao;
        pato.velocidade *= Mathf.Max(0.1f, especie.multiplicadorDeVelocidade);
        pato.corDaEspecie = especie.cor;
        pato.fogeDoJogador = especie.fogeDoJogador;
        pato.distanciaDeFuga = especie.distanciaDeFuga;
        pato.tempoDeVida = especie.tempoDeVida;
        pato.temComidaFavorita = especie.temComidaFavorita;
        pato.comidaFavorita = especie.comidaFavorita;
        pato.bonusDaComidaFavorita = especie.bonusDaComidaFavorita;

        pato.AplicarCorDaEspecie();
    }

    EspecieDePato SortearEspecie()
    {
        if (especies == null || especies.Length == 0) return null;

        float total = 0f;
        for (int i = 0; i < especies.Length; i++)
        {
            if (especies[i] != null && especies[i].peso > 0f) total += especies[i].peso;
        }

        if (total <= 0f) return especies[0];

        float sorteio = UnityEngine.Random.Range(0f, total);

        for (int i = 0; i < especies.Length; i++)
        {
            if (especies[i] == null) continue;

            sorteio -= especies[i].peso;
            if (sorteio <= 0f) return especies[i];
        }

        return especies[especies.Length - 1];
    }

    ComidaDoMapa SortearComida()
    {
        if (comidas == null || comidas.Length == 0) return null;

        float total = 0f;
        for (int i = 0; i < comidas.Length; i++)
        {
            if (comidas[i] != null && comidas[i].peso > 0f) total += comidas[i].peso;
        }

        if (total <= 0f) return comidas[0];

        float sorteio = UnityEngine.Random.Range(0f, total);

        for (int i = 0; i < comidas.Length; i++)
        {
            if (comidas[i] == null) continue;

            sorteio -= comidas[i].peso;
            if (sorteio <= 0f) return comidas[i];
        }

        return comidas[comidas.Length - 1];
    }

    
    
    
    ConfiguracaoDeFase ConfiguracaoDaFase(int indice)
    {
        if (modoDeFases == ModoDeFases.Configuradas && fases != null && fases.Length > 0)
        {
            if (indice < fases.Length)
            {
                ConfiguracaoDeFase config = fases[indice];
                if (config != null) return config;
            }
            else
            {
                int ciclos = indice / fases.Length;
                ConfiguracaoDeFase baseConfig = fases[indice % fases.Length];

                if (baseConfig != null) return ComDificuldade(baseConfig, ciclos);
            }
        }

        
        return new ConfiguracaoDeFase
        {
            nome = "Fase automática",
            tipo = TipoDeFase.AlimentarPatos,
            patos = PatosDaFase(indice),
            porcoesPorPato = PorcoesPorPato(indice),
            comidaNoMapa = ComidaDoMapa(indice),
            tempoLimite = 0f,
            tempoTresEstrelas = 0f,
        };
    }

    static ConfiguracaoDeFase ComDificuldade(ConfiguracaoDeFase baseConfig, int ciclos)
    {
        if (ciclos <= 0) return baseConfig;

        ConfiguracaoDeFase config = new ConfiguracaoDeFase
        {
            nome = baseConfig.nome,
            tipo = baseConfig.tipo,
            patos = baseConfig.patos + ciclos,
            porcoesPorPato = baseConfig.porcoesPorPato + ciclos,
            comidaNoMapa = baseConfig.comidaNoMapa + ciclos * 4,
            tempoLimite = baseConfig.tempoLimite > 0f ? baseConfig.tempoLimite + ciclos * 15f : 0f,
            tempoTresEstrelas = baseConfig.tempoTresEstrelas > 0f ? baseConfig.tempoTresEstrelas + ciclos * 10f : 0f,
        };

        return config;
    }

    static string ObjetivoDa(ConfiguracaoDeFase config)
    {
        if (config != null && config.tipo == TipoDeFase.LevarPatosAoLago)
        {
            return "Fase {0} - Aperte S nos patos e leve a fila ao lago: faltam {3}";
        }

        return "Fase {0} - Alimente os patos com F: faltam {3}";
    }

    int IndiceDaPrimeiraFaseDoLago()
    {
        GarantirFases();

        for (int i = 0; i < fases.Length; i++)
        {
            if (fases[i] != null && fases[i].tipo == TipoDeFase.LevarPatosAoLago) return i;
        }

        return 1;
    }

    
    
    
    void LimparFase()
    {
        if (faseDoLago != null)
        {
            
            faseDoLago.enabled = false;

            if (faseDoLago.gameObject != null) Destroy(faseDoLago.gameObject);
            faseDoLago = null;
        }

        DestruirPorTag("ComidaMapa");  
        DestruirPorTag("ComidaPato");  

        
        List<Pato> patosAntigos = new List<Pato>(Pato.Todos);
        foreach (Pato pato in patosAntigos)
        {
            if (pato == null) continue;

            
            
            pato.satisfeito = true;
            Destroy(pato.gameObject);
        }
    }

    void DestruirPorTag(string tag)
    {
        GameObject[] objetos = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject objeto in objetos) Destroy(objeto);
    }

    
    
    
    GameObject GerarObjetoAleatorio(GameObject prefab, float raioDeChecagem)
    {
        if (prefab == null)
        {
            return null;
        }

        Rect area = CalcularArea();
        Player jogador = AcharJogador();

        Vector2 centro = jogador != null ? (Vector2)jogador.transform.position : area.center;
        bool pertoDoJogador = nascerPertoDoJogador && jogador != null;

        int tentativas = evitarObstaculos ? Mathf.Max(1, tentativasDeSpawn) : 1;

        for (int i = 0; i < tentativas; i++)
        {
            Vector2 ponto;

            if (pertoDoJogador)
            {
                float angulo = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float raio = UnityEngine.Random.Range(raioMinimoDeSpawnDoJogador,
                                                     Mathf.Max(raioMinimoDeSpawnDoJogador, raioDeSpawnDoJogador));

                ponto = centro + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * raio;
            }
            else
            {
                ponto = new Vector2(UnityEngine.Random.Range(area.xMin, area.xMax),
                                    UnityEngine.Random.Range(area.yMin, area.yMax));
            }

            if (!area.Contains(ponto)) continue;

            if (!evitarObstaculos || LugarEstaVazio(ponto, raioDeChecagem))
            {
                return Instantiate(prefab, ponto, Quaternion.identity);
            }
        }

        
        if (evitarObstaculos && jogador != null)
        {
            Vector2[] direcoes = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };

            for (int i = 0; i < direcoes.Length; i++)
            {
                Vector2 ponto = centro + direcoes[i] * 6f;

                if (area.Contains(ponto) && LugarEstaVazio(ponto, raioDeChecagem))
                {
                    return Instantiate(prefab, ponto, Quaternion.identity);
                }
            }
        }

        if (!jaAvisouAreaCheia)
        {
            jaAvisouAreaCheia = true;

            Debug.LogWarning($"GerenciadorDeFases: não achei lugar livre para '{prefab.name}' depois de " +
                             $"{tentativas} tentativas. Não vou nascer no lugar errado " +
                             "(era isso que deixava milho inalcançável).", this);
        }

        
        return null;
    }

    bool LugarEstaVazio(Vector2 ponto, float raio)
    {
        
        
        Physics2D.SyncTransforms();

        Collider2D[] achados = Physics2D.OverlapCircleAll(ponto, raio, camadasDeObstaculo);

        for (int i = 0; i < achados.Length; i++)
        {
            Collider2D achado = achados[i];
            if (achado == null) continue;

            
            if (areaDeSpawn != null && achado == areaDeSpawn) continue;

            return false;
        }

        return true;
    }

    static Player AcharJogador()
    {
        if (jogadorCache == null) jogadorCache = FindAnyObjectByType<Player>();
        return jogadorCache;
    }

    Rect CalcularArea()
    {
        if (areaDoMapa == null)
        {
            GameObject fundo = GameObject.Find("Cenario");
            if (fundo != null) areaDoMapa = fundo.GetComponent<SpriteRenderer>();
        }

        if (areaDoMapa != null)
        {
            return AplicarMargem(RectDe(areaDoMapa.bounds));
        }

        return AplicarMargem(new Rect(transform.position.x - 10f, transform.position.y - 6f, 20f, 12f));
    }

    static Rect RectDe(Bounds limites)
    {
        return new Rect(limites.min.x, limites.min.y, limites.size.x, limites.size.y);
    }

    Rect AplicarMargem(Rect retangulo)
    {
        float margemX = Mathf.Min(margemInterna.x, retangulo.width * 0.4f);
        float margemY = Mathf.Min(margemInterna.y, retangulo.height * 0.4f);

        retangulo.xMin += margemX;
        retangulo.xMax -= margemX;
        retangulo.yMin += margemY;
        retangulo.yMax -= margemY;

        return retangulo;
    }

    void DesenharWaypointsDosPatos()
    {
        if (waypointsDeNascimentoDosPatos == null) return;

        int numero = 1;

        for (int i = 0; i < waypointsDeNascimentoDosPatos.Length; i++)
        {
            Transform waypoint = waypointsDeNascimentoDosPatos[i];
            if (waypoint == null) continue;

            Gizmos.color = new Color(1f, 0.86f, 0.2f);
            Gizmos.DrawSphere(waypoint.position, 0.5f);
            Gizmos.DrawWireSphere(waypoint.position, 1.4f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(waypoint.position + Vector3.up * 1.7f, $"Pato {numero}");
#endif
            numero++;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (mostrarAreaDeSpawn)
        {
            Rect area = CalcularArea();

            Gizmos.color = new Color(1f, 0.55f, 0.1f);
            Gizmos.DrawWireCube(new Vector3(area.center.x, area.center.y, 0f),
                                new Vector3(area.width, area.height, 0f));

            Player jogador = AcharJogador();
            if (jogador != null && nascerPertoDoJogador)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.6f);
                Gizmos.DrawWireSphere(jogador.transform.position, raioDeSpawnDoJogador);
            }
        }

        DesenharWaypointsDosPatos();

        
        ObterAreaDeEntrega(out Vector2 centroGizmo, out Vector2 tamanhoGizmo);

        Gizmos.color = new Color(0.2f, 0.8f, 1f);
        Gizmos.DrawWireCube(centroGizmo, tamanhoGizmo);

#if UNITY_EDITOR
        
        
        
        if (areaDeEntregaDoLago == null) DesenharAlcasDaAreaDoLago();
#endif
    }

#if UNITY_EDITOR
    
    
    
    
    void DesenharAlcasDaAreaDoLago()
    {
        Vector3 centro = new Vector3(centroDaAreaDeEntrega.x, centroDaAreaDeEntrega.y, 0f);
        Vector3 tamanho = new Vector3(tamanhoDaAreaDeEntrega.x, tamanhoDaAreaDeEntrega.y, 0.1f);

        UnityEditor.IMGUI.Controls.BoxBoundsHandle caixa =
            new UnityEditor.IMGUI.Controls.BoxBoundsHandle();

        caixa.center = centro;
        caixa.size = tamanho;

        UnityEditor.EditorGUI.BeginChangeCheck();

        
        Vector3 centroArrastado = UnityEditor.Handles.PositionHandle(centro, Quaternion.identity);
        caixa.center = centroArrastado;
        caixa.DrawHandle();

        if (!UnityEditor.EditorGUI.EndChangeCheck()) return;

        UnityEditor.Undo.RecordObject(this, "Mover/redimensionar área do lago");

        centroDaAreaDeEntrega = new Vector2(caixa.center.x, caixa.center.y);
        tamanhoDaAreaDeEntrega = new Vector2(Mathf.Max(4f, caixa.size.x),
                                             Mathf.Max(4f, caixa.size.y));

        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Criar/Selecionar Área do Lago")]
    void CriarOuSelecionarAreaDoLago()
    {
        if (areaDeEntregaDoLago != null)
        {
            UnityEditor.Selection.activeGameObject = areaDeEntregaDoLago.gameObject;
            return;
        }

        
        GameObject existente = GameObject.Find("AreaDoLago");
        if (existente != null)
        {
            BoxCollider2D caixaExistente = existente.GetComponent<BoxCollider2D>();
            if (caixaExistente != null)
            {
                UnityEditor.Undo.RecordObject(this, "Definir área do lago");
                areaDeEntregaDoLago = caixaExistente;
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.Selection.activeGameObject = existente;
                return;
            }
        }

        GameObject objeto = new GameObject("AreaDoLago");
        UnityEditor.Undo.RegisterCreatedObjectUndo(objeto, "Criar área do lago");

        objeto.transform.position = new Vector3(centroDaAreaDeEntrega.x,
                                                centroDaAreaDeEntrega.y, 0f);

        BoxCollider2D caixa = objeto.AddComponent<BoxCollider2D>();
        caixa.isTrigger = true;
        caixa.size = new Vector2(Mathf.Max(4f, tamanhoDaAreaDeEntrega.x),
                                 Mathf.Max(4f, tamanhoDaAreaDeEntrega.y));

        UnityEditor.Undo.RecordObject(this, "Definir área do lago");
        areaDeEntregaDoLago = caixa;
        UnityEditor.EditorUtility.SetDirty(this);

        UnityEditor.Selection.activeGameObject = objeto;

        Debug.Log("Área do Lago criada. Arraste/redimensione o objeto 'AreaDoLago' e salve a cena.", this);
    }

#endif
}
