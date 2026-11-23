using UnityEngine;
public enum TipoDeComida
{
    Milho = 0,
    Alface = 1,
    Pao = 2,
}

public class ComidaComTipo : MonoBehaviour
{
    public TipoDeComida tipo = TipoDeComida.Milho;

    public static string NomeDe(TipoDeComida tipo)
    {
        switch (tipo)
        {
            case TipoDeComida.Alface: return "Alface";
            case TipoDeComida.Pao: return "Pão";
            default: return "Milho";
        }
    }

    public static Color CorDe(TipoDeComida tipo)
    {
        switch (tipo)
        {
            case TipoDeComida.Alface: return new Color(0.60f, 0.95f, 0.50f);   
            case TipoDeComida.Pao: return new Color(1.00f, 0.80f, 0.45f);   
            default: return Color.white;                      
        }
    }

    public static ComidaComTipo Marcar(GameObject objeto, TipoDeComida tipo)
    {
        if (objeto == null) return null;

        ComidaComTipo etiqueta = objeto.GetComponent<ComidaComTipo>();
        if (etiqueta == null) etiqueta = objeto.AddComponent<ComidaComTipo>();

        etiqueta.tipo = tipo;

        SpriteRenderer desenho = objeto.GetComponentInChildren<SpriteRenderer>();
        if (desenho != null) desenho.color = CorDe(tipo);

        return etiqueta;
    }

    public static TipoDeComida TipoDe(GameObject objeto)
    {
        if (objeto == null) return TipoDeComida.Milho;

        ComidaComTipo etiqueta = objeto.GetComponent<ComidaComTipo>();
        return etiqueta != null ? etiqueta.tipo : TipoDeComida.Milho;
    }
}
