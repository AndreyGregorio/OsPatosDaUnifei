using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MoedasUI : MonoBehaviour
{
    public TextMeshProUGUI textoContador;
    public TextMeshProUGUI textoObjetivo;
    public string formatoObjetivo = "Fase {0}   Faltam {3} pato(s)";
    public bool darPulinhoAoPontuar = true;
    public float forcaDoPulinho = 0.35f;
    public float duracaoDoPulinho = 0.22f;
    public Color corDoFlash = new Color(1f, 0.9f, 0.35f);
    public Color corDaEstrelaGanha = new Color(1f, 0.86f, 0.25f);
    public Color corDaEstrelaVazia = new Color(1f, 1f, 1f, 0.22f);
    public Color corDoTempoNormal = Color.white;
    public Color corDoTempoApertado = new Color(1f, 0.55f, 0.45f);
    public Color corDoCombo = new Color(1f, 0.72f, 0.25f);

    private GerenciadorDeJogo gerenciador;

    
    private Vector3 escalaOriginalDoContador;
    private Color corOriginalDoContador;
    private Coroutine pulinhoAtual;
    private bool jaIniciou;

    
    private TextMeshProUGUI textoRelogio;
    private Image[] estrelasDoRelogio = new Image[3];
    private TextMeshProUGUI textoCombo;
    private string ultimoRelogio = "";

    
    private GameObject painelDeFim;
    private TextMeshProUGUI textoFimTitulo;
    private TextMeshProUGUI textoFimLinha;
    private TextMeshProUGUI textoFimRecorde;
    private Image[] estrelasDoPainel = new Image[3];

    private static Sprite spriteDaEstrela;
    private static Sprite spriteBranco;

    void Start()
    {
        if (textoContador == null) textoContador = ProcurarTexto("TextoContador");
        if (textoObjetivo == null) textoObjetivo = ProcurarTexto("TextoObjetivo");
        if (textoObjetivo == null) textoObjetivo = CriarTextoDeObjetivo();

        if (textoContador != null)
        {
            escalaOriginalDoContador = textoContador.transform.localScale;
            corOriginalDoContador = textoContador.color;
        }

        MontarRelogioECombo();
        MontarPainelDeFimDeFase();

        gerenciador = GerenciadorDeJogo.Instancia;
        gerenciador.AoMudarPontos += AtualizarPontos;
        gerenciador.AoMudarProgresso += AtualizarObjetivo;
        gerenciador.AoCompletarFase += MostrarFimDeFase;
        gerenciador.AoFalharFase += MostrarFalhaDeFase;

        AtualizarPontos(gerenciador.PontosAtuais);
        AtualizarObjetivo();

        jaIniciou = true;
    }

    void OnDestroy()
    {
        if (gerenciador == null) return;

        gerenciador.AoMudarPontos -= AtualizarPontos;
        gerenciador.AoMudarProgresso -= AtualizarObjetivo;
        gerenciador.AoCompletarFase -= MostrarFimDeFase;
        gerenciador.AoFalharFase -= MostrarFalhaDeFase;
    }

    void Update()
    {
        AtualizarRelogio();
        AtualizarTextoDoCombo();
    }

    
    
    
    void AtualizarPontos(int pontos)
    {
        if (textoContador != null) textoContador.text = pontos.ToString();

        if (!darPulinhoAoPontuar || !jaIniciou || textoContador == null) return;

        if (pulinhoAtual != null) StopCoroutine(pulinhoAtual);
        pulinhoAtual = StartCoroutine(PulinhoDoContador());
    }

    void AtualizarObjetivo()
    {
        if (gerenciador == null) return;

        int total = gerenciador.PatosNaFase;
        int alimentados = gerenciador.PatosSatisfeitos;
        int faltam = Mathf.Max(0, total - alimentados);

        if (textoObjetivo != null)
        {
            string formato = gerenciador.ObjetivoDaFase;

            if (string.IsNullOrEmpty(formato) || !formato.Contains("{3}"))
            {
                formato = FormatoDeVerdade();
            }

            textoObjetivo.text = string.Format(formato,
                                               gerenciador.FaseAtual,
                                               alimentados,
                                               total,
                                               faltam);
        }

        
        if (painelDeFim != null && gerenciador.FaseEstaRodando)
        {
            painelDeFim.SetActive(false);
        }
    }

    string FormatoDeVerdade()
    {
        if (string.IsNullOrEmpty(formatoObjetivo) || !formatoObjetivo.Contains("{3}"))
        {
            return "Fase {0}   Faltam {3} pato(s)";
        }

        return formatoObjetivo;
    }

    IEnumerator PulinhoDoContador()
    {
        Transform alvo = textoContador.transform;
        float tempo = 0f;

        while (tempo < duracaoDoPulinho)
        {
            tempo += Time.deltaTime;

            float progresso = Mathf.Clamp01(tempo / duracaoDoPulinho);
            float pulso = 1f + forcaDoPulinho * Mathf.Sin(progresso * Mathf.PI);

            alvo.localScale = escalaOriginalDoContador * pulso;
            textoContador.color = Color.Lerp(corDoFlash, corOriginalDoContador, progresso);

            yield return null;
        }

        alvo.localScale = escalaOriginalDoContador;
        textoContador.color = corOriginalDoContador;
        pulinhoAtual = null;
    }

    
    
    
    void MontarRelogioECombo()
    {
        Transform pai = transform.parent != null ? transform.parent : transform;

        textoRelogio = ProcurarTexto("TextoTempo");

        if (textoRelogio == null)
        {
            textoRelogio = CriarTexto("TextoTempo", pai,
                                      new Vector2(0.5f, 1f), new Vector2(0f, -55f),
                                      58f, corDoTempoNormal, TextAlignmentOptions.Center);
        }

        if (textoRelogio != null)
        {
            textoRelogio.transform.SetParent(pai, false);
        }

        
        for (int i = 0; i < 3; i++)
        {
            estrelasDoRelogio[i] = CriarEstrela(pai, "EstrelaRelogio" + (i + 1),
                                                new Vector2(0.5f, 1f),
                                                new Vector2((i - 1) * 62f, -122f),
                                                54f);

            estrelasDoRelogio[i].color = corDaEstrelaVazia;
        }

        textoCombo = ProcurarTexto("TextoCombo");

        if (textoCombo == null)
        {
            textoCombo = CriarTexto("TextoCombo", pai,
                                    new Vector2(0.5f, 1f), new Vector2(0f, -196f),
                                    64f, corDoCombo, TextAlignmentOptions.Center);
        }

        if (textoCombo != null)
        {
            textoCombo.transform.SetParent(pai, false);
            textoCombo.gameObject.SetActive(false);
        }
    }

    void AtualizarRelogio()
    {
        if (textoRelogio == null || gerenciador == null) return;

        bool temLimite = gerenciador.TemLimiteDeTempo;
        bool mostrar = (gerenciador.usarCronometro || temLimite) && gerenciador.FaseEstaRodando;

        if (textoRelogio.gameObject.activeSelf != mostrar)
        {
            textoRelogio.gameObject.SetActive(mostrar);
        }

        
        
        bool mostrarEstrelas = mostrar && !temLimite;

        for (int i = 0; i < estrelasDoRelogio.Length; i++)
        {
            if (estrelasDoRelogio[i] == null) continue;

            if (estrelasDoRelogio[i].gameObject.activeSelf != mostrarEstrelas)
            {
                estrelasDoRelogio[i].gameObject.SetActive(mostrarEstrelas);
            }
        }

        if (!mostrar)
        {
            
            ultimoRelogio = "";
            return;
        }

        float valor = temLimite ? gerenciador.TempoRestanteDaFase : gerenciador.TempoDaFase;
        string tempo = FormatarTempo(valor);

        if (tempo != ultimoRelogio)
        {
            ultimoRelogio = tempo;
            textoRelogio.text = temLimite ? tempo + " restante" : tempo;
        }

        if (temLimite)
        {
            textoRelogio.color = valor <= 10f ? corDoTempoApertado : corDoTempoNormal;
            return;
        }

        
        int estrelasAgora = gerenciador.CalcularEstrelas(gerenciador.TempoDaFase);
        textoRelogio.color = estrelasAgora >= 3 ? corDoTempoNormal : corDoTempoApertado;

        for (int i = 0; i < estrelasDoRelogio.Length; i++)
        {
            if (estrelasDoRelogio[i] == null) continue;
            estrelasDoRelogio[i].color = i < estrelasAgora ? corDaEstrelaGanha : corDaEstrelaVazia;
        }
    }

    static string FormatarTempo(float segundos)
    {
        int minutos = Mathf.FloorToInt(segundos / 60f);
        int resto = Mathf.FloorToInt(segundos - minutos * 60f);

        return $"{minutos}:{resto:00}";
    }

    
    
    
    void AtualizarTextoDoCombo()
    {
        if (textoCombo == null || gerenciador == null) return;

        int multiplicador = gerenciador.Multiplicador;
        bool mostrar = multiplicador > 1;

        if (textoCombo.gameObject.activeSelf != mostrar)
        {
            textoCombo.gameObject.SetActive(mostrar);
        }

        if (!mostrar) return;

        textoCombo.text = $"COMBO x{multiplicador}   ({gerenciador.TempoRestanteDoCombo:0.0}s)";

        
        float restante = gerenciador.TempoRestanteDoCombo;
        float piscar = restante < 3f ? 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 8f)) : 1f;

        Color cor = corDoCombo;
        cor.a = piscar;
        textoCombo.color = cor;
    }

    
    
    
    void MontarPainelDeFimDeFase()
    {
        Transform pai = transform.parent != null ? transform.parent : transform;

        painelDeFim = new GameObject("PainelFimDeFase", typeof(RectTransform));
        painelDeFim.transform.SetParent(pai, false);

        RectTransform rt = painelDeFim.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1020f, 430f);

        Image fundo = painelDeFim.AddComponent<Image>();
        fundo.sprite = SpriteBranco();
        fundo.color = new Color(0.06f, 0.09f, 0.14f, 0.9f);
        fundo.raycastTarget = false;

        textoFimTitulo = CriarTexto("TextoTituloFim", painelDeFim.transform,
                                    new Vector2(0.5f, 0.5f), new Vector2(0f, 150f),
                                    64f, Color.white, TextAlignmentOptions.Center);

        for (int i = 0; i < 3; i++)
        {
            estrelasDoPainel[i] = CriarEstrela(painelDeFim.transform, "EstrelaFim" + (i + 1),
                                               new Vector2(0.5f, 0.5f),
                                               new Vector2((i - 1) * 130f, 45f),
                                               120f);
        }

        textoFimLinha = CriarTexto("TextoLinhaFim", painelDeFim.transform,
                                   new Vector2(0.5f, 0.5f), new Vector2(0f, -60f),
                                   44f, new Color(1f, 0.97f, 0.85f), TextAlignmentOptions.Center);

        textoFimRecorde = CriarTexto("TextoRecordeFim", painelDeFim.transform,
                                     new Vector2(0.5f, 0.5f), new Vector2(0f, -140f),
                                     36f, new Color(0.75f, 0.9f, 1f), TextAlignmentOptions.Center);

        painelDeFim.SetActive(false);
    }

    void MostrarFimDeFase()
    {
        if (gerenciador == null) return;

        if (painelDeFim == null) MontarPainelDeFimDeFase();

        if (textoFimTitulo != null)
        {
            textoFimTitulo.text = $"FASE {gerenciador.FaseAtual} CONCLUÍDA!";
        }

        for (int i = 0; i < estrelasDoPainel.Length; i++)
        {
            if (estrelasDoPainel[i] == null) continue;
            estrelasDoPainel[i].color = i < gerenciador.UltimasEstrelas ? corDaEstrelaGanha : corDaEstrelaVazia;
        }

        if (textoFimLinha != null)
        {
            textoFimLinha.text = $"Tempo: {gerenciador.UltimoTempo:0.0}s     " +
                                 $"+{gerenciador.UltimoBonusDeTempo} de bônus de tempo";
        }

        if (textoFimRecorde != null)
        {
            float recorde = gerenciador.RecordeDaFase(gerenciador.FaseAtual);

            textoFimRecorde.text = gerenciador.UltimoBateuRecorde
                ? $"NOVO RECORDE!  ({recorde:0.0}s)"
                : $"Recorde da fase: {recorde:0.0}s";

            textoFimRecorde.color = gerenciador.UltimoBateuRecorde
                ? new Color(1f, 0.85f, 0.35f)
                : new Color(0.75f, 0.9f, 1f);
        }

        painelDeFim.SetActive(true);
        painelDeFim.transform.SetAsLastSibling();
    }

    
    
    
    
    void MostrarFalhaDeFase()
    {
        if (gerenciador == null) return;

        if (painelDeFim == null) MontarPainelDeFimDeFase();

        if (textoFimTitulo != null)
        {
            textoFimTitulo.text = "TEMPO ESGOTADO!";
        }

        for (int i = 0; i < estrelasDoPainel.Length; i++)
        {
            if (estrelasDoPainel[i] == null) continue;
            estrelasDoPainel[i].color = corDaEstrelaVazia;
        }

        if (textoFimLinha != null)
        {
            textoFimLinha.text = "Aperte R para tentar de novo.";
        }

        if (textoFimRecorde != null)
        {
            textoFimRecorde.text = "";
        }

        painelDeFim.SetActive(true);
        painelDeFim.transform.SetAsLastSibling();
    }

    
    
    
    TextMeshProUGUI CriarTextoDeObjetivo()
    {
        if (textoContador == null) return null;

        
        
        
        Transform pai = transform.parent != null ? transform.parent : transform;

        TextMeshProUGUI texto = CriarTexto("TextoObjetivo", pai,
                                           new Vector2(0f, 1f), new Vector2(40f, -40f),
                                           38f, new Color(1f, 0.96f, 0.85f, 0.95f),
                                           TextAlignmentOptions.TopLeft);

        RectTransform rt = texto.rectTransform;
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(720f, 170f);

        return texto;
    }

    TextMeshProUGUI CriarTexto(string nome, Transform pai, Vector2 ancora, Vector2 posicao,
                               float tamanhoDaFonte, Color cor, TextAlignmentOptions alinhamento)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform));
        objeto.transform.SetParent(pai, false);

        RectTransform rt = objeto.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = ancora;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = new Vector2(1400f, 120f);

        TextMeshProUGUI texto = objeto.AddComponent<TextMeshProUGUI>();

        if (textoContador != null) texto.font = textoContador.font;

        texto.fontSize = tamanhoDaFonte;
        texto.color = cor;
        texto.alignment = alinhamento;
        texto.raycastTarget = false;
        texto.text = "";

        return texto;
    }

    Image CriarEstrela(Transform pai, string nome, Vector2 ancora, Vector2 posicao, float tamanho)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform));
        objeto.transform.SetParent(pai, false);

        RectTransform rt = objeto.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = ancora;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = new Vector2(tamanho, tamanho);

        Image imagem = objeto.AddComponent<Image>();
        imagem.sprite = SpriteDaEstrela();
        imagem.raycastTarget = false;

        return imagem;
    }

    static Sprite SpriteDaEstrela()
    {
        if (spriteDaEstrela == null)
        {
            spriteDaEstrela = ArteProcedural.Criar(new string[]
            {
                ".....K.....",
                "....KWK....",
                "....KWK....",
                "KKKKWWWKKKK",
                ".KWWWWWWWK.",
                "..KWWWWWK..",
                "...KWWWK...",
                "..KWWWWWK..",
                "..KWK.KWK..",
                ".KWK...KWK.",
            }, 16f);
        }

        return spriteDaEstrela;
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

    TextMeshProUGUI ProcurarTexto(string nomeDoObjeto)
    {
        GameObject objeto = GameObject.Find(nomeDoObjeto);
        if (objeto == null) return null;
        return objeto.GetComponent<TextMeshProUGUI>();
    }
}
