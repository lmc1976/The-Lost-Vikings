using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Header("Input System")]
    [SerializeField] private InputActionAsset inputActions;

    private InputAction m_jumpAction;
    private InputAction m_moveAction;

    [Header("Movimentação")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    private Vector2 m_movementInput;
    private Rigidbody2D m_rigidbody;
    private Collider2D m_collider;

    [Header("Detecção")]
    [SerializeField] private float groundCheckHeight = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask ladderLayer;

    [Header("Pulo Avançado")]
    [SerializeField] private float jumpBufferTime = 0.15f;  // tempo que guarda a intenção de pulo
    [SerializeField] private float coyoteTime = 0.15f;      // tempo que permite pular após sair do chão

    private float jumpBufferCounter = 0f;
    private float coyoteCounter = 0f;

    [Header("Escada")]
    private bool onLadder = false;


    private void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody2D>();
        m_collider = GetComponent<Collider2D>();

        m_jumpAction = inputActions.FindAction("Jump");
        m_moveAction = inputActions.FindAction("Move");

        if (m_jumpAction == null || m_moveAction == null || inputActions.FindActionMap("Player") == null)
        {
            Debug.LogError("⚠️ As ações 'Jump' ou 'Move' não foram encontradas no InputActionAsset! ou o Actionmap 'Player' está faltando.");
        }
    }

    private void OnEnable()
    {
        inputActions.FindActionMap("Player").Enable();
    }

    private void OnDisable()
    {
        inputActions.FindActionMap("Player").Disable();
    }

    private void Update()
    {
        // Captura entrada de movimento
        if (m_moveAction != null)
            m_movementInput = m_moveAction.ReadValue<Vector2>();

        // Captura pedido de pulo e inicia contador de buffer
        if (m_jumpAction != null && m_jumpAction.WasPressedThisFrame())
            jumpBufferCounter = jumpBufferTime;

        // Reduz contadores com o tempo
        if (jumpBufferCounter > 0)
            jumpBufferCounter -= Time.deltaTime;

        if (IsGrounded())
            coyoteCounter = coyoteTime; // reset coyote time quando encosta no chão
        else
            coyoteCounter -= Time.deltaTime;




    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleJump();
        HandleLadder();
    }

    private void HandleMovement()
    {
        if (m_rigidbody != null)
            m_rigidbody.linearVelocity = new Vector2(m_movementInput.x * moveSpeed, m_rigidbody.linearVelocity.y);

        // Flip sprite
        if (m_movementInput.x != 0)
            transform.localScale = new Vector2(Mathf.Sign(m_movementInput.x), 1);
    }

    private void HandleJump()
    {
        // Se há buffer de pulo e ainda está no chão ou dentro do coyote time
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            m_rigidbody.linearVelocity = new Vector2(m_rigidbody.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            Debug.Log("⬆️ Pulo executado com buffer e coyote!");
        }
    }

    private bool IsGrounded()
    {
        Vector2 boxSize = new(m_collider.bounds.size.x * 0.9f, groundCheckHeight);
        Vector2 boxCenter = new(m_collider.bounds.center.x, m_collider.bounds.min.y - (groundCheckHeight / 2f));

        return Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundLayer) != null;
    }


private void HandleLadder()
{
    // 1. Usa OverlapBox para PEGAR o Collider2D da escada (LadderCollider).
    // O resultado será o Collider2D da escada se houver contato, ou null se não houver.
    Collider2D ladderCollider = Physics2D.OverlapBox(m_collider.bounds.center, m_collider.bounds.size, 0f, ladderLayer);
    
    // A condição de contato é checada por 'ladderCollider != null'

    if (ladderCollider != null && m_movementInput.y > 0)
    {
        // Obtém a posição X da escada (o objeto colidido)
        float ladderCenterX = ladderCollider.transform.position.x;
        
        onLadder = true;
        m_rigidbody.bodyType = RigidbodyType2D.Kinematic;
        
        // Centraliza o Rigidbody na posição X da escada
        m_rigidbody.position = new Vector2(ladderCenterX - 2f, m_rigidbody.position.y); 
        
        m_rigidbody.linearVelocity = new Vector2(0, m_movementInput.y * moveSpeed);
        Debug.Log("🪜 Tocando escada com seta pra cima");
    }
    else if (onLadder && m_movementInput.y == 0)
    {
        m_rigidbody.linearVelocity = Vector2.zero;
        Debug.Log("🪜 Parado na escada");
    }
    else if (onLadder && m_movementInput.y < 0)
    {
        m_rigidbody.bodyType = RigidbodyType2D.Dynamic;
        onLadder = false;
        Debug.Log("🪜 Saindo da escada com seta pra baixo");
    }
    // Verifica se o jogador estava na escada (onLadder) mas PERDEU O CONTATO (ladderCollider == null)
    else if (onLadder && ladderCollider == null) 
    {
        m_rigidbody.bodyType = RigidbodyType2D.Dynamic;
        onLadder = false;
        Debug.Log("🪜 Saindo da escada por não estar mais em contato");
    }
}




    private void OnDrawGizmos()
    {
        if (m_collider == null) return;

        // Gizmo chão
        Vector2 groundBoxSize = new(m_collider.bounds.size.x * 0.9f, groundCheckHeight);
        Vector2 groundBoxCenter = new(m_collider.bounds.center.x, m_collider.bounds.min.y - (groundCheckHeight / 2f));
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundBoxCenter, groundBoxSize);

        // Gizmo escada
        Vector2 ladderBoxSize = m_collider.bounds.size;
        Vector2 ladderBoxCenter = m_collider.bounds.center;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(ladderBoxCenter, ladderBoxSize);
    }
}
