using UnityEngine;

public class GerenciadorDeAudio : MonoBehaviour
{
    private static GerenciadorDeAudio instancia;

    public static GerenciadorDeAudio Instancia
    {
        get
        {
            if (instancia != null) return instancia;

            instancia = FindAnyObjectByType<GerenciadorDeAudio>();

            if (instancia == null)
            {
                GameObject objeto = new GameObject("GerenciadorDeAudio (criado automaticamente)");
                instancia = objeto.AddComponent<GerenciadorDeAudio>();
            }

            return instancia;
        }
    }

    [Header("Clipes de som (OPCIONAL)")]
    [Tooltip("Deixe vazio para o jogo usar o som gerado automaticamente. " +
             "Se você arrastar um .wav aqui, ele é quem toca.")]
    public AudioClip somMoeda;
    public AudioClip somQuack;
    public AudioClip somComer;
    public AudioClip somFaseCompleta;
    public AudioClip somErro;

    [Header("Volume")]
    [Range(0f, 1f)]
    [Tooltip("Volume geral dos efeitos do jogo (0 = mudo)")]
    public float volumeGeral = 0.5f;

    [Tooltip("Tempo mínimo entre dois 'quacks', para 8 patos não virarem uma algazarra")]
    public float intervaloEntreQuacks = 0.09f;

    private AudioSource fonte;
    private float proximoQuack;

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Debug.LogWarning("GerenciadorDeAudio: já existia um na cena. O duplicado foi removido.", this);
            Destroy(gameObject);
            return;
        }

        instancia = this;

        fonte = gameObject.GetComponent<AudioSource>();
        if (fonte == null) fonte = gameObject.AddComponent<AudioSource>();

        fonte.playOnAwake = false;
        fonte.loop = false;
        fonte.spatialBlend = 0f;
        fonte.volume = 1f;

        if (somMoeda == null) somMoeda = GerarMoeda();
        if (somQuack == null) somQuack = GerarQuack();
        if (somComer == null) somComer = GerarComer();
        if (somFaseCompleta == null) somFaseCompleta = GerarFanfarra();
        if (somErro == null) somErro = GerarErro();
    }

    public void TocarMoeda() { Tocar(somMoeda, 0.8f); }

    public void TocarQuack()
    {
        if (Time.time < proximoQuack) return;
        proximoQuack = Time.time + intervaloEntreQuacks;
        Tocar(somQuack, 0.9f);
    }

    public void TocarComer() { Tocar(somComer, 0.9f); }

    public void TocarErro() { Tocar(somErro, 0.7f); }

    public void TocarFaseCompleta() { Tocar(somFaseCompleta, 1f); }

    void Tocar(AudioClip clipe, float volume)
    {
        if (clipe == null || fonte == null) return;
        fonte.PlayOneShot(clipe, Mathf.Clamp01(volume * volumeGeral));
    }


    private const int Taxa = 44100;

    static AudioClip CriarClip(string nome, float[] amostras)
    {
        AudioClip clipe = AudioClip.Create(nome, amostras.Length, 1, Taxa, false);
        clipe.SetData(amostras, 0);
        return clipe;
    }

    static float Quadrada(float fase) { return Mathf.Sin(2f * Mathf.PI * fase) >= 0f ? 1f : -1f; }

    static float Triangular(float fase)
    {
        float f = fase - Mathf.Floor(fase);          
        return 4f * Mathf.Abs(f - 0.5f) - 1f;        
    }

    static AudioClip GerarMoeda()
    {
        const float duracao = 0.19f;
        const float f1 = 987.77f;    
        const float f2 = 1318.51f;   
        const float troca = 0.065f;

        int total = Mathf.CeilToInt(duracao * Taxa);
        float[] dados = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / Taxa;

            float fase = t < troca ? t * f1 : troca * f1 + (t - troca) * f2;

            float envelope = Mathf.Min(1f, t * 250f) * Mathf.Exp(-t * 10f);
            dados[i] = Quadrada(fase) * envelope * 0.34f;
        }

        return CriarClip("som_moeda", dados);
    }

    static AudioClip GerarQuack()
    {
        const float duracao = 0.20f;
        const float fInicio = 380f;
        const float fFim = 150f;

        int total = Mathf.CeilToInt(duracao * Taxa);
        float[] dados = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / Taxa;
            float vibrato = 1f + 0.07f * Mathf.Sin(2f * Mathf.PI * 30f * t);

            float fase = fInicio * t + (fFim - fInicio) * t * t / (2f * duracao);
            fase *= vibrato;

            float serra = 2f * (fase - Mathf.Floor(fase + 0.5f));
            float ruido = (Mathf.PerlinNoise(i * 0.45f, 0.37f) * 2f - 1f) * 0.20f;

            float envelope = Mathf.Min(1f, t * 70f) * Mathf.Exp(-t * 7f);
            dados[i] = (serra * 0.62f + ruido) * envelope * 0.40f;
        }

        return CriarClip("som_quack", dados);
    }

    static AudioClip GerarComer()
    {
        const float duracao = 0.13f;

        int total = Mathf.CeilToInt(duracao * Taxa);
        float[] dados = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / Taxa;

            float ruido = Mathf.PerlinNoise(i * 0.85f, 2.1f) * 2f - 1f;
            float tom = Mathf.Sin(2f * Mathf.PI * 190f * t);

            float envelope = Mathf.Min(1f, t * 150f) * Mathf.Exp(-t * 26f);
            dados[i] = (ruido * 0.7f + tom * 0.3f) * envelope * 0.35f;
        }

        return CriarClip("som_comer", dados);
    }
    static AudioClip GerarFanfarra()
    {
        float[] notas = { 523.25f, 659.25f, 783.99f, 1046.50f };
        const float duracaoNota = 0.11f;
        float duracao = duracaoNota * notas.Length + 0.25f;

        int total = Mathf.CeilToInt(duracao * Taxa);
        float[] dados = new float[total];

        float[] faseDaNota = new float[notas.Length];
        float acumulado = 0f;
        for (int n = 0; n < notas.Length; n++)
        {
            faseDaNota[n] = acumulado;
            acumulado += duracaoNota * notas[n];
        }

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / Taxa;

            int indice = Mathf.Min((int)(t / duracaoNota), notas.Length - 1);
            float dentroDaNota = t - indice * duracaoNota;

            float fase = faseDaNota[indice] + dentroDaNota * notas[indice];
            float envelope = Mathf.Min(1f, dentroDaNota * 120f) * Mathf.Exp(-dentroDaNota * 5f);
            float fim = 1f - Mathf.Clamp01((t - duracaoNota * notas.Length) / 0.25f);

            dados[i] = Triangular(fase) * envelope * fim * 0.30f;
        }

        return CriarClip("som_fase", dados);
    }

    static AudioClip GerarErro()
    {
        const float duracao = 0.16f;

        int total = Mathf.CeilToInt(duracao * Taxa);
        float[] dados = new float[total];

        for (int i = 0; i < total; i++)
        {
            float t = (float)i / Taxa;

            float onda = Quadrada(140f * t);
            float envelope = Mathf.Min(1f, t * 120f) * Mathf.Exp(-t * 16f);
            dados[i] = onda * envelope * 0.28f;
        }

        return CriarClip("som_erro", dados);
    }
}
