using UnityEngine;
using UnityEngine.InputSystem; // Novo sistema de input


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Player : MonoBehaviour
{
    public float velocidade = 25f;
    public float zonaMorta = 0.15f;
    public bool manterUltimaDirecao = true;
    public bool priorizarHorizontal = true;
    public Vector2 direcaoInicial = Vector2.down;
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movimento;
    private Vector2 direcaoOlhar = Vector2.down;
    private InputAction moverAcao;
    public Vector2 DirecaoOlhar => direcaoOlhar;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        direcaoOlhar = ParaQuatroDirecoes(direcaoInicial);
        LimparSetasSobrando();
        moverAcao = new InputAction("Mover");

        moverAcao.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // Configura as Setas do Teclado
        moverAcao.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        // Liga o leitor de teclas
        moverAcao.Enable();
    }

    // Desliga o leitor de teclas se o personagem for destruído
    void OnDestroy()
    {
        if (moverAcao != null) moverAcao.Disable();
    }

    void Update()
    {
        movimento = Vector2.ClampMagnitude(moverAcao.ReadValue<Vector2>(), 1f);
        if (movimento.magnitude < zonaMorta) movimento = Vector2.zero;

        Vector2 direcaoAndada = ParaQuatroDirecoes(movimento);

        if (direcaoAndada != Vector2.zero)
        {
            direcaoOlhar = direcaoAndada;
        }
        else if (!manterUltimaDirecao)
        {
            direcaoOlhar = ParaQuatroDirecoes(direcaoInicial);
        }

        animator.SetFloat("MovX", direcaoOlhar.x);
        animator.SetFloat("MovY", direcaoOlhar.y);
        animator.SetFloat("Velocidade", movimento.sqrMagnitude);
    }

    void FixedUpdate()
    {
        // Velocidade no lugar do MovePosition: quem resolve o encostar na parede é a
        // física. O personagem para limpo e desliza na diagonal, sem a tremidinha.
        // Unity 6: linearVelocity. (Unity 2022 ou anterior: rb.velocity)
        rb.linearVelocity = movimento * velocidade;
    }

    void LimparSetasSobrando()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform filho = transform.GetChild(i);

            if (filho != null && filho.name == "Seta")
            {
                Debug.Log("Player: apaguei um desenho de seta que tinha sobrado.", this);
                Destroy(filho.gameObject);
            }
        }
    }

    Vector2 ParaQuatroDirecoes(Vector2 direcao)
    {
        if (direcao.sqrMagnitude < 0.0001f) return Vector2.zero;

        float tamanhoX = Mathf.Abs(direcao.x);
        float tamanhoY = Mathf.Abs(direcao.y);

        bool deLado;

        if (tamanhoX > tamanhoY) deLado = true;
        else if (tamanhoY > tamanhoX) deLado = false;
        else deLado = priorizarHorizontal;

        return deLado
            ? new Vector2(Mathf.Sign(direcao.x), 0f)
            : new Vector2(0f, Mathf.Sign(direcao.y));
    }
}
