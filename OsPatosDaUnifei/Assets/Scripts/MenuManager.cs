using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Painéis de UI")]
    public GameObject menuPrincipal;
    public GameObject painelPause;
    private bool jogoIniciado = false;
    private bool jogoPausado = false;

    void Start()
    {
        
        if (menuPrincipal != null) menuPrincipal.SetActive(true);
        if (painelPause != null) painelPause.SetActive(false);
        Time.timeScale = 0f;
    }

    void Update()
    {
        if (jogoIniciado && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (jogoPausado) Continuar();
            else Pausar();
        }
    }

    
    public void Jogar()
    {
        if (menuPrincipal != null) menuPrincipal.SetActive(false); 
        Time.timeScale = 1f; 
        jogoIniciado = true;
    }

    
    public void Sair()
    {
        Debug.Log("Fechando o jogo...");
        Application.Quit();
    }

    
    public void Pausar()
    {
        if (painelPause != null) painelPause.SetActive(true);
        Time.timeScale = 0f; 
        jogoPausado = true;
    }

    public void Continuar()
    {
        if (painelPause != null) painelPause.SetActive(false);
        Time.timeScale = 1f; 
        jogoPausado = false;
    }
}