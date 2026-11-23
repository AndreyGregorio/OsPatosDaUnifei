using System.Collections.Generic;
using UnityEngine;

public class TransparenciaAtrasDoPlayer : MonoBehaviour
{
    public float alphaEscondido = 0.45f;
    public float velocidadeFade = 6f;
    public bool soQuandoNaFrente = true;
    public float folga = 0.35f;
    [SerializeField] private bool estaEscondendo;

    private SpriteRenderer[] sprites;
    private SpriteRenderer spriteDoPlayer;

    void Awake()
    {
        sprites = SpritesQueEuControlo();

        if (sprites.Length == 0)
            Debug.LogWarning($"TransparenciaAtrasDoPlayer: '{name}' não tem nenhum SpriteRenderer com sprite " +
                             "(nem nele, nem nos filhos).", this);
    }

    void Start()
    {
        ProcurarPlayer();
    }

    SpriteRenderer[] SpritesQueEuControlo()
    {
        List<SpriteRenderer> lista = new List<SpriteRenderer>();

        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.sprite == null) continue;

            if (sr.gameObject != gameObject)
            {
                TransparenciaAtrasDoPlayer donoDoSprite = sr.GetComponent<TransparenciaAtrasDoPlayer>();
                if (donoDoSprite != null && donoDoSprite != this) continue;
            }

            lista.Add(sr);
        }

        return lista.ToArray();
    }

    void ProcurarPlayer()
    {
        Player player = FindAnyObjectByType<Player>();

        if (player != null)
        {
            spriteDoPlayer = player.GetComponent<SpriteRenderer>();
            if (spriteDoPlayer == null) spriteDoPlayer = player.GetComponentInChildren<SpriteRenderer>();
        }
        else
        {
            
            GameObject objeto = GameObject.Find("Player");
            if (objeto != null) spriteDoPlayer = objeto.GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteDoPlayer == null)
            Debug.LogWarning("TransparenciaAtrasDoPlayer: não achei o SpriteRenderer do player. " +
                             "O objeto do player precisa se chamar 'Player' ou ter o script Player.", this);
    }

    void Update()
    {
        estaEscondendo = DeveEsconder();

        float alphaAlvo = estaEscondendo ? alphaEscondido : 1f;

        foreach (SpriteRenderer sr in sprites)
        {
            if (sr == null) continue;

            Color c = sr.color;
            c.a = Mathf.MoveTowards(c.a, alphaAlvo, velocidadeFade * Time.deltaTime);
            sr.color = c;
        }
    }

    bool DeveEsconder()
    {
        if (spriteDoPlayer == null) return false;

        foreach (SpriteRenderer sr in sprites)
        {
            if (sr == null) continue;

            if (EstaNaFrente(sr) && Sobrepoe(sr)) return true;
        }

        return false;
    }

    bool EstaNaFrente(SpriteRenderer sr)
    {
        if (!soQuandoNaFrente) return true;

        float camada = SortingLayer.GetLayerValueFromID(sr.sortingLayerID);
        float camadaDoPlayer = SortingLayer.GetLayerValueFromID(spriteDoPlayer.sortingLayerID);

        if (!Mathf.Approximately(camada, camadaDoPlayer)) return camada > camadaDoPlayer;

        if (sr.sortingOrder != spriteDoPlayer.sortingOrder)
            return sr.sortingOrder > spriteDoPlayer.sortingOrder;

        return sr.bounds.min.y < spriteDoPlayer.bounds.min.y;
    }
    bool Sobrepoe(SpriteRenderer sr)
    {
        Bounds objeto = sr.bounds;
        Bounds player = spriteDoPlayer.bounds;

        bool sobrepoeX = player.min.x <= objeto.max.x + folga && player.max.x >= objeto.min.x - folga;
        bool sobrepoeY = player.min.y <= objeto.max.y + folga && player.max.y >= objeto.min.y - folga;

        return sobrepoeX && sobrepoeY;
    }

    void OnDrawGizmosSelected()
    {
        SpriteRenderer[] meus = (sprites != null && sprites.Length > 0) ? sprites : SpritesQueEuControlo();
        if (meus.Length == 0) return;

        Gizmos.color = new Color(0.2f, 0.9f, 1f);

        foreach (SpriteRenderer sr in meus)
        {
            if (sr == null) continue;
            Gizmos.DrawWireCube(sr.bounds.center, sr.bounds.size);
        }
    }
}
