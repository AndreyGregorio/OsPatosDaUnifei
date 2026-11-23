using UnityEngine;
public static class ArteProcedural
{

    public static Color32 CorDoChar(char c)
    {
        switch (c)
        {
            case 'K': return new Color32(38, 28, 45, 255);
            case 'W': return new Color32(255, 255, 255, 255);
            case 'R': return new Color32(232, 72, 85, 255);
            case 'P': return new Color32(246, 140, 170, 255);
            case 'Y': return new Color32(255, 214, 92, 255);
            case 'O': return new Color32(250, 160, 70, 255);
            case 'G': return new Color32(120, 205, 100, 255);
            case 'B': return new Color32(95, 175, 240, 255);
            default: return new Color32(0, 0, 0, 0);
        }
    }

    public static Sprite Criar(string[] linhas, float pixelsPorUnidade = 16f)
    {
        int altura = linhas.Length;

        int largura = 0;
        for (int i = 0; i < altura; i++)
        {
            if (linhas[i].Length > largura) largura = linhas[i].Length;
        }

        Texture2D textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[largura * altura];

        for (int y = 0; y < altura; y++)
        {
            string linha = linhas[y];

            for (int x = 0; x < largura; x++)
            {
                char c = x < linha.Length ? linha[x] : '.';

                int linhaNaTextura = altura - 1 - y;
                pixels[linhaNaTextura * largura + x] = CorDoChar(c);
            }
        }

        textura.SetPixels32(pixels);
        textura.Apply();

        return Sprite.Create(textura,
                             new Rect(0, 0, largura, altura),
                             new Vector2(0.5f, 0.5f),   
                             pixelsPorUnidade);
    }

    public static void ColocarNaFrente(SpriteRenderer sr)
    {
        SortingLayer[] camadas = SortingLayer.layers;
        if (camadas.Length > 0)
        {
            sr.sortingLayerID = camadas[camadas.Length - 1].id;  
        }

        sr.sortingOrder = 32000;
    }
}
