using System;
using UnityEngine;


public class GerenciadorDeJogo : MonoBehaviour
{
    private static GerenciadorDeJogo instancia;

    public static GerenciadorDeJogo Instancia
    {
        get
        {
            if (instancia != null) return instancia;

            instancia = FindAnyObjectByType<GerenciadorDeJogo>();

            if (instancia == null)
            {
                GameObject objeto = new GameObject("GerenciadorDeJogo (criado automaticamente)");
                instancia = objeto.AddComponent<GerenciadorDeJogo>();
            }

            return instancia;
        }
    }
    public int pontosPorPorcao = 10;
    public int bonusPorPatoSatisfeito = 25;
    public bool usarCombo = true;
    public int patosParaCombo2 = 3;
    public int patosParaCombo3 = 5;
    public float tempoDoCombo = 10f;
    public bool usarCronometro = true;
    public float tempoTresEstrelasFase1 = 45f;
    public float tempoExtraPorFase = 15f;
    public float folgaParaDuasEstrelas = 1.6f;
    public int pontosPorEstrela = 50;
    public string prefixoDoRecorde = "UnifeiDucks_TempoFase";
    [SerializeField] private int pontosAtuais = 0;
    [SerializeField] private int faseAtual = 1;
    [SerializeField] private int patosSatisfeitos = 0;
    [SerializeField] private int patosNaFase = 0;
    [SerializeField] private float tempoDaFase = 0f;
    [SerializeField] private int combo = 0;
    [SerializeField] private int multiplicador = 1;
    [SerializeField] private int ultimasEstrelas = 0;
    [SerializeField] private float ultimoTempo = 0f;
    [SerializeField] private int ultimoBonusDeTempo = 0;
    [SerializeField] private bool ultimoBateuRecorde = false;
    [SerializeField] private int estrelasTotais = 0;

    
    
    
    [Header("Fase com tempo (0 = sem limite)")]
    [Tooltip("Quanto tempo o jogador tem para concluir a fase. 0 = só conta o tempo, sem perder.")]
    [SerializeField] private float tempoLimiteDaFase = 0f;
    [SerializeField] private float tempoRestanteDaFase = 0f;
    [SerializeField] private float metaDeTresEstrelasDaFase = 0f;
    [SerializeField] private string objetivoDaFase = "";
    [SerializeField] private bool ultimaFaseFalhou = false;

    private float tempoUltimoCombo;

    public int PontosAtuais => pontosAtuais;
    public int FaseAtual => faseAtual;
    public int PatosSatisfeitos => patosSatisfeitos;
    public int PatosNaFase => patosNaFase;

    public float TempoDaFase => tempoDaFase;
    public bool FaseEstaRodando { get; private set; }

    public int Combo => combo;
    public int Multiplicador => usarCombo ? multiplicador : 1;
    public float TempoRestanteDoCombo => (usarCombo && combo > 0)
        ? Mathf.Max(0f, tempoDoCombo - (Time.time - tempoUltimoCombo))
        : 0f;

    public int UltimasEstrelas => ultimasEstrelas;
    public float UltimoTempo => ultimoTempo;
    public int UltimoBonusDeTempo => ultimoBonusDeTempo;
    public bool UltimoBateuRecorde => ultimoBateuRecorde;
    public int EstrelasTotais => estrelasTotais;

    
    public bool TemLimiteDeTempo => tempoLimiteDaFase > 0f;
    public float TempoLimiteDaFase => tempoLimiteDaFase;
    public float TempoRestanteDaFase => tempoRestanteDaFase;
    public string ObjetivoDaFase => objetivoDaFase;
    public bool UltimaFaseFalhou => ultimaFaseFalhou;

    public event Action<int> AoMudarPontos;
    public event Action AoMudarProgresso;
    public event Action AoCompletarFase;
    public event Action AoFalharFase;

    public event Action AoMudarCombo;

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;

        estrelasTotais = PlayerPrefs.GetInt("UnifeiDucks_EstrelasTotais", 0);
    }

    void Update()
    {
        if (FaseEstaRodando)
        {
            if (usarCronometro)
            {
                tempoDaFase += Time.deltaTime;
            }

            if (tempoLimiteDaFase > 0f)
            {
                tempoRestanteDaFase = Mathf.Max(0f, tempoRestanteDaFase - Time.deltaTime);

                if (tempoRestanteDaFase <= 0f)
                {
                    FalharFase();
                    return;
                }
            }
        }

        if (!usarCombo || combo <= 0) return;
        if (Time.time - tempoUltimoCombo <= tempoDoCombo) return;

        combo = 0;
        multiplicador = 1;
        AoMudarCombo?.Invoke();
    }

    public int GanharPontos(int quantidade)
    {
        if (quantidade == 0) return 0;

        int ganho = quantidade * Multiplicador;

        pontosAtuais += ganho;
        AoMudarPontos?.Invoke(pontosAtuais);

        return ganho;
    }

    
    
    
    
    
    
    public void IniciarFase(int numeroDaFase, int totalDePatos,
                            float tempoLimite = 0f, string objetivo = null,
                            float metaTresEstrelas = 0f)
    {
        faseAtual = numeroDaFase;
        patosNaFase = Mathf.Max(0, totalDePatos);
        patosSatisfeitos = 0;

        tempoDaFase = 0f;
        combo = 0;
        multiplicador = 1;
        FaseEstaRodando = true;

        tempoLimiteDaFase = Mathf.Max(0f, tempoLimite);
        tempoRestanteDaFase = tempoLimiteDaFase;
        metaDeTresEstrelasDaFase = Mathf.Max(0f, metaTresEstrelas);
        objetivoDaFase = string.IsNullOrEmpty(objetivo) ? "" : objetivo;
        ultimaFaseFalhou = false;

        AoMudarProgresso?.Invoke();
        AoMudarCombo?.Invoke();
    }

    public int RegistrarPatoSatisfeito()
    {
        patosSatisfeitos++;

        if (usarCombo)
        {
            combo++;
            tempoUltimoCombo = Time.time;
            multiplicador = CalcularMultiplicador(combo);
            AoMudarCombo?.Invoke();
        }

        int ganhoDoBonus = GanharPontos(bonusPorPatoSatisfeito);

        AoMudarProgresso?.Invoke();

        if (patosNaFase > 0 && patosSatisfeitos >= patosNaFase)
        {
            TerminarFase();
        }

        return ganhoDoBonus;
    }

    public void RegistrarPatoPerdido()
    {
        patosSatisfeitos++;

        combo = 0;
        multiplicador = 1;
        AoMudarCombo?.Invoke();
        AoMudarProgresso?.Invoke();

        if (patosNaFase > 0 && patosSatisfeitos >= patosNaFase)
        {
            TerminarFase();
        }
    }

    
    
    
    void TerminarFase()
    {
        FaseEstaRodando = false;
        ultimaFaseFalhou = false;

        ultimoTempo = tempoDaFase;
        ultimasEstrelas = CalcularEstrelas(ultimoTempo);
        ultimoBonusDeTempo = ultimasEstrelas * pontosPorEstrela;

        float recordeAntigo = RecordeDaFase(faseAtual);
        ultimoBateuRecorde = recordeAntigo <= 0f || ultimoTempo < recordeAntigo;

        if (ultimoBateuRecorde)
        {
            PlayerPrefs.SetFloat(prefixoDoRecorde + faseAtual, ultimoTempo);
        }

        estrelasTotais += ultimasEstrelas;
        PlayerPrefs.SetInt("UnifeiDucks_EstrelasTotais", estrelasTotais);
        PlayerPrefs.Save();

        if (ultimoBonusDeTempo > 0)
        {
            pontosAtuais += ultimoBonusDeTempo;
            AoMudarPontos?.Invoke(pontosAtuais);
        }

        AoCompletarFase?.Invoke();
    }

    
    
    
    
    public void FalharFase()
    {
        if (!FaseEstaRodando) return;

        FaseEstaRodando = false;
        ultimaFaseFalhou = true;

        combo = 0;
        multiplicador = 1;
        AoMudarCombo?.Invoke();

        Debug.Log($"GerenciadorDeJogo: fase {faseAtual} falhou (tempo esgotado).");

        GerenciadorDeAudio.Instancia.TocarErro();

        AoFalharFase?.Invoke();
    }

    int CalcularMultiplicador(int comboAtual)
    {
        if (comboAtual >= Mathf.Max(2, patosParaCombo3)) return 3;
        if (comboAtual >= Mathf.Max(1, patosParaCombo2)) return 2;
        return 1;
    }

    public int CalcularEstrelas(float tempo)
    {
        float meta = TempoParaTresEstrelas(faseAtual);

        if (tempo <= meta) return 3;
        if (tempo <= meta * Mathf.Max(1.01f, folgaParaDuasEstrelas)) return 2;
        return 1;
    }

    public float TempoParaTresEstrelas(int fase)
    {
        if (metaDeTresEstrelasDaFase > 0f) return metaDeTresEstrelasDaFase;

        return tempoTresEstrelasFase1 + tempoExtraPorFase * Mathf.Max(0, fase - 1);
    }

    public float RecordeDaFase(int fase)
    {
        return PlayerPrefs.GetFloat(prefixoDoRecorde + fase, 0f);
    }
}
