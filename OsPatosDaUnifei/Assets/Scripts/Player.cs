using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Player : MonoBehaviour
{
    public float velocidade = 25f;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movimento;
    private Vector2 direcaoOlhar = Vector2.down;
    private InputAction moverAcao;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        LimparSetasSobrando();

        moverAcao = new InputAction("Mover");

        moverAcao.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        moverAcao.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        moverAcao.Enable();
    }

    void OnDestroy()
    {
        if (moverAcao != null) moverAcao.Disable();
    }

    void Update()
    {
        movimento = Vector2.ClampMagnitude(moverAcao.ReadValue<Vector2>(), 1f);

        Vector2 direcaoAndada = ParaQuatroDirecoes(movimento);

        if (direcaoAndada != Vector2.zero)
        {
            direcaoOlhar = direcaoAndada;
        }
        else
        {
            direcaoOlhar = Vector2.down;
        }

        animator.SetFloat("MovX", direcaoOlhar.x);
        animator.SetFloat("MovY", direcaoOlhar.y);
        animator.SetFloat("Velocidade", movimento.sqrMagnitude);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movimento * velocidade;
    }

    void LimparSetasSobrando()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform filho = transform.GetChild(i);

            if (filho != null && filho.name == "Seta")
            {
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

        if (tamanhoX > tamanhoY)
            deLado = true;
        else if (tamanhoY > tamanhoX)
            deLado = false;
        else
            deLado = true;

        return deLado
            ? new Vector2(Mathf.Sign(direcao.x), 0f)
            : new Vector2(0f, Mathf.Sign(direcao.y));
    }
}